using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Read-only ARCore C API access. Never creates/updates/destroys Unity's session/frame.
// Layout verified against ARCore XR Plugin 6.3.5 Includes~/UnityXRNativePtrs.h.
public static class S25RearNativeFrame
{
    [StructLayout(LayoutKind.Sequential)] struct NativeHandle { public int version; public IntPtr handle; }
    public sealed class Sample { public long timestamp; public IntPtr session; public double[] k, worldToCamera; public int width,height; }
    public static bool TryRead(ARSession arSession, ARCameraManager manager, Camera camera, out Sample sample, out string reason)
    {
        sample=null;reason="REAR_NATIVE_UNAVAILABLE";
#if UNITY_ANDROID && !UNITY_EDITOR
        IntPtr acquired=IntPtr.Zero,pose=IntPtr.Zero,intrinsics=IntPtr.Zero;
        try {
            if(arSession==null || arSession.subsystem==null || manager.subsystem==null)return false;
            var cp=new XRCameraParams{zNear=camera.nearClipPlane,zFar=camera.farClipPlane,screenWidth=Screen.width,screenHeight=Screen.height,screenOrientation=Screen.orientation};
            if(!manager.subsystem.TryGetLatestFrame(cp,out XRCameraFrame frame))return false;
            IntPtr sn=arSession.subsystem.nativePtr,fn=frame.nativePtr;
            if(sn==IntPtr.Zero || fn==IntPtr.Zero)return false;
            if(Marshal.ReadInt32(sn)!=1 || Marshal.ReadInt32(fn)!=1){reason="UNSUPPORTED_ARCORE_NATIVE_LAYOUT";return false;}
            var sh=Marshal.PtrToStructure<NativeHandle>(sn);var fh=Marshal.PtrToStructure<NativeHandle>(fn);
            if(sh.handle==IntPtr.Zero || fh.handle==IntPtr.Zero)return false;
            ArFrame_getTimestamp(sh.handle,fh.handle,out long stamp);
            ArFrame_acquireCamera(sh.handle,fh.handle,out acquired);
            if(acquired==IntPtr.Zero)return false;
            ArCamera_getTrackingState(sh.handle,acquired,out int tracking);
            if(tracking!=0){reason="REAR_NOT_TRACKING";return false;}
            ArPose_create(sh.handle,IntPtr.Zero,out pose);ArCamera_getPose(sh.handle,acquired,pose);
            float[] m=new float[16];ArPose_getMatrix(sh.handle,pose,m);
            // OpenGL world-from-physical-camera, column-major -> CV camera-from-world.
            // CV x right/y down/z forward = diag(1,-1,-1) * OpenGL camera.
            double[] p=new double[12];
            for(int r=0;r<3;r++){double sign=r==0?1:-1;
                for(int c=0;c<3;c++)p[r*4+c]=sign*m[r*4+c];
                p[r*4+3]=-(p[r*4]*m[12]+p[r*4+1]*m[13]+p[r*4+2]*m[14]);}
            ArCameraIntrinsics_create(sh.handle,out intrinsics);ArCamera_getImageIntrinsics(sh.handle,acquired,intrinsics);
            ArCameraIntrinsics_getFocalLength(sh.handle,intrinsics,out float fx,out float fy);
            ArCameraIntrinsics_getPrincipalPoint(sh.handle,intrinsics,out float cx,out float cy);
            ArCameraIntrinsics_getImageDimensions(sh.handle,intrinsics,out int w,out int h);
            if(stamp<=0 || fx<=0 || fy<=0 || w<1 || h<1){reason="INVALID_REAR_METADATA";return false;}
            sample=new Sample{timestamp=stamp,session=sh.handle,k=new double[]{fx,fy,cx,cy},worldToCamera=p,width=w,height=h};reason="REAR_NATIVE_READY";return true;
        }catch(Exception e){reason="REAR_NATIVE_ERROR: "+e.Message;return false;}
        finally{if(intrinsics!=IntPtr.Zero)ArCameraIntrinsics_destroy(intrinsics);if(pose!=IntPtr.Zero)ArPose_destroy(pose);if(acquired!=IntPtr.Zero)ArCamera_release(acquired);}
#else
        reason="ANDROID_ARCORE_REQUIRED";return false;
#endif
    }
    const string Lib="arcore_sdk_c";
    [DllImport(Lib)] static extern void ArFrame_getTimestamp(IntPtr session,IntPtr frame,out long timestamp);
    [DllImport(Lib)] static extern void ArFrame_acquireCamera(IntPtr session,IntPtr frame,out IntPtr camera);
    [DllImport(Lib)] static extern void ArCamera_getTrackingState(IntPtr session,IntPtr camera,out int state);
    [DllImport(Lib)] static extern void ArPose_create(IntPtr session,IntPtr raw,out IntPtr pose);
    [DllImport(Lib)] static extern void ArCamera_getPose(IntPtr session,IntPtr camera,IntPtr pose);
    [DllImport(Lib)] static extern void ArPose_getMatrix(IntPtr session,IntPtr pose,[Out] float[] matrix);
    [DllImport(Lib)] static extern void ArPose_destroy(IntPtr pose);
    [DllImport(Lib)] static extern void ArCamera_release(IntPtr camera);
    [DllImport(Lib)] static extern void ArCameraIntrinsics_create(IntPtr session,out IntPtr intrinsics);
    [DllImport(Lib)] static extern void ArCamera_getImageIntrinsics(IntPtr session,IntPtr camera,IntPtr intrinsics);
    [DllImport(Lib)] static extern void ArCameraIntrinsics_getFocalLength(IntPtr session,IntPtr intrinsics,out float x,out float y);
    [DllImport(Lib)] static extern void ArCameraIntrinsics_getPrincipalPoint(IntPtr session,IntPtr intrinsics,out float x,out float y);
    [DllImport(Lib)] static extern void ArCameraIntrinsics_getImageDimensions(IntPtr session,IntPtr intrinsics,out int w,out int h);
    [DllImport(Lib)] static extern void ArCameraIntrinsics_destroy(IntPtr intrinsics);
}
