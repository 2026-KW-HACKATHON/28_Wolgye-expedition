package com.example.s25dualcamera;

import android.graphics.Rect;
import android.hardware.camera2.CameraCharacteristics;
import android.hardware.camera2.CaptureResult;
import android.util.Size;
import android.util.SizeF;
import org.json.JSONArray;
import org.json.JSONObject;
import java.lang.reflect.Array;
import java.util.Arrays;
import java.util.HashSet;
import java.util.Set;

/** Preserve native units and coordinate systems. Null explicitly means unavailable. */
public final class CaptureMetadataDump {
    private static final String[] STATIC_KEYS={
        "android.lens.intrinsicCalibration","android.lens.distortion","android.lens.radialDistortion",
        "android.lens.poseRotation","android.lens.poseTranslation","android.lens.poseReference",
        "android.lens.facing","android.lens.info.availableFocalLengths","android.lens.info.minimumFocusDistance",
        "android.sensor.orientation","android.sensor.info.activeArraySize","android.sensor.info.preCorrectionActiveArraySize",
        "android.sensor.info.pixelArraySize","android.sensor.info.physicalSize","android.sensor.info.timestampSource",
        "android.distortionCorrection.availableModes","android.lens.info.availableOpticalStabilization",
        "android.control.availableVideoStabilizationModes","android.info.supportedHardwareLevel"};
    private static final String[] FRAME_KEYS={
        "android.sensor.timestamp","android.sensor.exposureTime","android.sensor.frameDuration",
        "android.sensor.rollingShutterSkew","android.sensor.sensitivity",
        "android.lens.intrinsicCalibration","android.lens.distortion","android.lens.radialDistortion",
        "android.lens.focalLength","android.lens.focusDistance","android.lens.state",
        "android.lens.poseRotation","android.lens.poseTranslation","android.scaler.cropRegion",
        "android.distortionCorrection.mode","android.control.videoStabilizationMode",
        "android.lens.opticalStabilizationMode","android.control.zoomRatio","android.control.aeState"};
    private static Object value(Object v)throws Exception{
        if(v==null)return JSONObject.NULL;
        if(v instanceof Rect){Rect r=(Rect)v;return new JSONArray(new int[]{r.left,r.top,r.right,r.bottom});}
        if(v instanceof Size){Size s=(Size)v;return new JSONArray(new int[]{s.getWidth(),s.getHeight()});}
        if(v instanceof SizeF){SizeF s=(SizeF)v;return new JSONArray(new double[]{s.getWidth(),s.getHeight()});}
        if(v.getClass().isArray()){JSONArray a=new JSONArray();for(int i=0;i<Array.getLength(v);i++)a.put(value(Array.get(v,i)));return a;}
        if(v instanceof Number || v instanceof Boolean || v instanceof String)return v;
        return v.toString();
    }
    public static JSONObject characteristics(CameraCharacteristics c)throws Exception{
        JSONObject j=new JSONObject();Set<String> wanted=new HashSet<>(Arrays.asList(STATIC_KEYS));
        for(String k:STATIC_KEYS)j.put(k,JSONObject.NULL);
        for(CameraCharacteristics.Key<?> k:c.getKeys())if(wanted.contains(k.getName()))j.put(k.getName(),value(c.get(k)));
        return j;
    }
    public static String frame(CaptureResult r,long receiptNs)throws Exception{
        JSONObject j=new JSONObject();Set<String> wanted=new HashSet<>(Arrays.asList(FRAME_KEYS));
        for(String k:FRAME_KEYS)j.put(k,JSONObject.NULL);
        for(CaptureResult.Key<?> k:r.getKeys())if(wanted.contains(k.getName()))j.put(k.getName(),value(r.get(k)));
        j.put("frameNumber",r.getFrameNumber()).put("receiptElapsedRealtimeNs",receiptNs);
        return j.toString();
    }
}
