package com.example.s25dualcamera;

import java.util.*;
import org.json.*;
import org.opencv.core.*;
import org.opencv.features2d.*;
import org.opencv.calib3d.Calib3d;
import org.opencv.video.Video;

/** Session-local metric map from known ARCore physical-camera poses.
 * All world coordinates are ARCore OpenGL session coordinates, NOT Unity world.
 * Only three-view verified, detected ORB features enter the localization map.
 * No prebuilt map, assumed feature depths, rear-to-front pose offsets or gyro fallback.
 */
public final class LiveMapEngine implements AutoCloseable {
    private static final int MAX_POINTS=1200, MAX_PENDING=1800, MIN_MAP=50;
    // Reference retention is NOT permission to display an old pose.
    private static final long RECHECK_NS=500000000L, FLOW_ONLY_NS=1000000000L;
    private static final int MAX_MISSES=2;
    private final ORB orb=ORB.create(2200,1.2f,8,31,0,2,ORB.HARRIS_SCORE,31,10);
    private final BFMatcher matcher=BFMatcher.create(Core.NORM_HAMMING,false);
    private final ArrayList<Landmark> map=new ArrayList<>(),pending=new ArrayList<>();
    private final ArrayList<Frame> keys=new ArrayList<>();
    private Frame lastRear;
    private Mat mapDesc=new Mat(); private int[] descIds=new int[0]; private boolean dirty=true,frozen;
    private Point3 target;
    private final Track[] tracks={new Track(),new Track()};
    private long epoch;
    private final boolean areaMode;
    private static final class Landmark {
        Point3 x; byte[] a,b; double[] origin,lastOrigin; long stamp; int confirmations;
        Landmark(Point3 x,byte[] a,byte[] b,double[] origin,double[] lastOrigin,long stamp){this.x=x;this.a=a;this.b=b;this.origin=origin;this.lastOrigin=lastOrigin;this.stamp=stamp;}
    }
    private static final class Frame implements AutoCloseable {
        Mat gray,desc; KeyPoint[] kp; double[] k,pose; int w,h;long seq,stamp;
        public void close(){gray.release();desc.release();}
    }
    private static final class Track {
        Mat gray; Point[] uv; int[] ids;double[] k,pose;long stamp,strongStamp;int good,misses;
        void clear(){if(gray!=null)gray.release();gray=null;uv=null;ids=null;k=null;pose=null;stamp=strongStamp=0;good=misses=0;}
    }
    private static final class Fit {boolean valid;String reason;Mat r,t;int[] ids;Point[] uv;double rms,holdout;double[] pose;int cells;}
    public LiveMapEngine(long epoch){this(epoch,false);}
    public LiveMapEngine(long epoch,boolean areaMode){this.epoch=epoch;this.areaMode=areaMode;}
    private Frame frame(long seq,long stamp,byte[] gray,int w,int h,double[] k,double[] pose){
        if(w<32||h<32||gray.length!=(long)w*h||k.length!=4||!(k[0]>0)||!(k[1]>0))throw new IllegalArgumentException("FRAME_SIZE_OR_K");
        for(double v:k)if(!Double.isFinite(v))throw new IllegalArgumentException("NONFINITE_K");
        Frame f=new Frame();f.seq=seq;f.stamp=stamp;f.w=w;f.h=h;f.k=k.clone();f.pose=pose==null?null:pose.clone();
        f.gray=new Mat(h,w,CvType.CV_8UC1);f.gray.put(0,0,gray);f.desc=new Mat();
        Mat mask=new Mat();MatOfKeyPoint kp=new MatOfKeyPoint();
        try{orb.detectAndCompute(f.gray,mask,kp,f.desc);f.kp=kp.toArray();}finally{mask.release();kp.release();}
        return f;
    }
    private JSONObject status(Frame f,int camera,String state,boolean valid)throws JSONException{
        return new JSONObject().put("epoch",epoch).put("camera",camera).put("seq",f.seq).put("timestampNs",f.stamp)
            .put("state",state).put("valid",valid).put("points",map.size()).put("pending",pending.size())
            .put("areaMode",areaMode).put("keyframes",keys.size()).put("frozen",frozen).put("hasTarget",target!=null);
    }
    public String process(int camera,long seq,long stamp,byte[] gray,int w,int h,double[] k,double[] pose)throws Exception{
        Frame f=frame(seq,stamp,gray,w,h,k,pose);boolean retained=false;
        try{
            if(camera==1 && !frozen){
                requirePose(pose);
                if(lastRear!=null && stamp<=lastRear.stamp)return status(f,camera,"STALE_REAR",false).toString();
                if(lastRear!=null && (distance(center(pose),center(lastRear.pose))>.6 || angle(pose,lastRear.pose)>50))
                    return status(f,camera,"REAR_POSE_JUMP_RESCAN",false).toString();
                confirmPending(f);
                boolean add=keys.isEmpty() || distance(center(pose),center(keys.get(keys.size()-1).pose))>.045;
                if(add){
                    // Only a few widely separated reference views per accepted keyframe.
                    int count=0;
                    for(int j=keys.size()-1;j>=0 && count<3;j--){Frame a=keys.get(j);double baseline=distance(center(a.pose),center(pose));
                        if(baseline>=.06 && baseline<=.8){triangulate(a,f);count++;}}
                    keys.add(f);retained=true;
                    while(keys.size()>8){Frame old=keys.remove(0);if(old!=lastRear)old.close();}
                }
                if(lastRear!=null && !keys.contains(lastRear))lastRear.close();
                lastRear=f;retained=true;
                return status(f,camera,map.size()>=MIN_MAP?"MAP_READY_PLACE_TARGET":"SCANNING_MOVE_SIDEWAYS",false).toString();
            }
            if(!frozen || target==null)return status(f,camera,"SCAN_AND_PLACE_WITH_REAR",false).toString();
            rebuild();
            Track t=tracks[camera];
            if(t.stamp>0 && f.stamp<=t.stamp)return status(f,camera,"STALE_FRAME",false).toString();
            if(t.stamp>0 && f.stamp-t.stamp>RECHECK_NS)t.clear();
            ArrayList<Integer> ids=new ArrayList<>();ArrayList<Point> uv=new ArrayList<>();HashSet<Integer> used=new HashSet<>();
            track(t,f,ids,uv,used);
            ArrayList<Integer> flowIds=new ArrayList<>(ids);ArrayList<Point> flowUv=new ArrayList<>(uv);
            addMatches(f,ids,uv,used);
            int minimum=areaMode?(camera==0?18:15):(camera==0?25:20);
            Fit fit=fit(ids,uv,f.k,f.w,f.h,minimum);
            String primaryReason=fit.reason;boolean flowOnly=false;
            // A fresh map search can introduce inconsistent matches even when known features track well.
            // Retry only those known features; SAME count, spread, holdout and reprojection gates.
            if(!fit.valid && t.good>=3 && t.strongStamp>0 && f.stamp-t.strongStamp<=FLOW_ONLY_NS && flowIds.size()>=minimum){
                releaseFit(fit);fit=fit(flowIds,flowUv,f.k,f.w,f.h,minimum);flowOnly=fit.valid;
            }
            try{
                if(!fit.valid){
                    String cause=fit.reason;
                    boolean retainReference=t.good>=3 && t.misses<MAX_MISSES && f.stamp-t.stamp<=RECHECK_NS;
                    JSONObject held=areaMode && retainReference?areaHold(t,f,flowIds,flowUv):null;
                    if(retainReference)t.misses++;else t.clear();
                    if(held!=null)return held.put("reason",cause).put("referenceRetained",true).toString();
                    return status(f,camera,retainReference?"RECHECKING":cause,false).put("reason",cause).put("primaryReason",primaryReason)
                        .put("matches",ids.size()).put("trackedFeatures",flowIds.size()).put("referenceRetained",retainReference).put("misses",t.misses).toString();
                }
                if(camera==1){
                    requirePose(pose);
                    if(distance(center(fit.pose),center(pose))>.08 || angle(fit.pose,pose)>6){t.clear();return status(f,camera,"REAR_MAP_ALIGNMENT_LOST_RESCAN",false).toString();}
                }
                double dt=(f.stamp-t.stamp)*1e-9;int confirmations=1;
                if(t.pose!=null && dt>0 && dt<=RECHECK_NS*1e-9){
                    if(distance(center(t.pose),center(fit.pose))>Math.min(.35,.04+2*dt) || angle(t.pose,fit.pose)>Math.min(40,8+180*dt)){
                        t.clear();return status(f,camera,"POSE_JUMP_HIDDEN",false).toString();}
                    confirmations=t.good+1;
                }
                long strongStamp=flowOnly?t.strongStamp:f.stamp;boolean recovered=t.misses>0;
                t.clear();t.gray=f.gray.clone();t.uv=fit.uv;t.ids=fit.ids;t.k=f.k.clone();t.pose=fit.pose;t.stamp=f.stamp;t.strongStamp=strongStamp;t.good=confirmations;
                boolean valid=confirmations>=3;Point p=project(fit.pose,f.k,target);
                JSONObject result=status(f,camera,valid?(areaMode?(flowOnly?"TRACKED_AREA_FLOW":"TRACKED_AREA"):(flowOnly?"TRACKED_FLOW":"TRACKED")):"VERIFYING_POSE",valid).put("inliers",fit.ids.length)
                    .put("matches",ids.size()).put("trackedFeatures",flowIds.size()).put("flowOnly",flowOnly).put("recoveredBriefGap",recovered)
                    .put("primaryReason",primaryReason).put("strongAgeMs",(f.stamp-strongStamp)/1000000.0)
                    .put("rms",fit.rms).put("holdout",fit.holdout).put("cells",fit.cells)
                    .put("confirmations",confirmations).put("worldToCamera",new JSONArray(fit.pose));
                boolean visible=p!=null && p.x>=0 && p.y>=0 && p.x<f.w && p.y<f.h;
                result.put("targetVisible",visible);
                if(p!=null)result.put("u",p.x).put("v",p.y).put("displayDepth",depth(fit.pose,target));
                result.put("target",new JSONArray(new double[]{target.x,target.y,target.z}));
                return result.toString();
            }finally{releaseFit(fit);}
        }finally{if(!retained)f.close();}
    }
    // Display-only bridge from the last verified image, never a new camera pose or map update.
    private JSONObject areaHold(Track t,Frame f,ArrayList<Integer> ids,ArrayList<Point> uv)throws JSONException{
        if(t.pose==null || t.good<3)return null;
        Point p=project(t.pose,t.k,target);if(p==null)return null;
        ArrayList<Point> before=new ArrayList<>(),after=new ArrayList<>();
        HashMap<Integer,Point> old=new HashMap<>();for(int i=0;i<t.ids.length;i++)old.put(t.ids[i],t.uv[i]);
        for(int i=0;i<ids.size();i++){Point a=old.get(ids.get(i));if(a!=null){before.add(a);after.add(uv.get(i));}}
        Point q=AreaMotion.bridge(p,before,after,f.stamp-t.stamp,f.w,f.h);
        if(q==null)return null;
        return status(f,fCamera(t),"AREA_HOLD",false).put("displayOnly",true).put("targetVisible",true)
            .put("u",q.x).put("v",q.y).put("displayDepth",depth(t.pose,target))
            .put("holdAgeMs",(f.stamp-t.stamp)/1000000.0).put("trackedFeatures",ids.size());
    }
    private int fCamera(Track t){return t==tracks[0]?0:1;}
    private static void releaseFit(Fit f){if(f.r!=null)f.r.release();if(f.t!=null)f.t.release();}
    public String placeAt(double[] point)throws Exception{
        if(frozen)return "TARGET_ALREADY_PLACED";
        if(lastRear==null || map.size()<MIN_MAP)return "MAP_NOT_READY";
        if(point==null || point.length!=3)return "NO_FLOOR_TARGET";
        Point3 p=new Point3(point[0],point[1],point[2]);if(!finite(p))return "INVALID_FLOOR_TARGET";
        double closest=Double.POSITIVE_INFINITY;for(Landmark l:map)closest=Math.min(closest,distance(l.x,p));
        if(closest>1.5)return "SCAN_NEAR_DALSOO";
        target=p;frozen=true;pending.clear();rebuild();return "TARGET_PLACED";
    }
    public void updateTarget(double[] point){
        if(!frozen || point==null || point.length!=3)return;
        Point3 p=new Point3(point[0],point[1],point[2]);if(!finite(p))throw new IllegalArgumentException("INVALID_FLOOR_TARGET");target=p;
    }
    public String place()throws Exception{
        if(frozen)return "TARGET_ALREADY_PLACED";
        if(lastRear==null || map.size()<MIN_MAP)return "MAP_NOT_READY";
        double best=Double.POSITIVE_INFINITY;Point3 selected=null;
        for(Landmark l:map){Point p=project(lastRear.pose,lastRear.k,l.x);if(p==null)continue;
            double score=Math.hypot((p.x-lastRear.w*.5)/lastRear.w,(p.y-lastRear.h*.5)/lastRear.h);
            if(score<best){best=score;selected=l.x;}}
        if(selected==null || best>.22)return "AIM_AT_MAPPED_FEATURE";
        target=new Point3(selected.x,selected.y+.06,selected.z);frozen=true;pending.clear();rebuild();return "TARGET_PLACED";
    }
    private void confirmPending(Frame f){
        if(pending.isEmpty() || f.desc.empty())return;
        Mat desc=new Mat(pending.size(),32,CvType.CV_8UC1);
        for(int i=0;i<pending.size();i++)desc.put(i,0,pending.get(i).b);
        ArrayList<MatOfDMatch> ms=new ArrayList<>();HashSet<Integer> found=new HashSet<>();
        try{matcher.knnMatch(f.desc,desc,ms,2);for(int qi=0;qi<ms.size();qi++){
            DMatch[] pair=ms.get(qi).toArray();if(pair.length<2||pair[0].distance>55||pair[0].distance>=.75*pair[1].distance)continue;
            int index=pair[0].trainIdx;if(!found.add(index))continue;Landmark l=pending.get(index);
            if(f.stamp<=l.stamp || distance(center(f.pose),l.lastOrigin)<.035)continue;
            Point p=project(f.pose,f.k,l.x);if(p==null || norm(p,f.kp[qi].pt)>1.8*Math.max(f.w,f.h)/640.0)continue;
            if(map.size()<MAX_POINTS && !nearExisting(l.x)){map.add(l);dirty=true;}l.confirmations=1;
        }}finally{for(Mat m:ms)m.release();desc.release();}
        pending.removeIf(l->l.confirmations>0 || (f.stamp-l.stamp)>8000000000L);
    }
    private boolean nearPending(Point3 x){for(Landmark l:pending)if(distance(l.x,x)<.02)return true;return false;}
    private boolean nearExisting(Point3 x){for(Landmark l:map)if(distance(l.x,x)<.012)return true;return false;}
    private void triangulate(Frame a,Frame b){
        if(a.desc.empty() || b.desc.empty() || pending.size()>=MAX_PENDING || map.size()>=MAX_POINTS)return;
        ArrayList<MatOfDMatch> forward=new ArrayList<>(),reverse=new ArrayList<>();
        Mat pa=projection(a.pose,a.k),pb=projection(b.pose,b.k);MatOfPoint2f ua=new MatOfPoint2f(),ub=new MatOfPoint2f();Mat xyzw=new Mat();
        try{
            matcher.knnMatch(a.desc,b.desc,forward,2);matcher.knnMatch(b.desc,a.desc,reverse,2);
            ArrayList<Point> av=new ArrayList<>(),bv=new ArrayList<>();ArrayList<int[]> pairs=new ArrayList<>();
            for(int i=0;i<forward.size();i++){DMatch[] m=forward.get(i).toArray();if(m.length<2||m[0].distance>55||m[0].distance>=.75*m[1].distance)continue;
                int j=m[0].trainIdx;DMatch[] back=reverse.get(j).toArray();if(back.length<2||back[0].trainIdx!=i||back[0].distance>=.75*back[1].distance)continue;
                av.add(a.kp[i].pt);bv.add(b.kp[j].pt);pairs.add(new int[]{i,j});}
            if(pairs.size()<10)return;ua.fromList(av);ub.fromList(bv);Calib3d.triangulatePoints(pa,pb,ua,ub,xyzw);
            double[] ca=center(a.pose),cb=center(b.pose);
            for(int i=0;i<pairs.size() && pending.size()<MAX_PENDING;i++){
                double w=xyzw.get(3,i)[0];if(Math.abs(w)<1e-8)continue;
                Point3 x=new Point3(xyzw.get(0,i)[0]/w,xyzw.get(1,i)[0]/w,xyzw.get(2,i)[0]/w);
                if(!finite(x)||depth(a.pose,x)<.2||depth(b.pose,x)<.2||depth(a.pose,x)>8||depth(b.pose,x)>8)continue;
                double parallax=rayAngle(ca,cb,x);if(parallax<2 || parallax>65)continue;
                Point p0=project(a.pose,a.k,x),p1=project(b.pose,b.k,x);
                if(p0==null||p1==null||norm(p0,av.get(i))>1.5||norm(p1,bv.get(i))>1.5||nearExisting(x)||nearPending(x))continue;
                byte[] da=new byte[32],db=new byte[32];a.desc.get(pairs.get(i)[0],0,da);b.desc.get(pairs.get(i)[1],0,db);
                pending.add(new Landmark(x,da,db,ca,cb,b.stamp));
            }
        }finally{for(Mat m:forward)m.release();for(Mat m:reverse)m.release();pa.release();pb.release();ua.release();ub.release();xyzw.release();}
    }
    private void rebuild(){if(!dirty)return;mapDesc.release();mapDesc=new Mat(map.size()*2,32,CvType.CV_8UC1);descIds=new int[map.size()*2];
        for(int i=0;i<map.size();i++){mapDesc.put(2*i,0,map.get(i).a);mapDesc.put(2*i+1,0,map.get(i).b);descIds[2*i]=descIds[2*i+1]=i;}dirty=false;}
    private void addMatches(Frame f,ArrayList<Integer> ids,ArrayList<Point> uv,Set<Integer> used){
        if(f.desc.empty()||mapDesc.empty())return;ArrayList<MatOfDMatch> matches=new ArrayList<>();
        try{matcher.knnMatch(f.desc,mapDesc,matches,Math.min(8,mapDesc.rows()));
            for(int i=0;i<matches.size();i++){DMatch[] ms=matches.get(i).toArray();if(ms.length<2)continue;DMatch best=ms[0],other=null;int id=descIds[best.trainIdx];
                for(int j=1;j<ms.length;j++)if(descIds[ms[j].trainIdx]!=id){other=ms[j];break;}
                if(other==null||best.distance>55||best.distance>=.75*other.distance||used.contains(id))continue;
                used.add(id);ids.add(id);uv.add(f.kp[i].pt);
            }
        }finally{for(Mat m:matches)m.release();}
    }
    private void track(Track t,Frame f,ArrayList<Integer> ids,ArrayList<Point> uv,Set<Integer> used){
        if(t.gray==null||f.stamp<=t.stamp||f.stamp-t.stamp>RECHECK_NS||t.gray.cols()!=f.w||t.gray.rows()!=f.h)return;
        for(int i=0;i<4;i++)if(Math.abs(f.k[i]-t.k[i])>Math.max(.5,.002*Math.abs(t.k[i])))return;
        MatOfPoint2f a=new MatOfPoint2f(t.uv),b=new MatOfPoint2f(),back=new MatOfPoint2f();MatOfByte good=new MatOfByte(),again=new MatOfByte();MatOfFloat err=new MatOfFloat(),err2=new MatOfFloat();
        try{Video.calcOpticalFlowPyrLK(t.gray,f.gray,a,b,good,err);Video.calcOpticalFlowPyrLK(f.gray,t.gray,b,back,again,err2);
            Point[] q=b.toArray(),r=back.toArray();byte[] g=good.toArray(),g2=again.toArray();
            for(int i=0;i<q.length;i++)if(g[i]!=0&&g2[i]!=0&&norm(r[i],t.uv[i])<1 && q[i].x>=0&&q[i].y>=0&&q[i].x<f.w&&q[i].y<f.h){ids.add(t.ids[i]);uv.add(q[i]);used.add(t.ids[i]);}
        }finally{a.release();b.release();back.release();good.release();again.release();err.release();err2.release();}
    }
    private Fit fit(ArrayList<Integer> ids,ArrayList<Point> uv,double[] k,int w,int h,int min){
        Fit f=new Fit();f.reason="SEARCHING_SCENE";if(ids.size()<min)return f;
        MatOfPoint3f xyz=new MatOfPoint3f();MatOfPoint2f image=new MatOfPoint2f();Mat K=intrinsics(k);MatOfDouble distortion=new MatOfDouble();Mat inliers=new Mat();
        f.r=new Mat();f.t=new Mat();double scale=Math.max(w,h)/640.0;
        try{
            ArrayList<Point3> points=new ArrayList<>();for(int id:ids)points.add(map.get(id).x);xyz.fromList(points);image.fromList(uv);
            boolean ok=Calib3d.solvePnPRansac(xyz,image,K,distortion,f.r,f.t,false,500,(float)((areaMode?3.5:2.5)*scale),.999,inliers,Calib3d.SOLVEPNP_EPNP);
            if(!ok||inliers.rows()<min||inliers.rows()<ids.size()*(areaMode?.55:.65)){f.reason="PNP_REJECTED";return f;}
            int n=inliers.rows();f.ids=new int[n];f.uv=new Point[n];ArrayList<Point3> good=new ArrayList<>();HashSet<Integer> cells=new HashSet<>();
            for(int i=0;i<n;i++){int ix=(int)inliers.get(i,0)[0];f.ids[i]=ids.get(ix);f.uv[i]=uv.get(ix);good.add(points.get(ix));int cx=(int)(f.uv[i].x*3/w),cy=(int)(f.uv[i].y*3/h);cells.add(cy*3+cx);}
            f.cells=cells.size();if(f.cells<(areaMode?3:4)){f.reason="FEATURES_TOO_CLUSTERED";return f;}
            xyz.fromList(good);image.fromArray(f.uv);Calib3d.solvePnPRefineLM(xyz,image,K,distortion,f.r,f.t);
            f.pose=pose(f.r,f.t);f.rms=rms(f.pose,k,good,Arrays.asList(f.uv));
            if(!Double.isFinite(f.rms)||f.rms>(areaMode?3:2)*scale){f.reason="REPROJECTION_REJECTED";return f;}
            for(Point3 p:good)if(depth(f.pose,p)<.1){f.reason="INVALID_DEPTH";return f;}
            ArrayList<Point3> train=new ArrayList<>(),test=new ArrayList<>();ArrayList<Point> trainUv=new ArrayList<>(),testUv=new ArrayList<>();
            for(int i=0;i<n;i++){if(i%3==0){test.add(good.get(i));testUv.add(f.uv[i]);}else{train.add(good.get(i));trainUv.add(f.uv[i]);}}
            Mat rr=new Mat(),tt=new Mat();
            try{xyz.fromList(train);image.fromList(trainUv);if(!Calib3d.solvePnP(xyz,image,K,distortion,rr,tt,false,Calib3d.SOLVEPNP_EPNP)){f.reason="HOLDOUT_SOLVE_FAILED";return f;}
                Calib3d.solvePnPRefineLM(xyz,image,K,distortion,rr,tt);double[] check=pose(rr,tt);f.holdout=rms(check,k,test,testUv);
                Point targetA=project(f.pose,k,target),targetB=project(check,k,target);
                if(!Double.isFinite(f.holdout)||f.holdout>(areaMode?4.5:3)*scale||targetA==null||targetB==null||norm(targetA,targetB)>(areaMode?8:5)*scale){f.reason="HOLDOUT_REJECTED";return f;}
            }finally{rr.release();tt.release();}
            f.valid=true;f.reason="VALID";return f;
        }finally{xyz.release();image.release();K.release();distortion.release();inliers.release();}
    }
    public String exportMap()throws JSONException{
        JSONObject j=new JSONObject().put("schema","s25-live-map-v1").put("coordinates","ARCore right-handed OpenGL session, meters").put("epoch",epoch).put("frozen",frozen);
        JSONArray a=new JSONArray();for(Landmark l:map){JSONArray da=new JSONArray(),db=new JSONArray();for(byte v:l.a)da.put(v&255);for(byte v:l.b)db.put(v&255);a.put(new JSONObject().put("xyz",new JSONArray(new double[]{l.x.x,l.x.y,l.x.z})).put("a",da).put("b",db));}j.put("landmarks",a);
        if(target!=null)j.put("target",new JSONArray(new double[]{target.x,target.y,target.z}));return j.toString();
    }
    public void close(){for(Track t:tracks)t.clear();for(Frame f:keys)f.close();if(lastRear!=null&&!keys.contains(lastRear))lastRear.close();keys.clear();lastRear=null;mapDesc.release();orb.clear();matcher.clear();}
    private static Mat intrinsics(double[] k){Mat m=Mat.eye(3,3,CvType.CV_64F);m.put(0,0,k[0]);m.put(1,1,k[1]);m.put(0,2,k[2]);m.put(1,2,k[3]);return m;}
    private static Mat projection(double[] p,double[] k){Mat m=new Mat(3,4,CvType.CV_64F);double[] a=new double[12];for(int j=0;j<4;j++){a[j]=k[0]*p[j]+k[2]*p[8+j];a[4+j]=k[1]*p[4+j]+k[3]*p[8+j];a[8+j]=p[8+j];}m.put(0,0,a);return m;}
    private static double[] pose(Mat rv,Mat tv){Mat r=new Mat();Calib3d.Rodrigues(rv,r);double[] p=new double[12];for(int i=0;i<3;i++){for(int j=0;j<3;j++)p[i*4+j]=r.get(i,j)[0];p[i*4+3]=tv.get(i,0)[0];}r.release();return p;}
    private static void requirePose(double[] p){if(p==null||p.length!=12)throw new IllegalArgumentException("NO_REAR_POSE");for(double v:p)if(!Double.isFinite(v))throw new IllegalArgumentException("NONFINITE_POSE");}
    private static double[] center(double[] p){return new double[]{-(p[0]*p[3]+p[4]*p[7]+p[8]*p[11]),-(p[1]*p[3]+p[5]*p[7]+p[9]*p[11]),-(p[2]*p[3]+p[6]*p[7]+p[10]*p[11])};}
    private static double depth(double[] p,Point3 x){return p[8]*x.x+p[9]*x.y+p[10]*x.z+p[11];}
    private static Point project(double[] p,double[] k,Point3 x){if(x==null)return null;double z=depth(p,x);if(z<=.05)return null;return new Point(k[0]*(p[0]*x.x+p[1]*x.y+p[2]*x.z+p[3])/z+k[2],k[1]*(p[4]*x.x+p[5]*x.y+p[6]*x.z+p[7])/z+k[3]);}
    private static double rms(double[] p,double[] k,List<Point3> x,List<Point> uv){double e=0;for(int i=0;i<x.size();i++){Point q=project(p,k,x.get(i));if(q==null)return Double.POSITIVE_INFINITY;double v=norm(q,uv.get(i));e+=v*v;}return Math.sqrt(e/x.size());}
    private static boolean finite(Point3 x){return Double.isFinite(x.x)&&Double.isFinite(x.y)&&Double.isFinite(x.z);}
    private static double norm(Point a,Point b){return Math.hypot(a.x-b.x,a.y-b.y);}
    private static double distance(double[] a,double[] b){return Math.sqrt(Math.pow(a[0]-b[0],2)+Math.pow(a[1]-b[1],2)+Math.pow(a[2]-b[2],2));}
    private static double distance(Point3 a,Point3 b){return Math.sqrt(Math.pow(a.x-b.x,2)+Math.pow(a.y-b.y,2)+Math.pow(a.z-b.z,2));}
    private static double angle(double[] a,double[] b){double tr=0;for(int i=0;i<3;i++)for(int j=0;j<3;j++)tr+=a[4*i+j]*b[4*i+j];return Math.toDegrees(Math.acos(Math.max(-1,Math.min(1,(tr-1)/2))));}
    private static double rayAngle(double[] a,double[] b,Point3 x){double[] u={x.x-a[0],x.y-a[1],x.z-a[2]},v={x.x-b[0],x.y-b[1],x.z-b[2]};double dot=0,uu=0,vv=0;for(int i=0;i<3;i++){dot+=u[i]*v[i];uu+=u[i]*u[i];vv+=v[i]*v[i];}return Math.toDegrees(Math.acos(Math.max(-1,Math.min(1,dot/Math.sqrt(uu*vv)))));}
}
