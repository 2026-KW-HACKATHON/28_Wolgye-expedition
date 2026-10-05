package com.example.s25dualcamera;

import android.app.Activity;
import android.content.Context;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.os.Handler;
import android.os.HandlerThread;
import java.io.*;
import java.util.concurrent.*;
import java.util.zip.*;
import java.util.concurrent.atomic.AtomicInteger;

/** U2 measurement recorder, NOT a pose estimator. Original clock domains are preserved. */
public final class UnifiedCapture implements SensorEventListener {
    private final SensorManager sensors;
    private final HandlerThread imuThread=new HandlerThread("S25RawImu");
    private final ExecutorService disk=Executors.newSingleThreadExecutor();
    private final AtomicInteger queued=new AtomicInteger();
    private final File root;
    private BufferedWriter imu, cameras, frameMetadata;
    private volatile boolean closed;
    private volatile String error="",state="RECORDING";
    private volatile long front,rear,gyro,accel,dropped,results;
    public UnifiedCapture(Activity activity,String directory) throws IOException {
        root=new File(directory);
        if(!root.mkdirs() && !root.isDirectory())throw new IOException("Cannot create capture directory");
        sensors=(SensorManager)activity.getSystemService(Context.SENSOR_SERVICE);
        Sensor a=sensors.getDefaultSensor(Sensor.TYPE_ACCELEROMETER),g=sensors.getDefaultSensor(Sensor.TYPE_GYROSCOPE);
        if(a==null || g==null){disk.shutdown();throw new IOException("Accelerometer or gyroscope missing");}
        imu=new BufferedWriter(new FileWriter(new File(root,"imu.csv")));
        cameras=new BufferedWriter(new FileWriter(new File(root,"cameras.csv")));
        frameMetadata=new BufferedWriter(new FileWriter(new File(root,"front_capture_results.jsonl")));
        imu.write("sensor,timestamp_ns,x,y,z,accuracy\n");
        cameras.write("camera,timestamp_ns,clock,width,height,file\n");
        imuThread.start();Handler handler=new Handler(imuThread.getLooper());
        boolean ar=sensors.registerListener(this,a,5000,handler);
        boolean gr=sensors.registerListener(this,g,5000,handler);
        if(!ar || !gr){close();throw new IOException("IMU registration failed");}
    }
    private synchronized boolean enqueue(Runnable task){
        if(closed)return false;
        if(queued.get()>=128){dropped++;return false;}
        queued.incrementAndGet();
        disk.execute(()->{try{task.run();}catch(Exception e){error=e.toString();}finally{queued.decrementAndGet();}});
        return true;
    }
    @Override public void onSensorChanged(SensorEvent e){
        final int type=e.sensor.getType(),accuracy=e.accuracy;
        final long t=e.timestamp;final float x=e.values[0],y=e.values[1],z=e.values[2];
        enqueue(()->{try{
            imu.write((type==Sensor.TYPE_GYROSCOPE?"gyro":"accel")+","+t+","+x+","+y+","+z+","+accuracy+"\n");
            if(type==Sensor.TYPE_GYROSCOPE)gyro++;else accel++;
        }catch(IOException ex){throw new RuntimeException(ex);}});
    }
    @Override public void onAccuracyChanged(Sensor s,int accuracy){}
    public void recordCamera(String camera,long timestamp,String clock,int w,int h,byte[] pixels){
        if(!camera.equals("front") && !camera.equals("rear"))throw new IllegalArgumentException("camera");
        if(w<=0 || h<=0 || pixels.length!=(long)w*h)throw new IllegalArgumentException("image size");
        enqueue(()->{try{
            String name=camera+"_"+timestamp+".pgm";
            try(OutputStream f=new BufferedOutputStream(new FileOutputStream(new File(root,name)))){
                f.write(("P5\n"+w+" "+h+"\n255\n").getBytes("US-ASCII"));f.write(pixels);
            }
            cameras.write(camera+","+timestamp+","+clock+","+w+","+h+","+name+"\n");
            if(camera.equals("front"))front++;else rear++;
        }catch(IOException e){throw new RuntimeException(e);}});
    }
    public void metadata(String name,String text){
        if(!name.matches("[a-zA-Z0-9_-]+"))throw new IllegalArgumentException("metadata name");
        enqueue(()->{try(Writer w=new FileWriter(new File(root,name+".json"))){w.write(text);}catch(IOException e){throw new RuntimeException(e);}});
    }
    public void frameMetadata(String json){
        enqueue(()->{try{frameMetadata.write(json);frameMetadata.newLine();results++;}catch(IOException e){throw new RuntimeException(e);}});
    }
    public void metadataError(String message){error="Metadata: "+message;}
    public String status(){return state+" | Front "+front+" Rear "+rear+" Gyro "+gyro+" Accel "+accel+" Results "+results+" | dropped "+dropped+" | "+error;}
    public synchronized void close(){
        if(closed)return;closed=true;state="SAVING";sensors.unregisterListener(this);imuThread.quit();
        disk.execute(()->{try{imu.close();cameras.close();frameMetadata.close();
            try(Writer summary=new FileWriter(new File(root,"summary.json"))){summary.write("{\"front\":"+front+",\"rear\":"+rear+",\"gyro\":"+gyro+",\"accel\":"+accel+",\"dropped\":"+dropped+",\"capture_results\":"+results+",\"write_errors\":"+(!error.isEmpty())+"}");}
            File[] files=root.listFiles();if(files==null)throw new IOException("Missing capture directory");
            try(ZipOutputStream zip=new ZipOutputStream(new BufferedOutputStream(new FileOutputStream(root.getPath()+".zip")))){
                byte[] buffer=new byte[65536];
                for(File f:files){zip.putNextEntry(new ZipEntry(f.getName()));try(InputStream in=new FileInputStream(f)){int n;while((n=in.read(buffer))!=-1)zip.write(buffer,0,n);}zip.closeEntry();}
            }
            state=error.isEmpty()?"SAVED":"SAVED_WITH_ERRORS";}catch(IOException e){error=e.toString();state="SAVE_ERROR";}});
        disk.shutdown();
    }
}
