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
    private final Object mailboxLock = new Object();
    private byte[] latest;

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
                Integer sensorOrientation = info.get(CameraCharacteristics.SENSOR_ORIENTATION);
                rotation = sensorOrientation == null ? 270 : sensorOrientation;
                Integer timeSource = info.get(CameraCharacteristics.SENSOR_INFO_TIMESTAMP_SOURCE);
                realtimeTimestamp = timeSource != null && timeSource == CameraMetadata.SENSOR_INFO_TIMESTAMP_SOURCE_REALTIME;
                estimateOptics(info);
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
                        request.set(CaptureRequest.CONTROL_VIDEO_STABILIZATION_MODE, CaptureRequest.CONTROL_VIDEO_STABILIZATION_MODE_OFF);
                        request.set(CaptureRequest.LENS_OPTICAL_STABILIZATION_MODE, CaptureRequest.LENS_OPTICAL_STABILIZATION_MODE_OFF);
                        if (sensorRect != null) request.set(CaptureRequest.SCALER_CROP_REGION, sensorRect);
                        if (disableDistortion) request.set(CaptureRequest.DISTORTION_CORRECTION_MODE, CaptureRequest.DISTORTION_CORRECTION_MODE_OFF);
                        s.setRepeatingRequest(request.build(), null, worker);
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
        Size best = null;
        // Low bandwidth first: largest supported size no larger than 320x240.
        for (Size s : sizes) if (s.getWidth() <= 320 && s.getHeight() <= 240
                && (best == null || area(s) > area(best))) best = s;
        if (best != null) return best;
        best = sizes[0];
        for (Size s : sizes) if (area(s) < area(best)) best = s;
        return best;
    }
    private static long area(Size s) { return (long) s.getWidth() * s.getHeight(); }
    public byte[] takeFrame() {
        synchronized (mailboxLock) { byte[] result = latest; latest = null; return result; }
    }
    public long clockMs() { return SystemClock.elapsedRealtime(); }
    public String snapshot() {
        try {
            JSONObject j = new JSONObject();
            j.put("state", state).put("optics", optics).put("cameraId", cameraId);
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
