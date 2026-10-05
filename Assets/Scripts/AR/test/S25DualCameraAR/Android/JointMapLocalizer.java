package com.example.s25dualcamera;

import android.os.Handler;
import android.app.Activity;
import android.content.Context;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.os.HandlerThread;
import org.json.*;
import org.opencv.android.OpenCVLoader;
import org.opencv.core.*;
import org.opencv.features2d.*;
import org.opencv.calib3d.Calib3d;
import org.opencv.video.Video;
import java.util.*;

/** Relocalizes both cameras against ONE prebuilt map. No runtime map expansion or inertial position fusion. */
public final class JointMapLocalizer implements SensorEventListener {
    private final HandlerThread thread=new HandlerThread("S25JointMap");
    private final Handler worker;
    private volatile boolean busy,closed;
    private volatile String output="";
    private String draft="";
    private boolean visualAccepted;
    private Mat descriptors;
    private int[] descriptorPoints,descriptorCameras;
    private Point3[] points;
    private double[][] bodyFromCamera;
    private ORB orb;
    private BFMatcher matcher;
    private boolean loaded;
    private final String mapJson;
    private final SensorManager sensors;
    private final HandlerThread imuThread=new HandlerThread("S25MapGyro");
    private final ArrayList<double[]> gyro=new ArrayList<>();
    private double[] commonBody,velocity=new double[3];
    private long commonStamp,processingStamp;
    private double[] processingK;
    private int commonSource=-1;
    @Override public void onSensorChanged(SensorEvent e){synchronized(gyro){if(closed)return;if(!gyro.isEmpty() && e.timestamp<=gyro.get(gyro.size()-1)[0])return;gyro.add(new double[]{e.timestamp,e.values[0],e.values[1],e.values[2]});while(gyro.size()>500)gyro.remove(0);}}
    @Override public void onAccuracyChanged(Sensor sensor,int accuracy){}
    private double[] gyroDelta(long start,long end){
        if(start==end)return identity();long lo=Math.min(start,end),hi=Math.max(start,end);ArrayList<double[]> samples;
        synchronized(gyro){if(gyro.size()<2 || gyro.get(0)[0]>lo || gyro.get(gyro.size()-1)[0]<hi)return null;samples=new ArrayList<>(gyro);}
        double[] delta=identity();Mat rv=new Mat(3,1,CvType.CV_64F),rm=new Mat();
        try{for(int i=1;i<samples.size();i++){double[] a=samples.get(i-1),b=samples.get(i);double l=Math.max(lo,a[0]),r=Math.min(hi,b[0]);if(r<=l)continue;double f=((l+r)*.5-a[0])/(b[0]-a[0]),dt=(r-l)*1e-9;for(int j=0;j<3;j++)rv.put(j,0,(a[j+1]+f*(b[j+1]-a[j+1]))*dt);Calib3d.Rodrigues(rv,rm);double[] m=identity();for(int u=0;u<3;u++)for(int v=0;v<3;v++)m[4*u+v]=rm.get(u,v)[0];delta=ReferenceLink.multiply(delta,m);}}
        finally{rv.release();rm.release();}return end>=start?delta:ReferenceLink.inverse(delta);
    }
    private static double[] identity(){double[] m=new double[16];m[0]=m[5]=m[10]=m[15]=1;return m;}
    private boolean predictOther(int camera,long seq,String cause){
        if(!loaded || commonBody==null || commonSource==camera || Math.abs(processingStamp-commonStamp)>100000000L || processingK==null)return false;
        double[] delta=gyroDelta(commonStamp,processingStamp);if(delta==null)return false;
        double[] pose=ReferenceLink.multiply(commonBody,delta);double dt=(processingStamp-commonStamp)*1e-9;for(int j=0;j<3;j++)pose[4*j+3]+=velocity[j]*dt;
        try{double[] cw=ReferenceLink.inverse(ReferenceLink.multiply(pose,bodyFromCamera[camera]));JSONObject j=new JSONObject().put("camera",camera).put("seq",seq).put("timestampNs",processingStamp).put("valid",true).put("state","OTHER_CAMERA_GYRO_PREDICTION: "+cause).put("bodyPose",new JSONArray(pose)).put("targetId",target);
            if(target>=0){double[] q=transform(cw,points[target]);j.put("targetVisible",q[2]>.05);if(q[2]>.05)j.put("u",processingK[0]*q[0]/q[2]+processingK[2]).put("v",processingK[1]*q[1]/q[2]+processingK[3]);}draft=j.toString();return true;
        }catch(Exception e){return false;}
    }
    private int target=-1,selectionCamera;
    private static final class TrackState {Mat image;long stamp;ArrayList<Point> pixels=new ArrayList<>();ArrayList<Integer> ids=new ArrayList<>();}
    private final TrackState[] tracks={new TrackState(),new TrackState()};
    private void tracked(TrackState state,Mat image,long stamp,int w,int h,ArrayList<Point3> wp,ArrayList<Point> ip,ArrayList<Integer> ids,HashSet<Integer> used){
        if(state.image==null || stamp<=state.stamp || stamp-state.stamp>500000000L || state.pixels.size()<15 || state.image.cols()!=w || state.image.rows()!=h)return;
        MatOfPoint2f a=new MatOfPoint2f(),b=new MatOfPoint2f(),back=new MatOfPoint2f();MatOfByte good=new MatOfByte(),goodBack=new MatOfByte();MatOfFloat err=new MatOfFloat(),errBack=new MatOfFloat();
        try{a.fromList(state.pixels);Video.calcOpticalFlowPyrLK(state.image,image,a,b,good,err);Video.calcOpticalFlowPyrLK(image,state.image,b,back,goodBack,errBack);Point[] bs=b.toArray(),backs=back.toArray();byte[] gs=good.toArray(),gbs=goodBack.toArray();
            for(int i=0;i<bs.length;i++){Point p=bs[i],start=state.pixels.get(i);if(gs[i]==0 || gbs[i]==0 || Math.hypot(backs[i].x-start.x,backs[i].y-start.y)>1 || p.x<0 || p.y<0 || p.x>=w || p.y>=h)continue;int id=state.ids.get(i);used.add(id);wp.add(points[id]);ip.add(p);ids.add(id);}
        }finally{a.release();b.release();back.release();good.release();goodBack.release();err.release();errBack.release();}
    }
    public JointMapLocalizer(Activity activity,String json){mapJson=json;thread.start();worker=new Handler(thread.getLooper());sensors=(SensorManager)activity.getSystemService(Context.SENSOR_SERVICE);imuThread.start();Sensor sensor=sensors.getDefaultSensor(Sensor.TYPE_GYROSCOPE);if(sensor!=null)sensors.registerListener(this,sensor,10000,new Handler(imuThread.getLooper()));}
    private static double[] array(JSONArray a)throws Exception{double[] v=new double[a.length()];for(int i=0;i<v.length;i++)v[i]=a.getDouble(i);return v;}
    private void load()throws Exception{
        if(!OpenCVLoader.initLocal())throw new IllegalStateException("OpenCV failed");
        JSONObject j=new JSONObject(mapJson);JSONArray ps=j.getJSONArray("points");points=new Point3[ps.length()];
        for(int i=0;i<points.length;i++){double[] p=array(ps.getJSONArray(i));points[i]=new Point3(p[0],p[1],p[2]);}
        JSONArray ds=j.getJSONArray("descriptors"),ids=j.getJSONArray("descriptor_points"),cams=j.getJSONArray("descriptor_cameras");
        descriptors=new Mat(ds.length(),32,CvType.CV_8UC1);descriptorPoints=new int[ds.length()];descriptorCameras=new int[ds.length()];
        for(int i=0;i<ds.length();i++){byte[] b=new byte[32];JSONArray row=ds.getJSONArray(i);for(int k=0;k<32;k++)b[k]=(byte)row.getInt(k);descriptors.put(i,0,b);descriptorPoints[i]=ids.getInt(i);descriptorCameras[i]=cams.getInt(i);}
        bodyFromCamera=new double[2][];for(int i=0;i<2;i++)bodyFromCamera[i]=array(j.getJSONArray("body_from_camera").getJSONArray(i));
        orb=ORB.create(1000,1.2f,8,31,0,2,ORB.HARRIS_SCORE,31,10);matcher=BFMatcher.create(Core.NORM_HAMMING,false);loaded=true;
    }
    public synchronized boolean submit(int camera,long seq,long timestampNs,byte[] gray,int width,int height,double[] intrinsics){
        if(closed || busy || !output.isEmpty())return false;busy=true;
        worker.post(()->{try{draft="";if(!loaded)load();processingStamp=timestampNs;processingK=intrinsics;process(camera,seq,timestampNs,gray,width,height,intrinsics);}catch(Exception e){publishError(camera,seq,e.toString());}finally{synchronized(this){output=draft;}busy=false;}});return true;
    }
    public synchronized String takeResult(){String r=output;output="";return r;}
    public void selectTarget(int camera){worker.post(()->{selectionCamera=camera;target=-2;});}
    private synchronized void publishError(int camera,long seq,String message){if((message.startsWith("SEARCHING_MAP") || message.equals("PNP_REJECTED") || message.equals("INSUFFICIENT_SPREAD") || message.equals("HIGH_REPROJECTION") || message.equals("BEHIND_CAMERA")) && predictOther(camera,seq,message))return;try{draft=new JSONObject().put("camera",camera).put("seq",seq).put("valid",false).put("state",message).toString();}catch(Exception ignored){}}
    private void process(int camera,long seq,long stamp,byte[] gray,int w,int h,double[] k)throws Exception{
        visualAccepted=false;processPass(camera,camera,seq,stamp,gray,w,h,k);
        if(!visualAccepted)processPass(camera,1-camera,seq,stamp,gray,w,h,k);
    }
    private void processPass(int camera,int descriptorCamera,long seq,long stamp,byte[] gray,int w,int h,double[] k)throws Exception{
        if(camera<0 || camera>1 || gray.length!=(long)w*h || k.length!=4)throw new IllegalArgumentException("frame");
        Mat image=new Mat(h,w,CvType.CV_8UC1),desc=new Mat(),mask=new Mat();MatOfKeyPoint kp=new MatOfKeyPoint();
        image.put(0,0,gray);ArrayList<MatOfDMatch> matches=new ArrayList<>();
        MatOfPoint3f xyz=new MatOfPoint3f();MatOfPoint2f uv=new MatOfPoint2f();Mat K=Mat.eye(3,3,CvType.CV_64F);MatOfDouble dist=new MatOfDouble();Mat rvec=new Mat(),tvec=new Mat(),inliers=new Mat(),R=new Mat();
        try{
            orb.detectAndCompute(image,mask,kp,desc);
            if(!desc.empty())matcher.knnMatch(desc,descriptors,matches,8);KeyPoint[] keys=kp.toArray();ArrayList<Point3> wp=new ArrayList<>();ArrayList<Point> ip=new ArrayList<>();ArrayList<Integer> mapIds=new ArrayList<>();HashSet<Integer> used=new HashSet<>();
            tracked(tracks[camera],image,stamp,w,h,wp,ip,mapIds,used);
            for(MatOfDMatch mm:matches){DMatch best=null,second=null;for(DMatch match:mm.toArray()){int idx=match.trainIdx;if(descriptorCameras[idx]!=descriptorCamera)continue;if(best==null)best=match;else if(descriptorPoints[idx]!=descriptorPoints[best.trainIdx]){second=match;break;}}if(best==null || second==null || best.distance>55 || best.distance>=.75*second.distance)continue;int id=descriptorPoints[best.trainIdx];if(!used.add(id))continue;wp.add(points[id]);ip.add(keys[best.queryIdx].pt);mapIds.add(id);}
            if(wp.size()<15){publishError(camera,seq,"SEARCHING_MAP matches="+wp.size());return;}
            xyz.fromList(wp);uv.fromList(ip);K.put(0,0,k[0]);K.put(1,1,k[1]);K.put(0,2,k[2]);K.put(1,2,k[3]);
            boolean ok=Calib3d.solvePnPRansac(xyz,uv,K,dist,rvec,tvec,false,200,3f,.999,inliers,Calib3d.SOLVEPNP_EPNP);
            if(!ok || inliers.rows()<(descriptorCamera==camera?15:25) || inliers.rows()<wp.size()*(descriptorCamera==camera?.5:.75)){publishError(camera,seq,"PNP_REJECTED");return;}
            ArrayList<Point3> iw=new ArrayList<>();ArrayList<Point> iu=new ArrayList<>();int cells=0;
            for(int i=0;i<inliers.rows();i++){int index=(int)inliers.get(i,0)[0];iw.add(wp.get(index));Point p=ip.get(index);iu.add(p);int cell=Math.min(2,(int)(p.x*3/w))+3*Math.min(2,(int)(p.y*3/h));cells|=1<<cell;}
            if(Integer.bitCount(cells)<4){publishError(camera,seq,"INSUFFICIENT_SPREAD");return;}
            xyz.fromList(iw);uv.fromList(iu);Calib3d.solvePnPRefineLM(xyz,uv,K,dist,rvec,tvec);Calib3d.Rodrigues(rvec,R);
            double[] cameraFromWorld=new double[16];cameraFromWorld[15]=1;
            for(int i=0;i<3;i++){for(int j=0;j<3;j++)cameraFromWorld[i*4+j]=R.get(i,j)[0];cameraFromWorld[i*4+3]=tvec.get(i,0)[0];}
            double sum=0;for(int i=0;i<iw.size();i++){double[] q=transform(cameraFromWorld,iw.get(i));if(q[2]<=.05){publishError(camera,seq,"BEHIND_CAMERA");return;}Point p=iu.get(i);double dx=k[0]*q[0]/q[2]+k[2]-p.x,dy=k[1]*q[1]/q[2]+k[3]-p.y;sum+=dx*dx+dy*dy;}
            double rms=Math.sqrt(sum/iw.size());if(rms>(descriptorCamera==camera?2.5:1.5)){publishError(camera,seq,"HIGH_REPROJECTION");return;}
            TrackState track=tracks[camera];if(track.image!=null)track.image.release();track.image=image.clone();track.stamp=stamp;track.pixels=new ArrayList<>(iu);track.ids.clear();for(int i=0;i<inliers.rows();i++)track.ids.add(mapIds.get((int)inliers.get(i,0)[0]));
            if(target==-2 && selectionCamera==camera){double best=Double.POSITIVE_INFINITY;for(int i=0;i<inliers.rows();i++){int index=(int)inliers.get(i,0)[0];Point p=ip.get(index);double d=(p.x-w*.5)*(p.x-w*.5)+(p.y-h*.5)*(p.y-h*.5);if(d<best){best=d;target=mapIds.get(index);}}}
            double[] worldFromBody=ReferenceLink.multiply(ReferenceLink.inverse(cameraFromWorld),ReferenceLink.inverse(bodyFromCamera[camera]));
            if(stamp>commonStamp){if(commonBody!=null && stamp-commonStamp<300000000L){double dt=(stamp-commonStamp)*1e-9;for(int axis=0;axis<3;axis++){double v=(worldFromBody[4*axis+3]-commonBody[4*axis+3])/dt;velocity[axis]=Math.abs(v)<2?v:0;}}else Arrays.fill(velocity,0);commonBody=worldFromBody.clone();commonStamp=stamp;commonSource=camera;}
            JSONObject j=new JSONObject().put("camera",camera).put("seq",seq).put("timestampNs",stamp).put("valid",true).put("state","SHARED_MAP_"+(camera==0?"FRONT":"REAR")).put("inliers",iw.size()).put("rms",rms).put("bodyPose",new JSONArray(worldFromBody)).put("targetId",target);
            if(target>=0){double[] q=transform(cameraFromWorld,points[target]);j.put("targetVisible",q[2]>.05);if(q[2]>.05)j.put("u",k[0]*q[0]/q[2]+k[2]).put("v",k[1]*q[1]/q[2]+k[3]);}
            visualAccepted=true;draft=j.toString();
        }finally{for(MatOfDMatch m:matches)m.release();image.release();desc.release();mask.release();kp.release();xyz.release();uv.release();K.release();dist.release();rvec.release();tvec.release();inliers.release();R.release();}
    }
    private static double[] transform(double[] m,Point3 p){double[] q=new double[3];for(int i=0;i<3;i++)q[i]=m[i*4]*p.x+m[i*4+1]*p.y+m[i*4+2]*p.z+m[i*4+3];return q;}
    public void dispose(){closed=true;sensors.unregisterListener(this);imuThread.quit();worker.post(()->{for(TrackState t:tracks)if(t.image!=null)t.image.release();if(descriptors!=null)descriptors.release();if(orb!=null)orb.clear();if(matcher!=null)matcher.clear();thread.quit();});}
}
