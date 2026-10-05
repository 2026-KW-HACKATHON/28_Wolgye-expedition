using System;
using System.IO;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

// Independent U2 input recorder. Does not output a fused pose or use ARCore poses as truth.
public sealed class S25UnifiedCapture : MonoBehaviour
{
    [SerializeField] ARCameraManager rearCameraManager;
    [SerializeField] string frontCameraId="1";
    AndroidJavaObject front,recorder;
    bool recording;
    double started,lastRear=-1,lastPoll;
    long lastRearNs;
    string status="Ready",folder="",error="";
    void OnEnable(){
        if(rearCameraManager==null)rearCameraManager=FindFirstObjectByType<ARCameraManager>();
        if(rearCameraManager!=null)rearCameraManager.frameReceived+=OnRear;
    }
    void Begin(){
#if UNITY_ANDROID && !UNITY_EDITOR
        if(recording)return;
        foreach(var p in FindObjectsByType<S25FrontMapProbe>(FindObjectsSortMode.None))if(p.isActiveAndEnabled){error="Disable S25FrontMapProbe first.";return;}
        foreach(var p in FindObjectsByType<S25DualCameraAR>(FindObjectsSortMode.None))if(p.isActiveAndEnabled){error="Disable S25DualCameraAR first.";return;}
        if(rearCameraManager==null || !rearCameraManager.isActiveAndEnabled){error="Enable rear AR Camera Manager.";return;}
        if(!Permission.HasUserAuthorizedPermission(Permission.Camera)){Permission.RequestUserPermission(Permission.Camera);error="Allow camera permission, then press Record.";return;}
        try{
            recorder?.Dispose();recorder=null;
            folder=Path.Combine(Application.persistentDataPath,"unified-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
            using var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer");using var activity=player.GetStatic<AndroidJavaObject>("currentActivity");
            recorder=new AndroidJavaObject("com.example.s25dualcamera.UnifiedCapture",activity,folder);
            recorder.Call("metadata","session",JsonUtility.ToJson(new SessionInfo{
                device=SystemInfo.deviceModel,unity=Application.unityVersion,packageId=Application.identifier}));
            front=new AndroidJavaObject("com.example.s25dualcamera.FrontFrameSource",activity);
            front.Call("setCapture",recorder);front.Call("start",frontCameraId);
            recording=true;started=Time.realtimeSinceStartupAsDouble;lastRear=-1;lastRearNs=0;error="";status="STARTING";
        }catch(Exception e){error=e.Message;End();}
#else
        error="Android device build required.";
#endif
    }
    [Serializable] sealed class SessionInfo {
        public string schema="s25-unified-input-v2",device,unity,packageId;
        public string stage="INPUT_CAPTURE_ONLY_NO_FUSED_POSE";
        public string imuAxes="Android device axes; accel includes gravity m/s^2; gyro rad/s; no Unity axis conversion";
        public string clocks="IMU Android elapsed realtime ns; front native sensor timestamp; rear XRCpuImage timestamp seconds converted to ns. Cross-clock alignment UNVERIFIED.";
        public string calibration="Camera-IMU extrinsics, temporal offsets and IMU noise UNCALIBRATED";
    }
    [Serializable] sealed class RearInfo {
        public int width,height,nativeWidth,nativeHeight,step;
        public float fx,fy,cx,cy;
        public bool hasIntrinsics;
        public double cpuTimestampSeconds;
        public long frameEventTimestampNs;
        public string layout="Raw luminance, no mirror or rotation, integer pixel subsampling";
    }
    void OnRear(ARCameraFrameEventArgs args){
        if(!recording || Time.realtimeSinceStartupAsDouble-lastRear<.065)return;
        if(!rearCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))return;
        try{
            long ns=(long)Math.Round(image.timestamp*1e9);
            if(ns<=lastRearNs)return;
            var plane=image.GetPlane(0);
            int step=Math.Max(1,(image.width+639)/640),w=(image.width+step-1)/step,h=(image.height+step-1)/step;
            byte[] pixels=new byte[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)pixels[y*w+x]=plane.data[y*step*plane.rowStride+x*step*plane.pixelStride];
            recorder.Call("recordCamera","rear",ns,"XRCPU_UNVERIFIED",w,h,pixels);
            if(lastRearNs==0){
                var meta=new RearInfo{width=w,height=h,nativeWidth=image.width,nativeHeight=image.height,step=step,
                    cpuTimestampSeconds=image.timestamp,frameEventTimestampNs=args.timestampNs??0};
                if(rearCameraManager.TryGetIntrinsics(out XRCameraIntrinsics k)){
                    meta.hasIntrinsics=true;
                    meta.fx=k.focalLength.x*image.width/k.resolution.x/step;meta.fy=k.focalLength.y*image.height/k.resolution.y/step;
                    meta.cx=k.principalPoint.x*image.width/k.resolution.x/step;meta.cy=k.principalPoint.y*image.height/k.resolution.y/step;
                }
                recorder.Call("metadata","rear_metadata",JsonUtility.ToJson(meta));
            }
            lastRearNs=ns;lastRear=Time.realtimeSinceStartupAsDouble;
        }catch(Exception e){error=e.Message;End();}
        finally{image.Dispose();}
    }
    void Update(){
        if(recording && Time.realtimeSinceStartupAsDouble-started>=20)End();
        if(recorder!=null && Time.realtimeSinceStartupAsDouble-lastPoll>.5){
            lastPoll=Time.realtimeSinceStartupAsDouble;
            try{status=recorder.Call<string>("status");if(front!=null)status+="\n"+front.Call<string>("snapshot");}catch(Exception e){error=e.Message;}
        }
    }
    void End(){
        recording=false;
        try{front?.Call("dispose");}catch(Exception e){error=e.Message;}
        front?.Dispose();front=null;
        try{recorder?.Call("close");}catch(Exception e){error=e.Message;}
    }
    void OnApplicationPause(bool paused){if(paused)End();}
    void OnDisable(){End();if(rearCameraManager!=null)rearCameraManager.frameReceived-=OnRear;recorder?.Dispose();recorder=null;}
    void OnGUI(){
        float scale=Screen.width/600f;var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
        GUILayout.BeginArea(new Rect(5,5,590,360),GUI.skin.box);
        GUILayout.Label("U2 UNIFIED METADATA CAPTURE — no fused tracking yet");
        GUI.enabled=!recording && !status.StartsWith("SAVING");
        if(GUILayout.Button("Record 20 seconds",GUILayout.Height(48)))Begin();
        GUI.enabled=recording;if(GUILayout.Button("Stop and save",GUILayout.Height(40)))End();GUI.enabled=true;
        GUILayout.Label(recording?$"Recording {Time.realtimeSinceStartupAsDouble-started:F1} / 20 seconds":"Stopped");
        GUILayout.Label(status);GUILayout.Label(error);GUILayout.Label(folder+".zip");
        GUILayout.Label("Keep still 5s, then slowly translate AND rotate. Both cameras should see static furniture. No target marker in U2.");
        GUILayout.EndArea();GUI.matrix=old;
    }
}
