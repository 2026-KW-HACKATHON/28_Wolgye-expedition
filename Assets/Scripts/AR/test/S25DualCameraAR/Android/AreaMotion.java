package com.example.s25dualcamera;
import java.util.*;
import org.opencv.core.Point;
/** Conservative image-space continuity, with an absolute age limit from the verified frame. */
public final class AreaMotion {
    public static Point bridge(Point target,List<Point> before,List<Point> after,long age,int w,int h){
        if(age<=0 || age>250000000L || before.size()<12 || before.size()!=after.size())return null;
        int n=before.size();double scale=Math.max(w,h)/640.0;double[] dx=new double[n],dy=new double[n];
        boolean left=false,right=false,above=false,below=false;
        for(int i=0;i<n;i++){
            Point a=before.get(i),b=after.get(i);dx[i]=b.x-a.x;dy[i]=b.y-a.y;
            if(!Double.isFinite(dx[i])||!Double.isFinite(dy[i]))return null;
            if(Math.hypot(a.x-target.x,a.y-target.y)<160*scale){left|=a.x<target.x;right|=a.x>target.x;above|=a.y<target.y;below|=a.y>target.y;}
        }
        if(!(left&&right&&above&&below))return null;
        Arrays.sort(dx);Arrays.sort(dy);double x=dx[n/2],y=dy[n/2];
        if(Math.hypot(x,y)>12*scale)return null;
        int consistent=0;
        for(int i=0;i<n;i++)if(Math.hypot(after.get(i).x-before.get(i).x-x,after.get(i).y-before.get(i).y-y)<=2*scale)consistent++;
        if(consistent<12 || consistent<n*.85)return null;
        Point q=new Point(target.x+x,target.y+y);
        return q.x>=0&&q.y>=0&&q.x<w&&q.y<h?q:null;
    }
}
