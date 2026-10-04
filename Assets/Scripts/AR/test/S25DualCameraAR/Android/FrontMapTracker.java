package com.example.s25dualcamera;

import android.os.Handler;
import android.os.HandlerThread;
import org.json.JSONArray;
import org.json.JSONObject;
import org.opencv.android.OpenCVLoader;
import org.opencv.core.*;
import org.opencv.video.Video;
import org.opencv.calib3d.Calib3d;
import org.opencv.features2d.ORB;
import org.opencv.features2d.BFMatcher;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.*;

/** Bounded growing front map, descriptor relocalization and gated rear-pose fallback.
 * Not a joint VIO optimizer: no IMU fusion or global bundle adjustment. */
public final class FrontMapTracker {
    private final HandlerThread thread=new HandlerThread("S25FrontMap");
    private final Handler worker;
    private volatile boolean busy,closed;
    private volatile String result;
    private String draftResult;
    private Growth growth;
    private final ReferenceLink referenceLink=new ReferenceLink();
    private int frameEpoch;
    private boolean recovering;
    private double lastRms;
    private int lastInliers;
    private boolean loaded,haveMap,lost;
    private Mat previous;
    private final ArrayList<Point> pixels=new ArrayList<>(), seedPixels=new ArrayList<>();
    private final ArrayList<Point3> world=new ArrayList<>();
    private double[] seedPose,lastPose;
    private Point3 target;
    private long previousTime,seedTime;
    private int width,height;
    private double fx,fy,cx,cy;
    public FrontMapTracker(){thread.start();worker=new Handler(thread.getLooper());}
    public synchronized boolean submit(final byte[] packet, final double[] pose,final boolean referenceGood, final int referenceEpoch){
        if(busy || closed)return false;
        busy=true;
        worker.post(()->{
            long seq=0,time=0; draftResult=null; frameEpoch=referenceEpoch;
            try {
                if(!loaded){if(!OpenCVLoader.initLocal())throw new IllegalStateException("OpenCV initLocal failed");loaded=true;growth=new Growth();}
                ByteBuffer b=ByteBuffer.wrap(packet).order(ByteOrder.LITTLE_ENDIAN);
                if(b.getInt()!=0x33465241)throw new IllegalArgumentException("packet magic");
                int w=b.getInt(),h=b.getInt();b.getInt();seq=b.getLong();time=b.getLong();
                double nfx=b.getFloat(),nfy=b.getFloat(),ncx=b.getFloat(),ncy=b.getFloat();
                if(w<=0 || h<=0 || packet.length!=48L+4L*w*h || !(nfx>0) || !(nfy>0))throw new IllegalArgumentException("packet size/intrinsics");
                if(previous!=null && (w!=width || h!=height || Math.abs(nfx-fx)>.1 || Math.abs(nfy-fy)>.1 || Math.abs(ncx-cx)>.1 || Math.abs(ncy-cy)>.1))clear();
                width=w;height=h;fx=nfx;fy=nfy;cx=ncx;cy=ncy;
                byte[] gray=new byte[w*h];
                for(int y=0;y<h;y++)for(int x=0;x<w;x++){
                    int i=48+((h-1-y)*w+x)*4;
                    gray[y*w+x]=(byte)(((packet[i]&255)*77+(packet[i+1]&255)*150+(packet[i+2]&255)*29)>>8);
                }
                Mat image=new Mat(h,w,CvType.CV_8UC1);image.put(0,0,gray);
                try{process(image,pose,referenceGood,seq,time);}finally{image.release();}
            } catch(Throwable e){lost=true;publish("ERROR: "+e.getClass().getSimpleName()+": "+e.getMessage(),seq,time,0,0,null);}
            finally {synchronized(this){if(draftResult!=null)result=draftResult;}busy=false;}
        });return true;
    }
    public synchronized String takeResult(){String s=result;result=null;return s;}
    public void reset(){worker.post(()->{clear();result=null;});}
    public void dispose(){closed=true;worker.post(()->{clear();thread.quit();});}
    private void clear(){if(previous!=null)previous.release();previous=null;pixels.clear();seedPixels.clear();world.clear();seedPose=lastPose=null;target=null;haveMap=lost=false;previousTime=seedTime=0;referenceLink.reset();if(growth!=null)growth.reset();}
    private void seed(Mat image,double[] pose,long time){
        if(previous!=null)previous.release();previous=image.clone();
        pixels.clear();pixels.addAll(growth.detect(image));
        seedPixels.clear();seedPixels.addAll(pixels);seedPose=pose.clone();seedTime=previousTime=time;
    }
    private void process(Mat image,double[] pose,boolean good,long seq,long time){
        if(lost){recover(image,pose,good,seq,time,false);return;}
        if(previous==null){
            if(!good){publish("WAIT_REAR_TRACKING",seq,time,0,0,null);return;}
            seed(image,pose,time);publish("MOVE_SIDEWAYS_8_TO_15_CM",seq,time,0,0,null);return;
        }
        if(time<=previousTime || time-previousTime>500){lost=true;if(growth!=null)growth.clearCandidates();recover(image,pose,good,seq,time,false);return;}
        if(!haveMap && !good){clear();publish("WAIT_REAR_TRACKING",seq,time,0,0,null);return;}
        ArrayList<Point> next=new ArrayList<>(), nextSeed=new ArrayList<>();ArrayList<Point3> nextWorld=new ArrayList<>();
        MatOfPoint2f p=new MatOfPoint2f(),q=new MatOfPoint2f(),back=new MatOfPoint2f();
        MatOfByte ok=new MatOfByte(),backOk=new MatOfByte();MatOfFloat err=new MatOfFloat(),backErr=new MatOfFloat();
        try{
            p.fromList(pixels);
            if(pixels.size()>=8){
                Video.calcOpticalFlowPyrLK(previous,image,p,q,ok,err,new Size(21,21),3);
                Video.calcOpticalFlowPyrLK(image,previous,q,back,backOk,backErr,new Size(21,21),3);
                Point[] a=q.toArray(),b=back.toArray();byte[] st=ok.toArray(),sb=backOk.toArray();
                for(int i=0;i<a.length;i++)if(st[i]!=0 && sb[i]!=0 && distance(pixels.get(i),b[i])<1 && a[i].x>=4 && a[i].y>=4 && a[i].x<width-4 && a[i].y<height-4){
                    next.add(a[i]);if(haveMap)nextWorld.add(world.get(i));else nextSeed.add(seedPixels.get(i));
                }
            }
        }finally{p.release();q.release();back.release();ok.release();backOk.release();err.release();backErr.release();}
        if(haveMap)growth.track(previous,image);
        previous.release();previous=image.clone();previousTime=time;
        pixels.clear();pixels.addAll(next);
        if(haveMap){
            world.clear();world.addAll(nextWorld);estimate(seq,time);
            if(lost){recover(image,pose,good,seq,time,true);return;}
            if(good)referenceLink.observe(lastPose,pose,frameEpoch);
            growth.afterPose(image,lastPose,time);
            publish("FRONT_PNP",seq,time,lastInliers,lastRms,lastPose);return;
        }
        seedPixels.clear();seedPixels.addAll(nextSeed);
        if(pixels.size()<40 || time-seedTime>8000){seed(image,pose,time);publish("RESEEDED - more texture / move sideways",seq,time,0,0,null);return;}
        double baseline=positionDistance(seedPose,pose);
        if(baseline<.08){publish("MOVE_SIDEWAYS_8_TO_15_CM",seq,time,0,baseline,null);return;}
        if(baseline>.3 || rotationAngle(seedPose,pose)>25){seed(image,pose,time);publish("RESEEDED - move slower, avoid turning",seq,time,0,baseline,null);return;}
        triangulate(pose);
        if(world.size()<30 || coverage(pixels)<5){seed(image,pose,time);world.clear();publish("MAP_REJECTED - need texture and parallax",seq,time,0,baseline,null);return;}
        haveMap=true;lastPose=pose.clone();
        int closest=0;double best=Double.MAX_VALUE;
        for(int i=0;i<pixels.size();i++){double d=distance(pixels.get(i),new Point(cx,cy));if(d<best){best=d;closest=i;}}
        target=world.get(closest);growth.afterPose(image,pose,time);publish("MAP_READY",seq,time,world.size(),baseline,pose);
    }
    private Mat projection(double[] c){
        Mat p=new Mat(3,4,CvType.CV_64F);double[] e=inverse(c),v=new double[12];
        for(int j=0;j<4;j++){v[j]=fx*e[j]+cx*e[8+j];v[4+j]=fy*e[4+j]+cy*e[8+j];v[8+j]=e[8+j];}p.put(0,0,v);return p;
    }
    private void triangulate(double[] current){
        Mat a=projection(seedPose),b=projection(current),u=new Mat(2,pixels.size(),CvType.CV_64F),v=new Mat(2,pixels.size(),CvType.CV_64F),out=new Mat();
        ArrayList<Point> kept=new ArrayList<>();
        try{
            for(int i=0;i<pixels.size();i++){u.put(0,i,seedPixels.get(i).x);u.put(1,i,seedPixels.get(i).y);v.put(0,i,pixels.get(i).x);v.put(1,i,pixels.get(i).y);}
            Calib3d.triangulatePoints(a,b,u,v,out);world.clear();
            double[] ea=inverse(seedPose),eb=inverse(current);
            for(int i=0;i<pixels.size();i++){
                double h=out.get(3,i)[0];if(Math.abs(h)<1e-9)continue;
                Point3 x=new Point3(out.get(0,i)[0]/h,out.get(1,i)[0]/h,out.get(2,i)[0]/h);
                Point3 xa=transform(ea,x),xb=transform(eb,x);
                if(!Double.isFinite(x.x+x.y+x.z) || xa.z<.2 || xb.z<.2 || xa.z>8 || xb.z>8)continue;
                if(distance(project(xa),seedPixels.get(i))>1.5 || distance(project(xb),pixels.get(i))>1.5)continue;
                double[] ra={x.x-seedPose[3],x.y-seedPose[7],x.z-seedPose[11]},rb={x.x-current[3],x.y-current[7],x.z-current[11]};
                double cos=dot(ra,rb)/Math.sqrt(dot(ra,ra)*dot(rb,rb));
                if(Math.acos(Math.max(-1,Math.min(1,cos)))<Math.toRadians(1.5))continue;
                world.add(x);kept.add(pixels.get(i));
            }
            pixels.clear();pixels.addAll(kept);
        }finally{a.release();b.release();u.release();v.release();out.release();}
    }
    private void estimate(long seq,long time){
        if(world.size()<20 || coverage(pixels)<5){lost=true;publish("LOST_FEATURES - Reset map",seq,time,0,0,null);return;}
        MatOfPoint3f xyz=new MatOfPoint3f();MatOfPoint2f uv=new MatOfPoint2f();MatOfDouble distortion=new MatOfDouble();
        Mat k=Mat.eye(3,3,CvType.CV_64F),r=new Mat(),t=new Mat(),inliers=new Mat(),rotation=new Mat();
        try{
            xyz.fromList(world);uv.fromList(pixels);k.put(0,0,fx);k.put(1,1,fy);k.put(0,2,cx);k.put(1,2,cy);
            boolean solved=Calib3d.solvePnPRansac(xyz,uv,k,distortion,r,t,false,150,2f,.995,inliers,Calib3d.SOLVEPNP_EPNP);
            if(!solved || inliers.rows()<20 || inliers.rows()<world.size()*.65){lost=true;publish("LOST_PNP - Reset map",seq,time,inliers.rows(),0,null);return;}
            ArrayList<Point3> iw=new ArrayList<>();ArrayList<Point> ip=new ArrayList<>();
            for(int i=0;i<inliers.rows();i++){int index=(int)inliers.get(i,0)[0];iw.add(world.get(index));ip.add(pixels.get(index));}
            xyz.fromList(iw);uv.fromList(ip);Calib3d.solvePnPRefineLM(xyz,uv,k,distortion,r,t);Calib3d.Rodrigues(r,rotation);
            double[] extrinsic=new double[16];extrinsic[15]=1;
            for(int i=0;i<3;i++){for(int j=0;j<3;j++)extrinsic[4*i+j]=rotation.get(i,j)[0];extrinsic[4*i+3]=t.get(i,0)[0];}
            double[] pose=inverse(extrinsic);double sum=0;boolean ahead=true;
            for(int i=0;i<iw.size();i++){Point3 x=transform(extrinsic,iw.get(i));ahead &= x.z>.05;double d=distance(project(x),ip.get(i));sum+=d*d;}
            double rms=Math.sqrt(sum/iw.size());
            if(!ahead || !Double.isFinite(rms) || rms>1.5 || coverage(ip)<5 || (!recovering && (positionDistance(lastPose,pose)>.2 || rotationAngle(lastPose,pose)>20))){
                lost=true;publish("LOST_QUALITY - Reset map",seq,time,iw.size(),rms,null);return;
            }
            world.clear();world.addAll(iw);pixels.clear();pixels.addAll(ip);lastPose=pose;lastRms=rms;lastInliers=iw.size();lost=false;
            publish("FRONT_PNP",seq,time,iw.size(),rms,pose);
        }finally{xyz.release();uv.release();distortion.release();k.release();r.release();t.release();inliers.release();rotation.release();}
    }
    private synchronized void publish(String state,long seq,long time,int inliers,double value,double[] pose){
        try{
            JSONObject j=new JSONObject();j.put("state",state).put("seq",seq).put("timeMs",time).put("points",pixels.size()).put("inliers",inliers).put("value",Double.isFinite(value)?value:0);
            j.put("added",growth==null?0:growth.added).put("keyframes",growth==null?0:growth.memory.size()).put("relocalized",growth==null?0:growth.relocalized).put("rearLinked",referenceLink.ready(frameEpoch));
            double[] link=referenceLink.transformFor(frameEpoch);
            j.put("referenceEpoch",frameEpoch);
            if(link!=null)j.put("mapFromRearWorld",new JSONArray(link));
            j.put("valid",pose!=null);if(pose!=null){JSONArray arr=new JSONArray();for(double x:pose)arr.put(x);j.put("pose",arr);}
            if(target!=null)j.put("target",new JSONArray(new double[]{target.x,target.y,target.z}));
            draftResult=j.toString();
        }catch(Exception e){draftResult="{\"state\":\"JSON_ERROR\",\"valid\":false}";}
    }
    private void recover(Mat image,double[] rear,boolean good,long seq,long time,boolean advanced){
        if(!haveMap){clear();publish("WAIT_REAR_TRACKING",seq,time,0,0,null);return;}
        if(!advanced){
            if(previous!=null && time>previousTime && time-previousTime<=500)growth.track(previous,image);
            else growth.clearCandidates();
            if(previous!=null)previous.release();previous=image.clone();previousTime=time;
        }
        if(growth.tryRecover(image,seq,time)){
            lost=false;growth.clearCandidates();
            if(good)referenceLink.observe(lastPose,rear,frameEpoch);
            growth.afterPose(image,lastPose,time);
            publish("FRONT_RELOCALIZED",seq,time,world.size(),lastRms,lastPose);return;
        }
        double[] fallback=good?referenceLink.predict(rear,frameEpoch):null;
        if(fallback!=null && lastPose!=null && positionDistance(lastPose,fallback)<.2 && rotationAngle(lastPose,fallback)<20){
            lastPose=fallback;world.clear();pixels.clear();
            growth.afterPose(image,fallback,time);
            if(world.size()>=30 && coverage(pixels)>=5){lost=false;publish("REAR_REBUILT",seq,time,world.size(),0,fallback);}
            else publish("REAR_FALLBACK",seq,time,0,0,fallback);
        }else{
            growth.clearCandidates();
            publish(good?"RELOCALIZING_REAR_NOT_LINKED":"RELOCALIZING",seq,time,0,0,null);
        }
    }

    /** Candidates are tracked independently of established landmarks and triangulated
     * only while a valid camera pose is available. Memory is bounded to 12 keyframes. */
    private final class Growth {
        final ArrayList<Point> first=new ArrayList<>(),current=new ArrayList<>();
        double[] startPose,lastSaved,recoveryPose;
        long startTime,savedAt,recoveryAt,confirmationAt;
        int added,relocalized,confirmations;
        final ArrayList<Keyframe> memory=new ArrayList<>();
        final ORB orb=ORB.create(400,1.2f,1,16,0,2,ORB.HARRIS_SCORE,31,10);
        final BFMatcher matcher=BFMatcher.create(Core.NORM_HAMMING,false);
        final class Keyframe {Mat descriptors;ArrayList<Point3> points;}
        void clearCandidates(){first.clear();current.clear();startPose=null;}
        void reset(){clearCandidates();for(Keyframe k:memory)k.descriptors.release();memory.clear();lastSaved=recoveryPose=null;added=relocalized=confirmations=0;savedAt=recoveryAt=confirmationAt=0;}
        void track(Mat before,Mat after){
            if(current.size()<8){clearCandidates();return;}
            MatOfPoint2f p=new MatOfPoint2f(),q=new MatOfPoint2f(),b=new MatOfPoint2f();
            MatOfByte st=new MatOfByte(),sb=new MatOfByte();MatOfFloat e=new MatOfFloat(),eb=new MatOfFloat();
            try{
                p.fromList(current);Video.calcOpticalFlowPyrLK(before,after,p,q,st,e,new Size(21,21),3);
                Video.calcOpticalFlowPyrLK(after,before,q,b,sb,eb,new Size(21,21),3);
                Point[] next=q.toArray(),back=b.toArray();byte[] ok=st.toArray(),ob=sb.toArray();
                ArrayList<Point> nf=new ArrayList<>(),nc=new ArrayList<>();
                for(int i=0;i<next.length;i++)if(ok[i]!=0 && ob[i]!=0 && distance(current.get(i),back[i])<1 && inside(next[i])){nf.add(first.get(i));nc.add(next[i]);}
                first.clear();first.addAll(nf);current.clear();current.addAll(nc);
            }finally{p.release();q.release();b.release();st.release();sb.release();e.release();eb.release();}
        }
        boolean inside(Point p){return p.x>=17 && p.y>=17 && p.x<width-17 && p.y<height-17;}
        ArrayList<Point> detect(Mat image){
            MatOfKeyPoint keys=new MatOfKeyPoint();Mat mask=new Mat();
            try{orb.detect(image,keys,mask);KeyPoint[] all=keys.toArray();Arrays.sort(all,(a,b)->Float.compare(b.response,a.response));
                ArrayList<Point> out=new ArrayList<>();
                for(KeyPoint k:all){if(!inside(k.pt))continue;boolean close=false;for(Point p:out)if(distance(p,k.pt)<6){close=true;break;}if(!close)out.add(k.pt);if(out.size()>=240)break;}
                return out;
            }finally{keys.release();mask.release();}
        }
        void begin(Mat image,double[] pose,long time){
            clearCandidates();startPose=pose.clone();startTime=time;
            for(Point p:detect(image)){boolean close=false;for(Point q:pixels)if(distance(p,q)<7){close=true;break;}
                if(!close){first.add(p);current.add(p);}if(current.size()>=160)break;}
        }
        void afterPose(Mat image,double[] pose,long time){
            if(startPose!=null && current.size()>=12 && time-startTime<=5000){
                double baseline=positionDistance(startPose,pose);
                if(baseline>=.06 && baseline<=.3 && rotationAngle(startPose,pose)<30){
                    // Reuse the exact triangulation gates used for initial map construction.
                    ArrayList<Point> active=new ArrayList<>(pixels),oldSeed=new ArrayList<>(seedPixels);
                    ArrayList<Point3> activeWorld=new ArrayList<>(world);double[] oldPose=seedPose;
                    ArrayList<Point> np;ArrayList<Point3> nw;
                    try{
                        pixels.clear();pixels.addAll(current);seedPixels.clear();seedPixels.addAll(first);seedPose=startPose;
                        triangulate(pose);np=new ArrayList<>(pixels);nw=new ArrayList<>(world);
                    }finally{pixels.clear();pixels.addAll(active);world.clear();world.addAll(activeWorld);seedPixels.clear();seedPixels.addAll(oldSeed);seedPose=oldPose;}
                    for(int i=0;i<np.size() && world.size()<240;i++){
                        boolean close=false;for(Point p:pixels)if(distance(p,np.get(i))<5){close=true;break;}
                        if(!close){pixels.add(np.get(i));world.add(nw.get(i));added++;}
                    }
                    clearCandidates();
                }else if(baseline>.3 || rotationAngle(startPose,pose)>=30)clearCandidates();
            }else clearCandidates();
            if(world.size()>=30 && time-savedAt>=1000 && (lastSaved==null || positionDistance(lastSaved,pose)>.06 || rotationAngle(lastSaved,pose)>8)){
                save(image);savedAt=time;lastSaved=pose.clone();
            }
            if(startPose==null && world.size()<220)begin(image,pose,time);
        }
        // Use the same intensity-centroid orientation for stored and detected keypoints.
        float orientation(byte[] image,Point p){
            int x=(int)Math.round(p.x),y=(int)Math.round(p.y);double mx=0,my=0;
            for(int dy=-15;dy<=15;dy++)for(int dx=-15;dx<=15;dx++)if(dx*dx+dy*dy<=225){double v=image[(y+dy)*width+x+dx]&255;mx+=dx*v;my+=dy*v;}
            double angle=Math.toDegrees(Math.atan2(my,mx));return (float)(angle<0?angle+360:angle);
        }
        Mat descriptors(Mat image,List<Point> points,MatOfKeyPoint keys){
            byte[] gray=new byte[width*height];image.get(0,0,gray);
            ArrayList<KeyPoint> kp=new ArrayList<>();for(int i=0;i<points.size();i++)if(inside(points.get(i))){Point p=points.get(i);kp.add(new KeyPoint((float)p.x,(float)p.y,31,orientation(gray,p),1,0,i));}
            keys.fromList(kp);Mat d=new Mat();orb.compute(image,keys,d);return d;
        }
        void save(Mat image){
            MatOfKeyPoint kp=new MatOfKeyPoint();Mat d=null;
            try{
                d=descriptors(image,pixels,kp);KeyPoint[] keys=kp.toArray();if(keys.length<30){d.release();return;}
                Keyframe k=new Keyframe();k.descriptors=d;k.points=new ArrayList<>();for(KeyPoint key:keys)k.points.add(world.get(key.class_id));
                memory.add(k);if(memory.size()>12){Keyframe old=memory.remove(1);old.descriptors.release();}
            }finally{kp.release();}
        }
        boolean tryRecover(Mat image,long seq,long time){
            if(time-recoveryAt<200 || memory.isEmpty())return false;
            recoveryAt=time;ArrayList<Point> detected=detect(image);MatOfKeyPoint kp=new MatOfKeyPoint();Mat descriptors=null;
            try{
                descriptors=descriptors(image,detected,kp);KeyPoint[] keys=kp.toArray();if(descriptors.empty()){confirmations=0;return false;}
                ArrayList<Point> bestPixels=null;ArrayList<Point3> bestWorld=null;int best=0;
                for(Keyframe k:memory){
                    ArrayList<MatOfDMatch> matches=new ArrayList<>();
                    try{
                        matcher.knnMatch(descriptors,k.descriptors,matches,2);
                        ArrayList<DMatch> usable=new ArrayList<>();for(MatOfDMatch m:matches){DMatch[] a=m.toArray();if(a.length==2 && a[0].distance<55 && a[0].distance<.7*a[1].distance)usable.add(a[0]);}
                        usable.sort((a,b)->Float.compare(a.distance,b.distance));HashSet<Integer> seen=new HashSet<>();
                        ArrayList<Point> ip=new ArrayList<>();ArrayList<Point3> iw=new ArrayList<>();
                        for(DMatch m:usable)if(seen.add(m.trainIdx)){ip.add(keys[m.queryIdx].pt);iw.add(k.points.get(m.trainIdx));}
                        if(ip.size()>best && coverage(ip)>=5){best=ip.size();bestPixels=ip;bestWorld=iw;}
                    }finally{for(MatOfDMatch m:matches)m.release();}
                }
                if(best<30){confirmations=0;return false;}
                world.clear();world.addAll(bestWorld);pixels.clear();pixels.addAll(bestPixels);
                double[] old=lastPose;recovering=true;
                try{estimate(seq,time);}finally{recovering=false;}
                if(lost || lastInliers<25 || lastRms>1.2){lost=true;lastPose=old;confirmations=0;return false;}
                double[] found=lastPose;lastPose=old;lost=true;
                if(recoveryPose!=null && time-confirmationAt<=700 && positionDistance(recoveryPose,found)<.10 && rotationAngle(recoveryPose,found)<10)confirmations++;
                else confirmations=1;
                recoveryPose=found;confirmationAt=time;
                if(confirmations<3)return false;
                lastPose=found;lost=false;confirmations=0;relocalized++;return true;
            }finally{kp.release();if(descriptors!=null)descriptors.release();}
        }
    }

    private int coverage(List<Point> pts){boolean[] cells=new boolean[12];for(Point p:pts){int x=Math.max(0,Math.min(2,(int)(p.x*3/width))),y=Math.max(0,Math.min(3,(int)(p.y*4/height)));cells[y*3+x]=true;}int n=0;for(boolean b:cells)if(b)n++;return n;}
    private Point project(Point3 p){return new Point(fx*p.x/p.z+cx,fy*p.y/p.z+cy);}
    private static double distance(Point a,Point b){return Math.hypot(a.x-b.x,a.y-b.y);}
    private static double dot(double[]a,double[]b){return a[0]*b[0]+a[1]*b[1]+a[2]*b[2];}
    private static double positionDistance(double[]a,double[]b){return Math.sqrt(Math.pow(a[3]-b[3],2)+Math.pow(a[7]-b[7],2)+Math.pow(a[11]-b[11],2));}
    private static double rotationAngle(double[]a,double[]b){double trace=0;for(int i=0;i<3;i++)for(int j=0;j<3;j++)trace+=a[i*4+j]*b[i*4+j];return Math.toDegrees(Math.acos(Math.max(-1,Math.min(1,(trace-1)/2))));}
    private static Point3 transform(double[] m,Point3 p){return new Point3(m[0]*p.x+m[1]*p.y+m[2]*p.z+m[3],m[4]*p.x+m[5]*p.y+m[6]*p.z+m[7],m[8]*p.x+m[9]*p.y+m[10]*p.z+m[11]);}
    private static double[] inverse(double[] m){double[] o=new double[16];o[15]=1;for(int i=0;i<3;i++){for(int j=0;j<3;j++)o[4*i+j]=m[4*j+i];o[4*i+3]=-(o[4*i]*m[3]+o[4*i+1]*m[7]+o[4*i+2]*m[11]);}return o;}
}
