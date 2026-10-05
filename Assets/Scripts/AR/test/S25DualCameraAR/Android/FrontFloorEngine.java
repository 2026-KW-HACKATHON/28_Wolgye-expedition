package com.example.s25dualcamera;
import java.util.*;
import org.json.*;
import org.opencv.core.*;
import org.opencv.features2d.*;
import org.opencv.calib3d.Calib3d;
import org.opencv.video.Video;

/** Rear-verified floor appearance -> automatic nearby placement -> front-only planar tracking.
 * This recognizes a previously observed physical plane, not arbitrary semantic floor pixels.
 */
public final class FrontFloorEngine implements AutoCloseable {
 private final long epoch;private final ORB orb=ORB.create(1800);private final BFMatcher matcher=BFMatcher.create(Core.NORM_HAMMING,false);
 private double[] floor;private double[] rearCenter,previousPose;private long stamp;private int good;
 private final ArrayList<Feature> seeds=new ArrayList<>(),map=new ArrayList<>();
 private Mat previous;private Point[] previousUv;private Point[] previousXy;private double[] previousK;
 private Point locked;private Point3 target;
 private static final class Feature {Point xy;byte[] d;Feature(Point p,byte[] d){xy=p;this.d=d;}}
 private static final class Fit {double[] pose;Point[] xy,uv;double rms;}
 public FrontFloorEngine(long epoch){this.epoch=epoch;}
 private JSONObject status(int camera,long seq,long ns,String state,boolean valid)throws JSONException{
  return new JSONObject().put("epoch",epoch).put("camera",camera).put("seq",seq).put("timestampNs",ns).put("state",state).put("valid",valid)
   .put("floorMode",true).put("points",map.size()).put("pending",seeds.size()).put("frozen",false).put("hasTarget",target!=null);
 }
 public String process(int camera,long seq,long ns,byte[] bytes,int w,int h,double[] k,double[] pose,double[] plane,double[] actor)throws Exception{
  if(bytes.length!=(long)w*h || k.length!=4 || k[0]<=0 || k[1]<=0)throw new IllegalArgumentException("BAD_FLOOR_FRAME");
  for(double v:k)if(!Double.isFinite(v))throw new IllegalArgumentException("BAD_FLOOR_K");
  if(floor==null && camera==1 && validPlane(plane))floor=plane.clone();
  if(floor==null)return status(camera,seq,ns,"WAITING_REAR_FLOOR",false).toString();
  Mat gray=new Mat(h,w,CvType.CV_8UC1),desc=new Mat(),mask=new Mat();MatOfKeyPoint keys=new MatOfKeyPoint();gray.put(0,0,bytes);
  try{
   orb.detectAndCompute(gray,mask,keys,desc);KeyPoint[] kp=keys.toArray();
   if(camera==1){
    requirePose(pose);collect(kp,desc,k,pose,w,h);resetTrack();locked=null;target=null;
    JSONObject out=status(camera,seq,ns,map.size()>=16?"FLOOR_READY_SHOW_FRONT":"FLOOR_SCAN_MOVE_SIDEWAYS",true);
    return display(out,pose,actor==null||actor.length!=3?world(new Point(0,0),floor[9]):new Point3(actor[0],actor[1],actor[2]),k,w,h).toString();
   }
   if(ns<=stamp)return status(camera,seq,ns,"STALE_FRONT_FRAME",false).toString();
   if(stamp>0 && ns-stamp>600000000L)resetTrack();
   ArrayList<Point> xy=new ArrayList<>(),uv=new ArrayList<>();
   flow(gray,k,ns,xy,uv);String source="FLOW";
   Fit fit=fit(xy,uv,k,w,h);
   if(fit==null){xy.clear();uv.clear();matches(kp,desc,xy,uv);fit=fit(xy,uv,k,w,h);source="RECOGNITION";}
   if(fit==null){good=0;return status(camera,seq,ns,"SEARCHING_FLOOR",false).put("matches",xy.size()).toString();}
   if(previousPose!=null && stamp>0 && ns-stamp<=600000000L){
    double dt=(ns-stamp)*1e-9;
    if(distance(center(previousPose),center(fit.pose))>Math.min(.45,.06+2*dt) || angle(previousPose,fit.pose)>Math.min(45,10+180*dt)){
     resetTrack();return status(camera,seq,ns,"FLOOR_POSE_JUMP",false).toString();
    }
   }
   good++;stamp=ns;previousPose=fit.pose;previousK=k.clone();if(previous!=null)previous.release();previous=gray.clone();previousUv=fit.uv;previousXy=fit.xy;
   if(good<3)return status(camera,seq,ns,"VERIFYING_FLOOR",false).put("inliers",fit.xy.length).toString();
   if(locked==null){
    double x=0,y=0;for(Point p:fit.xy){x+=p.x;y+=p.y;}Point candidate=new Point(x/fit.xy.length,y/fit.xy.length);
    if(!inside(candidate))return status(camera,seq,ns,"FLOOR_NOT_SAFE",false).toString();
    locked=candidate;target=world(locked,floor[9]);
   }
   // Refresh descriptors only for points whose metric floor correspondence passed current geometry.
   if(source.equals("FLOW"))refresh(kp,desc,fit);
   return display(status(camera,seq,ns,"FRONT_FLOOR_LOCKED",true).put("inliers",fit.xy.length).put("rms",fit.rms).put("source",source),fit.pose,target,k,w,h).toString();
  }finally{gray.release();desc.release();mask.release();keys.release();}
 }
 private void collect(KeyPoint[] kp,Mat desc,double[] k,double[] pose,int w,int h){
  double[] c=center(pose);double scale=Math.max(w,h)/640.;
  if(rearCenter!=null && distance(c,rearCenter)<.06)return;
  if(!seeds.isEmpty() && !desc.empty()){
   Mat sd=new Mat(seeds.size(),32,CvType.CV_8UC1);ArrayList<MatOfDMatch> found=new ArrayList<>();
   try{for(int i=0;i<seeds.size();i++)sd.put(i,0,seeds.get(i).d);matcher.knnMatch(sd,desc,found,2);Set<Integer> used=new HashSet<>();
    for(int i=0;i<found.size();i++){DMatch[] m=found.get(i).toArray();if(m.length<2||m[0].distance>55||m[0].distance>=.72*m[1].distance||!used.add(m[0].trainIdx))continue;
     Feature f=seeds.get(i);Point q=project(pose,k,world(f.xy,0));if(q==null||norm(q,kp[m[0].trainIdx].pt)>2*scale)continue;
     boolean duplicate=false;for(Feature a:map)if(norm(a.xy,f.xy)<.015){duplicate=true;break;}
     if(!duplicate && map.size()<1000)map.add(f);
    }
   }finally{sd.release();for(Mat m:found)m.release();}
  }
  seeds.clear();rearCenter=c;
  for(int i=0;i<kp.length;i++){Point p=intersect(kp[i].pt,k,pose);if(p==null||!inside(p))continue;byte[] d=new byte[32];desc.get(i,0,d);seeds.add(new Feature(p,d));}
 }
 private void matches(KeyPoint[] kp,Mat desc,List<Point> xy,List<Point> uv){
  if(map.size()<16 || desc.empty())return;Mat md=new Mat(map.size(),32,CvType.CV_8UC1);ArrayList<MatOfDMatch> ms=new ArrayList<>();
  try{for(int i=0;i<map.size();i++)md.put(i,0,map.get(i).d);matcher.knnMatch(desc,md,ms,2);Set<Integer> used=new HashSet<>();
   for(int i=0;i<ms.size();i++){DMatch[] m=ms.get(i).toArray();if(m.length<2||m[0].distance>55||m[0].distance>=.72*m[1].distance||!used.add(m[0].trainIdx))continue;xy.add(map.get(m[0].trainIdx).xy);uv.add(kp[i].pt);}
  }finally{md.release();for(Mat m:ms)m.release();}
 }
 private void flow(Mat gray,double[] k,long ns,List<Point> xy,List<Point> uv){
  if(previous==null||ns<=stamp||ns-stamp>600000000L||previous.size().width!=gray.cols()||previous.size().height!=gray.rows())return;
  for(int i=0;i<4;i++)if(Math.abs(k[i]-previousK[i])>Math.max(.5,.002*Math.abs(k[i])))return;
  MatOfPoint2f a=new MatOfPoint2f(previousUv),b=new MatOfPoint2f(),back=new MatOfPoint2f();MatOfByte ok=new MatOfByte(),ok2=new MatOfByte();MatOfFloat err=new MatOfFloat(),err2=new MatOfFloat();
  try{Video.calcOpticalFlowPyrLK(previous,gray,a,b,ok,err);Video.calcOpticalFlowPyrLK(gray,previous,b,back,ok2,err2);Point[] q=b.toArray(),r=back.toArray();byte[] yes=ok.toArray(),again=ok2.toArray();
   for(int i=0;i<q.length;i++)if(yes[i]!=0&&again[i]!=0&&norm(r[i],previousUv[i])<1&&q[i].x>=0&&q[i].y>=0&&q[i].x<gray.cols()&&q[i].y<gray.rows()){xy.add(previousXy[i]);uv.add(q[i]);}
  }finally{a.release();b.release();back.release();ok.release();ok2.release();err.release();err2.release();}
 }
 private Fit fit(List<Point> xy,List<Point> uv,double[] k,int w,int h){
  if(xy.size()<16)return null;
  MatOfPoint2f a=new MatOfPoint2f(),b=new MatOfPoint2f();Mat mask=new Mat(),H=null,K=Mat.eye(3,3,CvType.CV_64F),r=new Mat(),t=new Mat(),R=new Mat();MatOfDouble distortion=new MatOfDouble();MatOfPoint3f xyz=new MatOfPoint3f();
  try{
   a.fromList(xy);b.fromList(uv);double scale=Math.max(w,h)/640.;H=Calib3d.findHomography(a,b,Calib3d.RANSAC,3*scale,mask,1500,.995);if(H.empty())return null;
   ArrayList<Point> goodX=new ArrayList<>(),goodU=new ArrayList<>();ArrayList<Point3> points=new ArrayList<>();Set<Integer> cells=new HashSet<>();
   double minX=w,maxX=0,minY=h,maxY=0;
   for(int i=0;i<xy.size();i++)if(mask.get(i,0)[0]!=0){Point q=uv.get(i);goodX.add(xy.get(i));goodU.add(q);points.add(world(xy.get(i),0));cells.add((int)(q.x*4/w)+4*(int)(q.y*4/h));minX=Math.min(minX,q.x);maxX=Math.max(maxX,q.x);minY=Math.min(minY,q.y);maxY=Math.max(maxY,q.y);}
   if(goodX.size()<16||goodX.size()<xy.size()*.6||cells.size()<3||(maxX-minX)*(maxY-minY)<w*h*.015)return null;
   double[] x=column(H,0,k),y=column(H,1,k),tr=column(H,2,k);double factor=2/(length(x)+length(y));if(tr[2]<0)factor=-factor;
   for(int i=0;i<3;i++){x[i]*=factor;y[i]*=factor;tr[i]*=factor;}normalize(x);double dot=dot(x,y);for(int i=0;i<3;i++)y[i]-=dot*x[i];normalize(y);double[] z=cross(x,y),normal=normal();
   double[] rotation=new double[9],translation=new double[3];for(int i=0;i<3;i++){for(int j=0;j<3;j++)rotation[i*3+j]=x[i]*floor[3+j]+y[i]*floor[6+j]+z[i]*normal[j];translation[i]=tr[i]-rotation[i*3]*floor[0]-rotation[i*3+1]*floor[1]-rotation[i*3+2]*floor[2];}
   R.create(3,3,CvType.CV_64F);R.put(0,0,rotation);Calib3d.Rodrigues(R,r);t.create(3,1,CvType.CV_64F);t.put(0,0,translation);K.put(0,0,k[0]);K.put(1,1,k[1]);K.put(0,2,k[2]);K.put(1,2,k[3]);xyz.fromList(points);b.fromList(goodU);Calib3d.solvePnPRefineLM(xyz,b,K,distortion,r,t);Calib3d.Rodrigues(r,R);
   double[] pose=new double[12];for(int i=0;i<3;i++){for(int j=0;j<3;j++)pose[i*4+j]=R.get(i,j)[0];pose[i*4+3]=t.get(i,0)[0];}
   double[] c=center(pose);double height=0;for(int i=0;i<3;i++)height+=(c[i]-floor[i])*normal[i];if(height<.15||height>3.5)return null;
   double error=0;for(int i=0;i<points.size();i++){Point p=project(pose,k,points.get(i));if(p==null)return null;double e=norm(p,goodU.get(i));error+=e*e;}double rms=Math.sqrt(error/points.size());if(!Double.isFinite(rms)||rms>3*scale)return null;
   Fit f=new Fit();f.pose=pose;f.xy=goodX.toArray(new Point[0]);f.uv=goodU.toArray(new Point[0]);f.rms=rms;return f;
  }catch(RuntimeException e){return null;}
  finally{a.release();b.release();mask.release();if(H!=null)H.release();K.release();r.release();t.release();R.release();distortion.release();xyz.release();}
 }
 private void refresh(KeyPoint[] kp,Mat desc,Fit fit){
  // Refresh appearance of already verified locations; never invent metric depth for arbitrary new pixels.
  for(int i=0;i<fit.xy.length;i++){int best=-1;double distance=1.5;for(int j=0;j<kp.length;j++){double d=norm(kp[j].pt,fit.uv[i]);if(d<distance){distance=d;best=j;}}
   if(best<0)continue;for(Feature f:map)if(norm(f.xy,fit.xy[i])<.005){byte[] bytes=new byte[32];desc.get(best,0,bytes);f.d=bytes;break;}}
 }
 private JSONObject display(JSONObject out,double[] pose,Point3 p,double[] k,int w,int h)throws JSONException{
  Point uv=project(pose,k,p);out.put("worldToCamera",new JSONArray(pose)).put("target",new JSONArray(new double[]{p.x,p.y,p.z}));
  out.put("targetVisible",uv!=null&&uv.x>=0&&uv.y>=0&&uv.x<w&&uv.y<h);if(uv!=null)out.put("u",uv.x).put("v",uv.y).put("displayDepth",pose[8]*p.x+pose[9]*p.y+pose[10]*p.z+pose[11]);return out;
 }
 private Point intersect(Point p,double[] k,double[] pose){double[] c=center(pose),ray={(p.x-k[2])/k[0],(p.y-k[3])/k[1],1},d=new double[3],n=normal();for(int j=0;j<3;j++)for(int i=0;i<3;i++)d[j]+=pose[4*i+j]*ray[i];double den=dot(n,d);if(Math.abs(den)<.15)return null;double numerator=0;for(int i=0;i<3;i++)numerator+=n[i]*(floor[i]-c[i]);double depth=numerator/den;if(depth<.2||depth>6)return null;double u=0,v=0;for(int i=0;i<3;i++){double a=c[i]+depth*d[i]-floor[i];u+=a*floor[3+i];v+=a*floor[6+i];}return new Point(u,v);}
 private boolean inside(Point p){int count=(int)floor[10];boolean inside=false;for(int i=0,j=count-1;i<count;j=i++){double x=floor[11+i*2],y=floor[12+i*2],a=floor[11+j*2],b=floor[12+j*2];if((y>p.y)!=(b>p.y)&&p.x<(a-x)*(p.y-y)/(b-y)+x)inside=!inside;}return inside;}
 private Point3 world(Point p,double offset){double[] n=normal();return new Point3(floor[0]+floor[3]*p.x+floor[6]*p.y+n[0]*offset,floor[1]+floor[4]*p.x+floor[7]*p.y+n[1]*offset,floor[2]+floor[5]*p.x+floor[8]*p.y+n[2]*offset);}
 private double[] normal(){return cross(new double[]{floor[3],floor[4],floor[5]},new double[]{floor[6],floor[7],floor[8]});}
 private static boolean validPlane(double[] p){if(p==null||p.length<17||p[10]<3||p[10]>1024||p[10]!=(int)p[10]||p.length!=11+2*(int)p[10])return false;for(double v:p)if(!Double.isFinite(v))return false;double[] u={p[3],p[4],p[5]},v={p[6],p[7],p[8]};return Math.abs(length(u)-1)<.01&&Math.abs(length(v)-1)<.01&&Math.abs(dot(u,v))<.01;}
 private static double[] column(Mat h,int j,double[] k){double z=h.get(2,j)[0];return new double[]{(h.get(0,j)[0]-k[2]*z)/k[0],(h.get(1,j)[0]-k[3]*z)/k[1],z};}
 private static Point project(double[] p,double[] k,Point3 a){double z=p[8]*a.x+p[9]*a.y+p[10]*a.z+p[11];return z<=.05?null:new Point(k[0]*(p[0]*a.x+p[1]*a.y+p[2]*a.z+p[3])/z+k[2],k[1]*(p[4]*a.x+p[5]*a.y+p[6]*a.z+p[7])/z+k[3]);}
 private static double[] center(double[] p){return new double[]{-(p[0]*p[3]+p[4]*p[7]+p[8]*p[11]),-(p[1]*p[3]+p[5]*p[7]+p[9]*p[11]),-(p[2]*p[3]+p[6]*p[7]+p[10]*p[11])};}
 private static void requirePose(double[] p){if(p==null||p.length!=12)throw new IllegalArgumentException("NO_REAR_POSE");for(double x:p)if(!Double.isFinite(x))throw new IllegalArgumentException("INVALID_REAR_POSE");}
 private static double dot(double[] a,double[] b){double s=0;for(int i=0;i<3;i++)s+=a[i]*b[i];return s;}
 private static double[] cross(double[] a,double[] b){return new double[]{a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]};}
 private static double length(double[] a){return Math.sqrt(dot(a,a));}private static void normalize(double[] a){double n=length(a);for(int i=0;i<a.length;i++)a[i]/=n;}
 private static double norm(Point a,Point b){return Math.hypot(a.x-b.x,a.y-b.y);}private static double distance(double[] a,double[] b){double s=0;for(int i=0;i<3;i++)s+=(a[i]-b[i])*(a[i]-b[i]);return Math.sqrt(s);}
 private static double angle(double[] a,double[] b){double t=0;for(int i=0;i<3;i++)for(int j=0;j<3;j++)t+=a[i*4+j]*b[i*4+j];return Math.toDegrees(Math.acos(Math.max(-1,Math.min(1,(t-1)/2))));}
 private void resetTrack(){if(previous!=null)previous.release();previous=null;previousPose=null;previousK=null;stamp=0;good=0;previousUv=null;previousXy=null;}
 public void close(){resetTrack();orb.clear();matcher.clear();map.clear();seeds.clear();}
}
