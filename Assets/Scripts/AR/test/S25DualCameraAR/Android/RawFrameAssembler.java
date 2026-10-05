package com.example.s25dualcamera;
import org.json.*;
import java.nio.*;
import java.util.*;
/** Exact timestamp join. Never substitutes another frame's exposure or intrinsics. */
public final class RawFrameAssembler {
    private static final class Frame {long t;int w,h,rotation;byte[] gray,rgba;Frame(long t,int w,int h,int r,byte[] g){this.t=t;this.w=w;this.h=h;rotation=r;gray=g;}}
    private final LinkedHashMap<Long,Frame> images=new LinkedHashMap<>();
    private final LinkedHashMap<Long,JSONObject> results=new LinkedHashMap<>();
    private byte[] latest;
    private String status="WAITING_METADATA";
    public synchronized void image(long t,int w,int h,int rotation,byte[] gray,boolean realtime){
        if(!realtime){status="UNSUPPORTED_TIMESTAMP_CLOCK";return;}
        images.put(t,new Frame(t,w,h,rotation,gray));trim(images);join(t);
    }
    public synchronized void colorImage(long t,int w,int h,int rotation,byte[] gray,byte[] rgba,boolean realtime){
        if(!realtime){status="UNSUPPORTED_TIMESTAMP_CLOCK";return;}
        if(gray.length!=(long)w*h || rgba.length!=4L*w*h){status="INVALID_COLOR_PACKET";return;}
        Frame f=new Frame(t,w,h,rotation,gray);f.rgba=rgba;images.put(t,f);trim(images);join(t);
    }
    public synchronized void result(String json){try{JSONObject j=new JSONObject(json);if(j.isNull("android.sensor.timestamp"))return;long t=j.getLong("android.sensor.timestamp");results.put(t,j);trim(results);join(t);}catch(Exception e){status="METADATA_ERROR "+e.getMessage();}}
    private static void trim(Map<Long,?> map){while(map.size()>8)map.remove(map.keySet().iterator().next());}
    private void join(long t){
        if(!images.containsKey(t) || !results.containsKey(t))return;
        Frame f=images.remove(t);JSONObject m=results.remove(t);
        try{
            JSONArray k=m.getJSONArray("android.lens.intrinsicCalibration"),crop=m.getJSONArray("android.scaler.cropRegion");
            long exp=m.getLong("android.sensor.exposureTime"),skew=m.getLong("android.sensor.rollingShutterSkew");
            double cw=crop.getDouble(2)-crop.getDouble(0),ch=crop.getDouble(3)-crop.getDouble(1);
            if(cw<=0 || ch<=0 || Math.abs(cw/ch-f.w/(double)f.h)>1e-5)throw new IllegalStateException("Unhandled crop aspect");
            JSONArray distortion=m.optJSONArray("android.lens.distortion");
            if(distortion==null)throw new IllegalStateException("Missing distortion model");
            for(int i=0;i<distortion.length();i++)if(Math.abs(distortion.getDouble(i))>1e-7)throw new IllegalStateException("Nonzero distortion needs rectification");
            double fx=k.getDouble(0)*f.w/cw,fy=k.getDouble(1)*f.h/ch,cx=(k.getDouble(2)-crop.getDouble(0))*f.w/cw,cy=(k.getDouble(3)-crop.getDouble(1))*f.h/ch;
            if(fx<=0 || fy<=0 || Math.abs(k.getDouble(4))>1e-7)throw new IllegalStateException("Invalid or skewed intrinsics");
            byte[] packet=new byte[48+f.gray.length+(f.rgba==null?0:f.rgba.length)];ByteBuffer b=ByteBuffer.wrap(packet).order(ByteOrder.LITTLE_ENDIAN);
            b.putInt(f.rgba==null?0x35415253:0x37415253).putInt(f.w).putInt(f.h).putInt(f.rotation).putLong(t+exp/2+skew/2).putLong(t);
            b.putFloat((float)fx).putFloat((float)fy).putFloat((float)cx).putFloat((float)cy);b.put(f.gray);if(f.rgba!=null)b.put(f.rgba);latest=packet;status="RAW_READY";
        }catch(Exception e){status="CALIBRATION_REQUIRED "+e.getMessage();}
    }
    public synchronized byte[] take(){byte[] r=latest;latest=null;return r;}
    public synchronized String status(){return status;}
}
