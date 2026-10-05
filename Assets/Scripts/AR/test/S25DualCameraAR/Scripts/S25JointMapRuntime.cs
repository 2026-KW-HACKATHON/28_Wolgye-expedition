using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

// Device test of localization in a PREBUILT common map. Not live map building / full VIO.
public sealed class S25JointMapRuntime : MonoBehaviour
{
    [SerializeField] ARCameraManager rearCameraManager;
    [SerializeField] TextAsset sharedMap;
    [SerializeField] string frontCameraId="1";
    [SerializeField, Range(640,1280)] int trackingMaxDimension=960;
    AndroidJavaObject source,localizer;
    bool running;int viewCamera;long rearSequence;
    double lastRear,lastDiagnostics;
    string rawStatus="WAITING";
    string message="Start in the room used for the U2 recording.";
    readonly Dictionary<string,Frame> pending=new();
    readonly Frame[] shown=new Frame[2];
    readonly Result[] results={new Result(),new Result()};
    readonly Texture2D[] textures=new Texture2D[2];
    StreamWriter diagnosticLog;
    string logPath="";
    Frame queuedRear;
    sealed class Frame {public byte[] gray,trackingGray;public double[] trackingK;public int tw,th;public int camera,w,h;public long seq,stamp;public double[] k;public double received;}
    [Serializable] sealed class Result {public int camera,inliers,targetId=-1;public long seq,timestampNs;public bool valid,targetVisible;public float u,v,rms;public string state="WAITING";public float[] bodyPose;}
    void OnEnable(){if(rearCameraManager==null)rearCameraManager=FindFirstObjectByType<ARCameraManager>();if(rearCameraManager!=null)rearCameraManager.frameReceived+=Rear;}
    void StartRuntime(){
#if UNITY_ANDROID && !UNITY_EDITOR
        foreach(var c in FindObjectsByType<S25UnifiedCapture>(FindObjectsSortMode.None))if(c.isActiveAndEnabled){message="Disable S25UnifiedCapture first.";return;}
        foreach(var c in FindObjectsByType<S25FrontMapProbe>(FindObjectsSortMode.None))if(c.isActiveAndEnabled){message="Disable S25FrontMapProbe first.";return;}
        foreach(var c in FindObjectsByType<S25DualCameraAR>(FindObjectsSortMode.None))if(c.isActiveAndEnabled){message="Disable S25DualCameraAR first.";return;}
        if(!Permission.HasUserAuthorizedPermission(Permission.Camera)){Permission.RequestUserPermission(Permission.Camera);return;}
        if(sharedMap==null)sharedMap=Resources.Load<TextAsset>("S25SharedMap");
        if(sharedMap==null){message="S25SharedMap.json missing";return;}
        if(rearCameraManager==null){message="Rear AR Camera Manager missing";return;}
        StopRuntime();
        try{
            logPath=Path.Combine(Application.persistentDataPath,"joint-map-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".jsonl");
            diagnosticLog=new StreamWriter(logPath){AutoFlush=true};
            using var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer");using var activity=player.GetStatic<AndroidJavaObject>("currentActivity");
            localizer=new AndroidJavaObject("com.example.s25dualcamera.JointMapLocalizer",activity,sharedMap.text);
            source=new AndroidJavaObject("com.example.s25dualcamera.FrontFrameSource",activity);source.Call("setRawEnabled",true);source.Call("start",frontCameraId);running=true;
            message="Prebuilt-map test. Both camera images use the same map. Select a landmark when tracked.";
        }catch(Exception e){message=e.Message;StopRuntime();}
#else
        message="Android device required.";
#endif
    }
    void Rear(ARCameraFrameEventArgs e){
        if(!running || Time.realtimeSinceStartupAsDouble-lastRear<.08)return;
        if(!rearCameraManager.TryAcquireLatestCpuImage(out XRCpuImage im))return;
        try{
            if(!rearCameraManager.TryGetIntrinsics(out XRCameraIntrinsics k))return;
            int step=Math.Max(1,(im.width+639)/640),w=(im.width+step-1)/step,h=(im.height+step-1)/step;var p=im.GetPlane(0);var gray=new byte[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)gray[y*w+x]=p.data[y*step*p.rowStride+x*step*p.pixelStride];
            queuedRear=new Frame{camera=1,seq=++rearSequence,stamp=(long)Math.Round(im.timestamp*1e9),w=w,h=h,gray=gray,k=new double[]{k.focalLength.x*im.width/k.resolution.x/step,k.focalLength.y*im.height/k.resolution.y/step,k.principalPoint.x*im.width/k.resolution.x/step,k.principalPoint.y*im.height/k.resolution.y/step},received=Time.realtimeSinceStartupAsDouble};lastRear=queuedRear.received;
        }catch(Exception ex){message=ex.Message;}finally{im.Dispose();}
    }
    Frame queuedFront;int nextCamera;
    void Update(){
        if(!running)return;
        try{
            if(Time.realtimeSinceStartupAsDouble-lastDiagnostics>.5){lastDiagnostics=Time.realtimeSinceStartupAsDouble;rawStatus=source.Call<string>("rawStatus");}
            string json=localizer.Call<string>("takeResult");
            if(!string.IsNullOrEmpty(json)){
                diagnosticLog?.WriteLine(json);
                Result r=JsonUtility.FromJson<Result>(json);string key=r.camera+":"+r.seq;
                if(pending.TryGetValue(key,out Frame f)){
                    pending.Remove(key);results[r.camera]=r;shown[r.camera]=f;
                    var tex=textures[r.camera];if(tex==null || tex.width!=f.w || tex.height!=f.h){if(tex!=null)Destroy(tex);tex=new Texture2D(f.w,f.h,TextureFormat.RGBA32,false);textures[r.camera]=tex;}
                    byte[] rgba=new byte[f.w*f.h*4];for(int y=0;y<f.h;y++)for(int x=0;x<f.w;x++){byte v=f.gray[y*f.w+x];int i=((f.h-1-y)*f.w+x)*4;rgba[i]=rgba[i+1]=rgba[i+2]=v;rgba[i+3]=255;}tex.LoadRawTextureData(rgba);tex.Apply();
                }
            }
            byte[] b=source.Call<byte[]>("takeRawFrame");
            if(b!=null && b.Length>48 && BitConverter.ToInt32(b,0)==0x35415253){
                int w=BitConverter.ToInt32(b,4),h=BitConverter.ToInt32(b,8);if(b.Length!=48+w*h)throw new Exception("Bad raw frame");
                var gray=new byte[w*h];Buffer.BlockCopy(b,48,gray,0,gray.Length);queuedFront=new Frame{camera=0,w=w,h=h,seq=BitConverter.ToInt64(b,24),stamp=BitConverter.ToInt64(b,16),gray=gray,k=new double[]{BitConverter.ToSingle(b,32),BitConverter.ToSingle(b,36),BitConverter.ToSingle(b,40),BitConverter.ToSingle(b,44)},received=Time.realtimeSinceStartupAsDouble};
            }
            Frame candidate=nextCamera==0?queuedFront:queuedRear;if(candidate==null)candidate=nextCamera==0?queuedRear:queuedFront;
            if(candidate!=null){
                if(Time.realtimeSinceStartupAsDouble-candidate.received>.3){if(candidate.camera==0)queuedFront=null;else queuedRear=null;return;}
                PrepareTracking(candidate);
                if(localizer.Call<bool>("submit",candidate.camera,candidate.seq,candidate.stamp,candidate.trackingGray,candidate.tw,candidate.th,candidate.trackingK)){
                    pending[candidate.camera+":"+candidate.seq]=candidate;nextCamera=1-candidate.camera;
                    if(candidate.camera==0)queuedFront=null;else queuedRear=null;
                }
            }
        }catch(Exception e){message=e.Message;StopRuntime();}
    }
    void PrepareTracking(Frame f){
        if(f.trackingGray!=null)return;
        double scale=Math.Min(1.0,Mathf.Clamp(trackingMaxDimension,640,1280)/(double)Math.Max(f.w,f.h));
        f.tw=Math.Max(1,(int)Math.Round(f.w*scale));f.th=Math.Max(1,(int)Math.Round(f.h*scale));
        double sx=f.tw/(double)f.w,sy=f.th/(double)f.h;
        f.trackingK=new[]{f.k[0]*sx,f.k[1]*sy,(f.k[2]+.5)*sx-.5,(f.k[3]+.5)*sy-.5};
        if(f.tw==f.w && f.th==f.h){f.trackingGray=f.gray;return;}
        f.trackingGray=new byte[f.tw*f.th];
        for(int y=0;y<f.th;y++)for(int x=0;x<f.tw;x++){
            double px=Math.Max(0,Math.Min(f.w-1,(x+.5)/sx-.5)),py=Math.Max(0,Math.Min(f.h-1,(y+.5)/sy-.5));
            int x0=(int)px,y0=(int)py,x1=Math.Min(x0+1,f.w-1),y1=Math.Min(y0+1,f.h-1);double ax=px-x0,ay=py-y0;
            double upper=f.gray[y0*f.w+x0]*(1-ax)+f.gray[y0*f.w+x1]*ax,lower=f.gray[y1*f.w+x0]*(1-ax)+f.gray[y1*f.w+x1]*ax;
            f.trackingGray[y*f.tw+x]=(byte)Math.Round(upper*(1-ay)+lower*ay);
        }
    }
    void StopRuntime(){running=false;diagnosticLog?.Dispose();diagnosticLog=null;try{source?.Call("dispose");localizer?.Call("dispose");}catch(Exception e){Debug.LogWarning(e.Message);}source?.Dispose();localizer?.Dispose();source=localizer=null;pending.Clear();queuedFront=queuedRear=null;for(int i=0;i<2;i++){results[i]=new Result();shown[i]=null;}}
    void OnApplicationPause(bool p){if(p)StopRuntime();}
    void OnDisable(){StopRuntime();if(rearCameraManager!=null)rearCameraManager.frameReceived-=Rear;foreach(var t in textures)if(t!=null)Destroy(t);}
    void OnGUI(){
        GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.blackTexture);
        var f=shown[viewCamera];var r=results[viewCamera];var tex=textures[viewCamera];float top=Screen.height*.4f;
        if(f!=null && tex!=null){float scale=Mathf.Min(Screen.width/(float)f.w,(Screen.height-top)/f.h);Rect box=new Rect((Screen.width-f.w*scale)/2,top,f.w*scale,f.h*scale);GUI.DrawTexture(box,tex);
            bool fresh=running && Time.realtimeSinceStartupAsDouble-f.received<.5;
            if(fresh && r.valid && r.targetVisible && r.u>=0 && r.v>=0 && r.u<f.tw && r.v<f.th){float x=box.x+((r.u+.5f)*f.w/f.tw-.5f)*scale,y=box.y+((r.v+.5f)*f.h/f.th-.5f)*scale;GUI.color=Color.green;GUI.DrawTexture(new Rect(x-12,y-2,24,4),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(x-2,y-12,4,24),Texture2D.whiteTexture);GUI.color=Color.white;}
        }
        var old=GUI.matrix;float s=Screen.width/600f;GUI.matrix=Matrix4x4.Scale(Vector3.one*s);GUILayout.BeginArea(new Rect(5,5,590,400),GUI.skin.box);
        GUILayout.Label("JOINT MAP DEVICE TEST — prebuilt room map, visual localization");
        GUILayout.BeginHorizontal();if(GUILayout.Button("Start",GUILayout.Height(40)))StartRuntime();if(GUILayout.Button("Stop",GUILayout.Height(40)))StopRuntime();GUILayout.EndHorizontal();
        if(GUILayout.Button(viewCamera==0?"Show REAR":"Show FRONT",GUILayout.Height(40)))viewCamera=1-viewCamera;
        GUI.enabled=running && r.valid && r.state.StartsWith("SHARED_MAP_") && shown[viewCamera]!=null && Time.realtimeSinceStartupAsDouble-shown[viewCamera].received<.5;
        if(GUILayout.Button("Select nearest matched landmark",GUILayout.Height(40)))localizer.Call("selectTarget",viewCamera);GUI.enabled=true;
        for(int i=0;i<2;i++)GUILayout.Label((i==0?"FRONT: ":"REAR: ")+results[i].state+" | inliers "+results[i].inliers+" RMS "+results[i].rms.ToString("F2")+" px");
        GUILayout.Label("View "+(viewCamera==0?"FRONT":"REAR")+" | target "+r.targetId+" | map scale UNVALIDATED");GUILayout.Label(message);GUILayout.Label("Front input: "+rawStatus);GUILayout.Label("Log: "+logPath);
        if(f!=null)GUILayout.Label("Preview "+f.w+"x"+f.h+" | Tracking "+f.tw+"x"+f.th);
        GUILayout.Label("Raw landscape images. No live map expansion. Lost tracking hides the marker.");GUILayout.EndArea();GUI.matrix=old;
    }
}
