package com.example.s25dualcamera;

import android.Manifest;
import android.app.Activity;
import android.content.Context;
import android.content.pm.PackageManager;
import android.graphics.ImageFormat;
import android.graphics.Rect;
import android.hardware.camera2.*;
import android.hardware.camera2.params.StreamConfigurationMap;
import android.media.Image;
import android.media.ImageReader;
import android.os.Handler;
import android.os.HandlerThread;
import android.os.SystemClock;
import android.util.Log;
import android.util.Size;
import android.util.SizeF;
import org.json.JSONObject;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.Collections;

/** CPU transfer prototype: one front YUV stream, latest-frame mailbox, no native UI.
 * Portrait only. No ARCore/rear camera ownership. Unity gets bottom-up upright RGBA.
 * Native conversion throttled to ~15 fps to bound allocations/work during diagnosis. */
public final class FrontFrameSource {
    private final Activity activity;
    private final HandlerThread thread = new HandlerThread("S25FrontFrames");
    private final Handler worker;
    private CameraDevice camera;
    private CameraCaptureSession session;
    private ImageReader reader;
    private int generation, rotation, pendingOpens;
    private long sequence, lastConvertedMs;
    private volatile long converted, lastFrameMs;
    private volatile String state = "IDLE", optics = "unavailable", cameraId = "";
    private volatile boolean disposed;
    private boolean realtimeTimestamp;
    private Size size;
    private float fx, fy, cx, cy;
    private Rect sensorRect;
    private boolean disableDistortion;
    private int autofocusMode = CaptureRequest.CONTROL_AF_MODE_OFF;
    private final Object mailboxLock = new Object();
    private byte[] latest;
    private volatile UnifiedCapture capture;
    private volatile RawFrameAssembler raw;
    private volatile boolean rawColor;
    public void setRawColorEnabled(boolean enabled){rawColor=enabled;}
    public void setRawEnabled(boolean enabled){raw=enabled?new RawFrameAssembler():null;}
    public byte[] takeRawFrame(){RawFrameAssembler r=raw;return r==null?null:r.take();}
    public String rawStatus(){RawFrameAssembler r=raw;return r==null?"DISABLED":r.status()+" | capture "+(size==null?"pending":size.toString())+" | AF "+autofocusMode;}
    public void setCapture(UnifiedCapture recorder){capture=recorder;}
    private void recordRaw(Image image){
        UnifiedCapture recorder=capture;RawFrameAssembler assembler=raw;if(recorder==null && assembler==null)return;
        Image.Plane plane=image.getPlanes()[0];ByteBuffer y=plane.getBuffer().duplicate();
        int w=image.getWidth(),h=image.getHeight(),base=y.position();byte[] pixels=new byte[w*h];
        for(int row=0;row<h;row++)for(int col=0;col<w;col++)pixels[row*w+col]=y.get(base+row*plane.getRowStride()+col*plane.getPixelStride());
        if(recorder!=null)recorder.recordCamera("front",image.getTimestamp(),realtimeTimestamp?"ANDROID_REALTIME":"CAMERA_UNKNOWN",w,h,pixels);
        if(assembler!=null){if(rawColor)assembler.colorImage(image.getTimestamp(),w,h,rotation,pixels,rawRgba(image),realtimeTimestamp);else assembler.image(image.getTimestamp(),w,h,rotation,pixels,realtimeTimestamp);}
    }

    public FrontFrameSource(Activity activity) {
        this.activity = activity;
        thread.start();
        worker = new Handler(thread.getLooper());
    }

    public void start(final String id) {
        if (disposed) return;
        worker.post(() -> {
            if (disposed) return;
            close("STARTING");
            final int token = generation;
            cameraId = id;
            sequence = converted = lastFrameMs = lastConvertedMs = 0;
            try {
                if (activity.checkSelfPermission(Manifest.permission.CAMERA) != PackageManager.PERMISSION_GRANTED)
                    throw new SecurityException("CAMERA permission missing");
                CameraManager manager = (CameraManager) activity.getSystemService(Context.CAMERA_SERVICE);
                CameraCharacteristics info = manager.getCameraCharacteristics(id);
                Integer facing = info.get(CameraCharacteristics.LENS_FACING);
                if (facing == null || facing != CameraCharacteristics.LENS_FACING_FRONT)
                    throw new IllegalArgumentException("ID must be FRONT");
                StreamConfigurationMap map = info.get(CameraCharacteristics.SCALER_STREAM_CONFIGURATION_MAP);
                if (map == null) throw new IllegalStateException("No stream map");
                size = chooseSize(map.getOutputSizes(ImageFormat.YUV_420_888));
                autofocusMode = CaptureRequest.CONTROL_AF_MODE_OFF;
                int[] afModes = info.get(CameraCharacteristics.CONTROL_AF_AVAILABLE_MODES);
                if (afModes != null) for (int mode : afModes)
                    if (mode == CaptureRequest.CONTROL_AF_MODE_CONTINUOUS_VIDEO) autofocusMode = mode;
                if (autofocusMode == CaptureRequest.CONTROL_AF_MODE_OFF && afModes != null)
                    for (int mode : afModes) if (mode == CaptureRequest.CONTROL_AF_MODE_CONTINUOUS_PICTURE) autofocusMode = mode;
                Integer sensorOrientation = info.get(CameraCharacteristics.SENSOR_ORIENTATION);
                rotation = sensorOrientation == null ? 270 : sensorOrientation;
                Integer timeSource = info.get(CameraCharacteristics.SENSOR_INFO_TIMESTAMP_SOURCE);
                realtimeTimestamp = timeSource != null && timeSource == CameraMetadata.SENSOR_INFO_TIMESTAMP_SOURCE_REALTIME;
                estimateOptics(info);
                if(capture!=null){
                    JSONObject meta=new JSONObject();
                    meta.put("cameraId",id).put("sensorOrientation",rotation).put("timestampRealtime",realtimeTimestamp);
                    meta.put("width",size.getWidth()).put("height",size.getHeight());
                    meta.put("intrinsicsStatus","DEVICE_METADATA_REQUIRES_CROP_AND_DISTORTION_VALIDATION");
                    meta.put("characteristics",CaptureMetadataDump.characteristics(info));
                    meta.put("requestedCrop",sensorRect==null?JSONObject.NULL:new org.json.JSONArray(new int[]{sensorRect.left,sensorRect.top,sensorRect.right,sensorRect.bottom}));
                    meta.put("requestedDistortionOff",disableDistortion);
                    meta.put("poseWarning","Pose reference may be a same-facing camera or undefined; not automatically an IMU extrinsic.");
                    meta.put("note","No image rotation or mirroring; calibrate against these raw frames.");
                    capture.metadata("front_metadata",meta.toString());
                }
                reader = ImageReader.newInstance(size.getWidth(), size.getHeight(), ImageFormat.YUV_420_888, 3);
                reader.setOnImageAvailableListener(source -> {
                    if (token != generation || disposed) return;
                    Image image = null;
                    try {
                        image = source.acquireLatestImage();
                        if (image == null || token != generation) return;
                        long now = SystemClock.elapsedRealtime();
                        if (now - lastConvertedMs < 66) return;
                        lastConvertedMs = now;
                        recordRaw(image);
                        if(raw!=null && capture==null){converted++;lastFrameMs=now;state="RAW_STREAMING";return;}
                        long sampleMs = realtimeTimestamp ? image.getTimestamp() / 1000000L : now;
                        byte[] packet = convert(image, sampleMs);
                        synchronized (mailboxLock) { latest = packet; }
                        converted++;
                        lastFrameMs = now;
                        state = "STREAMING";
                    } catch (Exception e) {
                        if (token == generation) fail("CONVERT", e);
                    } finally { if (image != null) image.close(); }
                }, worker);
                state = "OPENING";
                pendingOpens++;
                try { manager.openCamera(id, new CameraDevice.StateCallback() {
                    private boolean delivered;
                    private void markDelivered() {
                        if (!delivered) { delivered = true; pendingOpens--; }
                    }
                    @Override public void onOpened(CameraDevice device) {
                        markDelivered();
                        if (token != generation || disposed) { device.close(); maybeQuit(); return; }
                        camera = device;
                        configure(token);
                    }
                    @Override public void onDisconnected(CameraDevice device) {
                        markDelivered();
                        device.close();
                        if (token == generation) close("DISCONNECTED");
                        maybeQuit();
                    }
                    @Override public void onError(CameraDevice device, int error) {
                        markDelivered();
                        device.close();
                        if (token == generation) close("CAMERA_ERROR " + error);
                        maybeQuit();
                    }
                }, worker); } catch (Exception openError) { pendingOpens--; throw openError; }
                worker.postDelayed(() -> {
                    if (token == generation && converted == 0) close("FIRST_FRAME_TIMEOUT: " + state);
                }, 10000);
            } catch (Exception e) { fail("START", e); }
        });
    }

    @SuppressWarnings("deprecation")
    private void configure(final int token) {
        try {
            state = "CONFIGURING";
            camera.createCaptureSession(Collections.singletonList(reader.getSurface()), new CameraCaptureSession.StateCallback() {
                @Override public void onConfigured(CameraCaptureSession s) {
                    if (token != generation || disposed) { s.close(); return; }
                    session = s;
                    try {
                        CaptureRequest.Builder request = camera.createCaptureRequest(CameraDevice.TEMPLATE_PREVIEW);
                        request.addTarget(reader.getSurface());
                        request.set(CaptureRequest.CONTROL_MODE, CaptureRequest.CONTROL_MODE_AUTO);
                        request.set(CaptureRequest.CONTROL_AF_MODE, autofocusMode);
                        request.set(CaptureRequest.CONTROL_VIDEO_STABILIZATION_MODE, CaptureRequest.CONTROL_VIDEO_STABILIZATION_MODE_OFF);
                        request.set(CaptureRequest.LENS_OPTICAL_STABILIZATION_MODE, CaptureRequest.LENS_OPTICAL_STABILIZATION_MODE_OFF);
                        if (sensorRect != null) request.set(CaptureRequest.SCALER_CROP_REGION, sensorRect);
                        if (disableDistortion) request.set(CaptureRequest.DISTORTION_CORRECTION_MODE, CaptureRequest.DISTORTION_CORRECTION_MODE_OFF);
                        CameraCaptureSession.CaptureCallback metadataCallback=capture==null && raw==null?null:new CameraCaptureSession.CaptureCallback(){
                            @Override public void onCaptureCompleted(CameraCaptureSession session,CaptureRequest req,TotalCaptureResult result){
                                if(token!=generation || disposed)return;
                                UnifiedCapture recorder=capture;RawFrameAssembler assembler=raw;
                                try{String json=CaptureMetadataDump.frame(result,SystemClock.elapsedRealtimeNanos());if(recorder!=null)recorder.frameMetadata(json);if(assembler!=null)assembler.result(json);}
                                catch(Exception e){if(recorder!=null)recorder.metadataError(e.toString());else fail("METADATA",e);}
                            }
                        };
                        s.setRepeatingRequest(request.build(), metadataCallback, worker);
                        state = "WAITING_FOR_YUV";
                    } catch (Exception e) { fail("REPEATING", e); }
                }
                @Override public void onConfigureFailed(CameraCaptureSession s) {
                    s.close();
                    if (token == generation) close("YUV_CONFIGURE_FAILED");
                }
            }, worker);
        } catch (Exception e) { fail("SESSION", e); }
    }

    private void estimateOptics(CameraCharacteristics info) {
        int[] modes = info.get(CameraCharacteristics.DISTORTION_CORRECTION_AVAILABLE_MODES);
        disableDistortion = false;
        if (modes != null) for (int m : modes) if (m == CameraMetadata.DISTORTION_CORRECTION_MODE_OFF) disableDistortion = true;
        sensorRect = info.get(disableDistortion ? CameraCharacteristics.SENSOR_INFO_PRE_CORRECTION_ACTIVE_ARRAY_SIZE
                : CameraCharacteristics.SENSOR_INFO_ACTIVE_ARRAY_SIZE);
        if (sensorRect == null) sensorRect = info.get(CameraCharacteristics.SENSOR_INFO_ACTIVE_ARRAY_SIZE);
        float[] k = info.get(CameraCharacteristics.LENS_INTRINSIC_CALIBRATION);
        float cropW = sensorRect == null ? size.getWidth() : sensorRect.width();
        float cropH = sensorRect == null ? size.getHeight() : sensorRect.height();
        float centerX = sensorRect == null ? cropW / 2 : sensorRect.exactCenterX();
        float centerY = sensorRect == null ? cropH / 2 : sensorRect.exactCenterY();
        float ratio = size.getWidth() / (float) size.getHeight();
        if (cropW / cropH > ratio) cropW = cropH * ratio; else cropH = cropW / ratio;
        if (disableDistortion && sensorRect != null && k != null && k.length >= 4 && k[0] > 0 && k[1] > 0) {
            fx = k[0] * size.getWidth() / cropW;
            fy = k[1] * size.getHeight() / cropH;
            cx = (k[2] - (centerX - cropW / 2)) * size.getWidth() / cropW;
            cy = (k[3] - (centerY - cropH / 2)) * size.getHeight() / cropH;
            optics = "reported intrinsics + assumed centered crop (not calibrated)";
        } else {
            SizeF physical = info.get(CameraCharacteristics.SENSOR_INFO_PHYSICAL_SIZE);
            Size pixels = info.get(CameraCharacteristics.SENSOR_INFO_PIXEL_ARRAY_SIZE);
            float[] focal = info.get(CameraCharacteristics.LENS_INFO_AVAILABLE_FOCAL_LENGTHS);
            if (physical != null && pixels != null && focal != null && focal.length > 0 && sensorRect != null) {
                fx = focal[0] * pixels.getWidth() / physical.getWidth() * size.getWidth() / cropW;
                fy = focal[0] * pixels.getHeight() / physical.getHeight() * size.getHeight() / cropH;
                optics = "focal/sensor-size estimate (not calibrated)";
            } else {
                fx = fy = size.getWidth() / (2f * (float) Math.tan(Math.toRadians(65) / 2));
                optics = "fallback 65 degree raw horizontal FOV (not calibrated)";
            }
            cx = (size.getWidth() - 1) / 2f;
            cy = (size.getHeight() - 1) / 2f;
        }
    }

    // Top-down, unrotated color image. Gray and color share the exact source Image.
    private byte[] rawRgba(Image image){
        int w=image.getWidth(),h=image.getHeight();byte[] out=new byte[w*h*4];Image.Plane[] p=image.getPlanes();
        ByteBuffer y=p[0].getBuffer(),u=p[1].getBuffer(),v=p[2].getBuffer();int y0=y.position(),u0=u.position(),v0=v.position();
        for(int row=0;row<h;row++)for(int col=0;col<w;col++){
            int yy=(y.get(y0+row*p[0].getRowStride()+col*p[0].getPixelStride())&255)-16;
            int uu=(u.get(u0+row/2*p[1].getRowStride()+col/2*p[1].getPixelStride())&255)-128;
            int vv=(v.get(v0+row/2*p[2].getRowStride()+col/2*p[2].getPixelStride())&255)-128;
            int l=298*Math.max(0,yy),i=(row*w+col)*4;out[i]=(byte)clamp((l+409*vv+128)>>8);out[i+1]=(byte)clamp((l-100*uu-208*vv+128)>>8);out[i+2]=(byte)clamp((l+516*uu+128)>>8);out[i+3]=(byte)255;
        }return out;
    }
    private byte[] convert(Image image, long sampleMs) {
        int w = image.getWidth(), h = image.getHeight();
        Rect crop = image.getCropRect();
        if (crop.left != 0 || crop.top != 0 || crop.width() != w || crop.height() != h)
            throw new IllegalStateException("Unexpected YUV buffer crop: " + crop);
        boolean swap = rotation % 180 != 0;
        int ow = swap ? h : w, oh = swap ? w : h;
        byte[] packet = new byte[48 + ow * oh * 4];
        ByteBuffer header = ByteBuffer.wrap(packet).order(ByteOrder.LITTLE_ENDIAN);
        float ofx = swap ? fy : fx, ofy = swap ? fx : fy, ocx = cx, ocy = cy;
        if (rotation == 90) { ocx = h - 1 - cy; ocy = cx; }
        else if (rotation == 180) { ocx = w - 1 - cx; ocy = h - 1 - cy; }
        else if (rotation == 270) { ocx = cy; ocy = w - 1 - cx; }
        header.putInt(0x33465241).putInt(ow).putInt(oh).putInt(rotation);
        header.putLong(++sequence).putLong(sampleMs);
        header.putFloat(ofx).putFloat(ofy).putFloat(ocx).putFloat(ocy);
        Image.Plane[] planes = image.getPlanes();
        ByteBuffer yb = planes[0].getBuffer(), ub = planes[1].getBuffer(), vb = planes[2].getBuffer();
        int y0 = yb.position(), u0 = ub.position(), v0 = vb.position();
        int yr = planes[0].getRowStride(), ur = planes[1].getRowStride(), vr = planes[2].getRowStride();
        int yp = planes[0].getPixelStride(), up = planes[1].getPixelStride(), vp = planes[2].getPixelStride();
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) {
            int yy = (yb.get(y0 + y * yr + x * yp) & 255) - 16;
            int u = (ub.get(u0 + (y / 2) * ur + (x / 2) * up) & 255) - 128;
            int v = (vb.get(v0 + (y / 2) * vr + (x / 2) * vp) & 255) - 128;
            int luma = 298 * Math.max(0, yy);
            int ox = x, oy = y;
            if (rotation == 90) { ox = h - 1 - y; oy = x; }
            else if (rotation == 180) { ox = w - 1 - x; oy = h - 1 - y; }
            else if (rotation == 270) { ox = y; oy = w - 1 - x; }
            int i = 48 + ((oh - 1 - oy) * ow + ox) * 4; // Unity Texture2D bottom-up
            packet[i] = (byte) clamp((luma + 409 * v + 128) >> 8);
            packet[i + 1] = (byte) clamp((luma - 100 * u - 208 * v + 128) >> 8);
            packet[i + 2] = (byte) clamp((luma + 516 * u + 128) >> 8);
            packet[i + 3] = (byte) 255;
        }
        return packet;
    }

    private static int clamp(int n) { return Math.max(0, Math.min(255, n)); }
    private Size chooseSize(Size[] sizes) {
        if (sizes == null || sizes.length == 0) throw new IllegalStateException("No YUV sizes");
        // Preserve sensor detail for preview/recording. Tracking is resized separately.
        // Prefer 4:3 so raw intrinsics retain the same sensor crop as existing maps.
        Size best = null;
        for (Size candidate : sizes) {
            int w=candidate.getWidth(), h=candidate.getHeight();
            if (w<=1280 && h<=960 && Math.abs(w/(double)h-4.0/3.0)<0.01
                    && (best==null || area(candidate)>area(best))) best=candidate;
        }
        if (best!=null) return best;
        for (Size candidate : sizes) if (candidate.getWidth()<=1280 && candidate.getHeight()<=960
                && (best==null || area(candidate)>area(best))) best=candidate;
        if (best!=null) return best;
        best=sizes[0];
        for(Size candidate:sizes) if(area(candidate)<area(best)) best=candidate;
        return best;
    }
    private static long area(Size s) { return (long) s.getWidth() * s.getHeight(); }
    public byte[] takeFrame() {
        synchronized (mailboxLock) { byte[] result = latest; latest = null; return result; }
    }
    public long clockMs() { return SystemClock.elapsedRealtime(); }
    public long clockNs() { return SystemClock.elapsedRealtimeNanos(); }
    public String snapshot() {
        try {
            JSONObject j = new JSONObject();
            j.put("state", state).put("optics", optics).put("cameraId", cameraId);
            j.put("captureWidth",size==null?0:size.getWidth()).put("captureHeight",size==null?0:size.getHeight()).put("autofocusMode",autofocusMode);
            j.put("converted", converted).put("frameAgeMs", lastFrameMs == 0 ? -1 : SystemClock.elapsedRealtime() - lastFrameMs);
            j.put("timestampSource", realtimeTimestamp ? "REALTIME" : "callback receipt");
            return j.toString();
        } catch (Exception e) { return "{\"state\":\"SNAPSHOT_ERROR\"}"; }
    }
    public void stop() { if (!disposed) worker.post(() -> close("STOPPED")); }
    public void dispose() {
        if (disposed) return;
        disposed = true;
        worker.post(() -> { close("DISPOSED"); maybeQuit(); });
    }
    private void maybeQuit() { if (disposed && pendingOpens == 0) thread.quitSafely(); }
    private void close(String reason) {
        generation++;
        if (session != null) { session.close(); session = null; }
        if (camera != null) { camera.close(); camera = null; }
        if (reader != null) { reader.close(); reader = null; }
        synchronized (mailboxLock) { latest = null; }
        state = reason;
        Log.i("S25FrontFrames", reason);
    }
    private void fail(String stage, Exception e) {
        Log.e("S25FrontFrames", stage, e);
        close("ERROR " + stage + ": " + e.getClass().getSimpleName() + ": " + e.getMessage());
    }
}
