package com.example.s25dualcamera;

/** Conservative coordinate hand-off gate; contains no Android/OpenCV dependencies. */
public final class ReferenceLink {
    private double[] candidate, accepted;
    private int epoch=-1, count;
    public void reset(){candidate=accepted=null;epoch=-1;count=0;}
    public void observe(double[] mapCamera,double[] rearCamera,int currentEpoch){
        if(epoch!=currentEpoch){reset();epoch=currentEpoch;}
        double[] next=multiply(mapCamera,inverse(rearCamera));
        if(candidate==null || distance(candidate,next)>.03 || angle(candidate,next)>3){
            candidate=next;count=1;
            // A small single-frame disagreement must not destroy a confirmed link.
            if(accepted!=null && (distance(accepted,next)>.25 || angle(accepted,next)>15))accepted=null;
            return;
        }
        candidate=next;
        if(++count>=5)accepted=next.clone();
    }
    public double[] predict(double[] rearCamera,int currentEpoch){return accepted!=null && currentEpoch==epoch?multiply(accepted,rearCamera):null;}
    public double[] transformFor(int currentEpoch){return ready(currentEpoch)?accepted.clone():null;}
    public String status(int currentEpoch){return epoch!=currentEpoch?"EPOCH_CHANGED":accepted==null?"VERIFYING_"+count+"_OF_5":count<5?"HELD_RECHECKING":"CONFIRMED";}
    public boolean ready(int currentEpoch){return accepted!=null && currentEpoch==epoch;}
    public static double[] multiply(double[] a,double[] b){double[] o=new double[16];for(int i=0;i<4;i++)for(int j=0;j<4;j++)for(int k=0;k<4;k++)o[i*4+j]+=a[i*4+k]*b[k*4+j];return o;}
    public static double[] inverse(double[] m){double[] o=new double[16];o[15]=1;for(int i=0;i<3;i++){for(int j=0;j<3;j++)o[4*i+j]=m[4*j+i];o[4*i+3]=-(o[4*i]*m[3]+o[4*i+1]*m[7]+o[4*i+2]*m[11]);}return o;}
    public static double distance(double[] a,double[] b){double s=0;for(int i=3;i<12;i+=4)s+=(a[i]-b[i])*(a[i]-b[i]);return Math.sqrt(s);}
    public static double angle(double[] a,double[] b){double t=0;for(int i=0;i<3;i++)for(int j=0;j<3;j++)t+=a[i*4+j]*b[i*4+j];return Math.toDegrees(Math.acos(Math.max(-1,Math.min(1,(t-1)/2))));}
}
