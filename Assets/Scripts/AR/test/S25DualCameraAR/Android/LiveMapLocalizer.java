package com.example.s25dualcamera;
import android.os.Handler;
import android.os.HandlerThread;
import org.opencv.android.OpenCVLoader;
import org.json.JSONObject;
import java.io.*;
/** One job and one result at a time: no stale unbounded queue. */
public final class LiveMapLocalizer {
    private final HandlerThread thread=new HandlerThread("S25LiveMap");
    private final Handler worker;
    private volatile boolean closed,busy;
    private String output="",action="";
    private LiveMapEngine engine;
    private FrontFloorEngine floorEngine;
    private final long epoch;
    private final boolean areaMode;
    public LiveMapLocalizer(long epoch){this(epoch,false);}
    public LiveMapLocalizer(long epoch,boolean areaMode){this.epoch=epoch;this.areaMode=areaMode;thread.start();worker=new Handler(thread.getLooper());}
    private void load(){if(engine==null){if(!OpenCVLoader.initLocal())throw new IllegalStateException("OPENCV_UNAVAILABLE");engine=new LiveMapEngine(epoch,areaMode);}}
    public synchronized boolean submit(int camera,long seq,long stamp,byte[] gray,int w,int h,double[] k,double[] pose){
        return submit(camera,seq,stamp,gray,w,h,k,pose,null);
    }
    public synchronized boolean submit(int camera,long seq,long stamp,byte[] gray,int w,int h,double[] k,double[] pose,double[] target){
        return submit(camera,seq,stamp,gray,w,h,k,pose,target,null);
    }
    public synchronized boolean submit(int camera,long seq,long stamp,byte[] gray,int w,int h,double[] k,double[] pose,double[] target,double[] plane){
        if(closed||busy||!output.isEmpty())return false;busy=true;
        worker.post(()->{long started=System.nanoTime();String result;try{load();if(plane!=null && plane.length>=17){if(floorEngine==null)floorEngine=new FrontFloorEngine(epoch);result=floorEngine.process(camera,seq,stamp,gray,w,h,k,pose,plane,target);}else{engine.updateTarget(target);result=engine.process(camera,seq,stamp,gray,w,h,k,pose);}result=new JSONObject(result).put("processingMs",(System.nanoTime()-started)/1000000.0).toString();}catch(Throwable e){
            try{result=new JSONObject().put("epoch",epoch).put("camera",camera).put("seq",seq).put("valid",false).put("state","ERROR: "+e.toString()).toString();}catch(Exception ignored){result="";}}
            synchronized(this){if(!closed)output=result;busy=false;}});return true;
    }
    public synchronized String takeResult(){String r=output;output="";return r;}
    public synchronized String takeAction(){String r=action;action="";return r;}
    public synchronized boolean place(String mapPath){
        return place(mapPath,null);
    }
    public synchronized boolean place(String mapPath,double[] target){
        if(closed||busy||engine==null||!output.isEmpty())return false;busy=true;
        worker.post(()->{String value;try{value=target==null?engine.place():engine.placeAt(target);if(value.equals("TARGET_PLACED")){
                File f=new File(mapPath);try(Writer w=new OutputStreamWriter(new FileOutputStream(f),"UTF-8")){w.write(engine.exportMap());}}
            }catch(Exception e){value="SAVE_ERROR: "+e;}
            synchronized(this){if(!closed)action=value;busy=false;}});return true;
    }
    public synchronized void dispose(){if(closed)return;closed=true;output="";worker.post(()->{if(engine!=null)engine.close();if(floorEngine!=null)floorEngine.close();floorEngine=null;engine=null;thread.quitSafely();});}
}
