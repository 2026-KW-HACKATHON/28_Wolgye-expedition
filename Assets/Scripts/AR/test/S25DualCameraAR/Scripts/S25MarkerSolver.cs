using System;
using System.Collections.Generic;

// Independent of Unity: pinhole reprojection with 7 unknowns (rotation vector,
// lens translation in nominal front axes, log focal scale). No lens distortion model.
public static class S25MarkerSolver
{
    public struct Point
    {
        public double x,y,z,u,v,fx,fy,cx,cy;
    }
    public static double[] Residual(List<Point> points, double[] a)
    {
        var r = new double[points.Count * 2];
        double angle=Math.Sqrt(a[0]*a[0]+a[1]*a[1]+a[2]*a[2]);
        double kx=0,ky=0,kz=0;
        if(angle>1e-12) { kx=a[0]/angle; ky=a[1]/angle; kz=a[2]/angle; }
        double c=Math.Cos(angle), s=-Math.Sin(angle), scale=Math.Exp(a[6]);
        for(int i=0;i<points.Count;i++)
        {
            var p=points[i]; double x=p.x-a[3], y=p.y-a[4], z=p.z-a[5];
            double dot=kx*x+ky*y+kz*z;
            double qx=x*c+(ky*z-kz*y)*s+kx*dot*(1-c);
            double qy=y*c+(kz*x-kx*z)*s+ky*dot*(1-c);
            double qz=z*c+(kx*y-ky*x)*s+kz*dot*(1-c);
            if(qz<=0.03) { r[2*i]=r[2*i+1]=10000+1000*(0.03-qz); continue; }
            r[2*i]=p.fx*scale*qx/qz+p.cx-p.u;
            r[2*i+1]=p.cy-p.fy*scale*qy/qz-p.v;
        }
        return r;
    }
    static double Cost(double[] r) { double s=0; foreach(double v in r)s+=v*v; return s; }
    public static double Rms(List<Point> points,double[] a) => Math.Sqrt(Cost(Residual(points,a))/Math.Max(1,points.Count));
    public static double[] Fit(List<Point> points)
    {
        double[] best=null; double bestCost=double.PositiveInfinity;
        for(int seed=0;seed<4;seed++)
        {
            var a=new double[7]; a[2]=seed*Math.PI/2;
            double lambda=0.01;
            for(int iter=0;iter<100;iter++)
            {
                var r=Residual(points,a); double cost=Cost(r); var j=new double[r.Length,7];
                for(int k=0;k<7;k++)
                {
                    var b=(double[])a.Clone(); b[k]+=1e-5; var rb=Residual(points,b);
                    for(int i=0;i<r.Length;i++)j[i,k]=(rb[i]-r[i])/1e-5;
                }
                var h=new double[7,8];
                for(int k=0;k<7;k++)
                {
                    for(int l=0;l<7;l++)for(int i=0;i<r.Length;i++)h[k,l]+=j[i,k]*j[i,l];
                    for(int i=0;i<r.Length;i++)h[k,7]-=j[i,k]*r[i];
                    h[k,k]+=lambda*(h[k,k]+1);
                }
                var delta=Solve(h); if(delta==null)break;
                var trial=(double[])a.Clone();
                for(int k=0;k<7;k++)trial[k]+=delta[k];
                for(int k=3;k<6;k++)trial[k]=Math.Max(-0.15,Math.Min(0.15,trial[k]));
                trial[6]=Math.Max(Math.Log(0.6),Math.Min(Math.Log(1.6),trial[6]));
                double trialCost=Cost(Residual(points,trial));
                if(trialCost<cost)
                { a=trial; lambda=Math.Max(1e-9,lambda/3); if(cost-trialCost<1e-9)break; }
                else { lambda*=10; if(lambda>1e12)break; }
            }
            double finalCost=Cost(Residual(points,a));
            if(finalCost<bestCost){bestCost=finalCost;best=a;}
        }
        return best;
    }
    static double[] Solve(double[,] m)
    {
        int n=m.GetLength(0);
        for(int k=0;k<n;k++)
        {
            int pivot=k; for(int i=k+1;i<n;i++)if(Math.Abs(m[i,k])>Math.Abs(m[pivot,k]))pivot=i;
            if(Math.Abs(m[pivot,k])<1e-15)return null;
            for(int j=k;j<=n;j++){double t=m[k,j];m[k,j]=m[pivot,j];m[pivot,j]=t;}
            double d=m[k,k]; for(int j=k;j<=n;j++)m[k,j]/=d;
            for(int i=0;i<n;i++)if(i!=k){double f=m[i,k];for(int j=k;j<=n;j++)m[i,j]-=f*m[k,j];}
        }
        var a=new double[n];for(int i=0;i<n;i++)a[i]=m[i,n];return a;
    }
}
