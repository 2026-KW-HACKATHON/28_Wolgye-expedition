using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

// Separate v8 experiment: disable S25DualCameraAR/v2 before enabling this component.
[DefaultExecutionOrder(11000)]
public sealed class S25FrontMapProbe : MonoBehaviour
{
    [SerializeField] ARCameraManager rearCameraManager;
    [SerializeField] ARRaycastManager rearRaycastManager;
    [SerializeField] ARPlaneManager rearPlaneManager;
    readonly List<ARRaycastHit> planeHits=new List<ARRaycastHit>();
    bool hasPlaneTarget;
    Vector3 planeTargetMap;
    string targetOrigin="Front triangulated point";
    [SerializeField] string frontCameraId="1";
    [SerializeField] Vector3 frontOffsetMeters;
    [SerializeField] Vector3 frontAngleCorrection;
    [SerializeField] float poseTimeAdjustmentMs;
    // Pose is in the initial map coordinates, not a newly reset ARCore coordinate frame.
    public bool HasPose { get; private set; }
    public Pose CurrentPose { get; private set; }
    public string PoseSource { get; private set; } = "NONE";
    int referenceEpoch;
    bool previousRearGood;
    Camera rearCamera;
    AndroidJavaObject source,tracker;
    Texture2D texture;
    bool running,ignoreRear,showRear,showCube;
    string viewStatus="";
    ScreenOrientation oldOrientation;
    double rearFrameAt=-10;
    long? rearTimestamp;
    string message="Start, aim FRONT at a textured static room, then move sideways 8-15 cm.";
    Result result=new Result();
    double resultAt=-10;
    float agreementM,agreementDeg;
    int verified;
    readonly List<TimedPose> history=new List<TimedPose>();
    readonly Dictionary<long,Frame> pending=new Dictionary<long,Frame>();
    Frame displayed;
    StreamWriter log;
    string logPath;
    struct TimedPose {public double time;public Pose pose;}
    sealed class Frame {public byte[] packet;public bool referenceGood;public Pose reference;public double capturedAt;}
    [Serializable] sealed class Result
    {
        public string state="IDLE"; public long seq,timeMs;public int points,inliers,added,keyframes,relocalized;public bool rearLinked;public float value;
        public int referenceEpoch;public bool valid;public float[] pose,target,mapFromRearWorld;
    }
    void OnEnable()
    {
        if(rearRaycastManager==null)rearRaycastManager=FindFirstObjectByType<ARRaycastManager>();
        if(rearPlaneManager==null)rearPlaneManager=FindFirstObjectByType<ARPlaneManager>();
        oldOrientation=Screen.orientation;Screen.orientation=ScreenOrientation.Portrait;
        if(rearCameraManager==null)rearCameraManager=FindFirstObjectByType<ARCameraManager>();
        if(rearCameraManager!=null){rearCamera=rearCameraManager.GetComponent<Camera>();rearCameraManager.frameReceived+=RearFrame;}
        ARSession.stateChanged+=TrackingChanged;
    }
    void RearFrame(ARCameraFrameEventArgs e){if(e.timestampNs.HasValue && e.timestampNs!=rearTimestamp){rearTimestamp=e.timestampNs;rearFrameAt=Time.realtimeSinceStartupAsDouble;}}
    void TrackingChanged(ARSessionStateChangedEventArgs e){history.Clear();referenceEpoch++;}
    bool RearGood => rearCamera!=null && ARSession.state==ARSessionState.SessionTracking && ARSession.notTrackingReason==NotTrackingReason.None &&
        rearCameraManager.currentFacingDirection==CameraFacingDirection.World && Time.realtimeSinceStartupAsDouble-rearFrameAt<.3;
    void StartProbe()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if(!RearGood){message="Wait for rear SessionTracking first.";return;}
        if(!Permission.HasUserAuthorizedPermission(Permission.Camera)){Permission.RequestUserPermission(Permission.Camera);return;}
        foreach(var other in FindObjectsByType<S25DualCameraAR>(FindObjectsSortMode.None))
            if(other.isActiveAndEnabled){message="Disable S25DualCameraAR component first; keep rear AR enabled.";return;}
        StopProbe();
        try{
            using var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer");using var activity=player.GetStatic<AndroidJavaObject>("currentActivity");
            source=new AndroidJavaObject("com.example.s25dualcamera.FrontFrameSource",activity);
            tracker=new AndroidJavaObject("com.example.s25dualcamera.FrontMapTracker");
            source.Call("start",frontCameraId);running=true;ignoreRear=false;showRear=false;verified=0;result=new Result();
            logPath=Path.Combine(Application.persistentDataPath,"front-map-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".csv");
            log=new StreamWriter(logPath);log.WriteLine("sequence,sensor_ms,state,rear_state,rear_ignored,points,inliers,value,agreement_m,agreement_deg,added,keyframes,relocalized,rear_linked,source");
            message="Aim FRONT at books/furniture (not your face). Slide phone sideways slowly; do not turn.";
        }catch(Exception e){StopProbe();message=e.Message;Debug.LogException(e);}
#else
        message="Android device build required.";
#endif
    }
    void ResetMap()
    {
        hasPlaneTarget=false;targetOrigin="Front triangulated point";
        try{tracker?.Call("dispose");tracker?.Dispose();tracker=new AndroidJavaObject("com.example.s25dualcamera.FrontMapTracker");
            pending.Clear();result=new Result();HasPose=false;PoseSource="NONE";verified=0;ignoreRear=false;referenceEpoch++;message="New map: rear uncovered, front sees static room; slide sideways 8-15 cm.";
        }catch(Exception e){message=e.Message;}
    }
    void LateUpdate()
    {
        double now=Time.realtimeSinceStartupAsDouble;
        bool rearGoodNow=RearGood;
        if(rearGoodNow!=previousRearGood){referenceEpoch++;previousRearGood=rearGoodNow;}
        if(displayed==null || now-displayed.capturedAt>=.6){HasPose=false;PoseSource="NONE";}
        if(rearCamera!=null && RearGood){history.Add(new TimedPose{time=now,pose=new Pose(rearCamera.transform.position,rearCamera.transform.rotation)});}
        while(history.Count>240 || (history.Count>0 && now-history[0].time>2))history.RemoveAt(0);
        if(!running || source==null || tracker==null)return;
        try{
            string json=tracker.Call<string>("takeResult");
            if(!string.IsNullOrEmpty(json)){
                Result next=JsonUtility.FromJson<Result>(json);
                if(next!=null && pending.TryGetValue(next.seq,out Frame frame)){
                    result=next;displayed=frame;resultAt=now;ShowFrame(frame.packet);
                    HasPose=next.valid && next.pose?.Length==16 && now-frame.capturedAt<.6;
                    PoseSource=HasPose?(next.state.StartsWith("REAR")?"REAR":next.state=="MAP_READY"?"BOOTSTRAP":"FRONT"):"NONE";
                    if(HasPose)CurrentPose=UnityPose(next.pose);
                    if(next.valid && next.pose!=null && next.pose.Length==16 && frame.referenceGood){
                        Pose estimate=UnityPose(next.pose);
                        agreementM=Vector3.Distance(estimate.position,frame.reference.position);agreementDeg=Quaternion.Angle(estimate.rotation,frame.reference.rotation);
                        if(next.state=="FRONT_PNP" && next.rearLinked)verified++;else verified=0;
                    }
                    else if(!ignoreRear)verified=0;
                    log?.WriteLine(FormattableString.Invariant($"{next.seq},{next.timeMs},{next.state},{ARSession.state},{ignoreRear},{next.points},{next.inliers},{next.value:F4},{agreementM:F4},{agreementDeg:F3},{next.added},{next.keyframes},{next.relocalized},{next.rearLinked},{PoseSource}"));
                    if(next.seq%30==0)log?.Flush();
                    var keys=new List<long>(pending.Keys);foreach(long key in keys)if(key<=next.seq)pending.Remove(key);
                }else if(next!=null && next.state.StartsWith("ERROR")){result=next;message=next.state;}
            }
            byte[] packet=source.Call<byte[]>("takeFrame");
            if(packet==null || packet.Length<48)return;
            long seq=BitConverter.ToInt64(packet,16),stamp=BitConverter.ToInt64(packet,24);
            double age=(source.Call<long>("clockMs")-stamp)/1000.0;
            double at=now-age+poseTimeAdjustmentMs/1000.0;
            bool referenceGood=!ignoreRear && RearGood && age>=0 && age<.4 && history.Count>=2 && at>=history[0].time && at<=history[history.Count-1].time;
            Pose rear=referenceGood?At(at):new Pose(Vector3.zero,Quaternion.identity);
            Pose front=new Pose(rear.position+rear.rotation*frontOffsetMeters,rear.rotation*Quaternion.Euler(0,180,0)*Quaternion.Euler(frontAngleCorrection));
            if(tracker.Call<bool>("submit",packet,CvPose(front),referenceGood,referenceEpoch))
                pending[seq]=new Frame{packet=packet,reference=front,referenceGood=referenceGood,capturedAt=now-age};
            if(pending.Count>8){var keys=new List<long>(pending.Keys);keys.Sort();pending.Remove(keys[0]);}
        }catch(Exception e){message=e.Message;Debug.LogException(e);StopProbe();}
    }
    Pose At(double t){for(int i=1;i<history.Count;i++)if(history[i].time>=t){var a=history[i-1];var b=history[i];float f=(float)((t-a.time)/(b.time-a.time));return new Pose(Vector3.Lerp(a.pose.position,b.pose.position,f),Quaternion.Slerp(a.pose.rotation,b.pose.rotation,f));}return history[history.Count-1].pose;}
    static double[] CvPose(Pose pose){Matrix4x4 m=Matrix4x4.TRS(pose.position,pose.rotation,Vector3.one);double[] a=new double[16];for(int i=0;i<4;i++)for(int j=0;j<4;j++)a[i*4+j]=m[i,j]*(i==1?-1:1)*(j==1?-1:1);return a;}
    static Pose UnityPose(float[] a){Matrix4x4 m=Matrix4x4.identity;for(int i=0;i<4;i++)for(int j=0;j<4;j++)m[i,j]=a[i*4+j]*(i==1?-1:1)*(j==1?-1:1);return new Pose(m.GetColumn(3),Quaternion.LookRotation(m.GetColumn(2),m.GetColumn(1)));}
    void ShowFrame(byte[] packet){int w=BitConverter.ToInt32(packet,4),h=BitConverter.ToInt32(packet,8);if(texture==null || texture.width!=w || texture.height!=h){if(texture!=null)Destroy(texture);texture=new Texture2D(w,h,TextureFormat.RGBA32,false);}
        var rgba=new byte[w*h*4];Buffer.BlockCopy(packet,48,rgba,0,rgba.Length);texture.LoadRawTextureData(rgba);texture.Apply();}
    void StopProbe(){hasPlaneTarget=false;targetOrigin="Front triangulated point";running=false;HasPose=false;PoseSource="NONE";try{source?.Call("dispose");tracker?.Call("dispose");}catch(Exception e){Debug.LogWarning(e.Message);}source?.Dispose();tracker?.Dispose();source=tracker=null;pending.Clear();result=new Result();displayed=null;log?.Dispose();log=null;}
    void OnApplicationPause(bool paused){if(paused)StopProbe();}
    void OnDisable(){StopProbe();if(rearCameraManager!=null)rearCameraManager.frameReceived-=RearFrame;ARSession.stateChanged-=TrackingChanged;if(texture!=null)Destroy(texture);history.Clear();Screen.orientation=oldOrientation;}
    bool RearLinkUsable => running && !ignoreRear && RearGood && result.valid && result.rearLinked &&
        result.referenceEpoch==referenceEpoch && result.mapFromRearWorld?.Length==16 && displayed!=null &&
        Time.realtimeSinceStartupAsDouble-displayed.capturedAt<.6;

    void PlaceRearPlaneTarget()
    {
        if(!showRear || !RearLinkUsable){message="Use REAR view / AUTO and wait for Rear link True.";return;}
        if(rearRaycastManager==null)rearRaycastManager=FindFirstObjectByType<ARRaycastManager>();
        if(rearPlaneManager==null)rearPlaneManager=FindFirstObjectByType<ARPlaneManager>();
        if(rearRaycastManager==null || !rearRaycastManager.isActiveAndEnabled || rearPlaneManager==null || !rearPlaneManager.isActiveAndEnabled){
            message="Add/enable AR Plane Manager and AR Raycast Manager on XR Origin.";return;
        }
        Vector2 aim=new Vector2(Screen.width*.5f,Screen.height*.5f);
        if(!rearRaycastManager.Raycast(aim,planeHits,TrackableType.PlaneWithinPolygon)){
            message="No detected plane at white reticle. Slowly scan the tabletop with REAR camera.";return;
        }
        ARRaycastHit hit=planeHits[0];
        if(Vector3.Dot(hit.pose.rotation*Vector3.up,Vector3.up)<.85f){message="Aim at a horizontal tabletop or floor, not the monitor.";return;}
        Pose link=UnityPose(result.mapFromRearWorld);
        planeTargetMap=Matrix4x4.TRS(link.position,link.rotation,Vector3.one).MultiplyPoint3x4(hit.pose.position);
        hasPlaneTarget=true;targetOrigin="Rear plane point (fixed at placement)";
        message=$"Placed on detected plane at {hit.distance:F2} m. Move REAR sideways first, then Show FRONT and aim at SAME spot.";
        Debug.Log($"S25 v8 plane target: AR world={hit.pose.position:F5}, map={planeTargetMap:F5}, epoch={referenceEpoch}");
    }
    // Target and cube stay in one map coordinate frame. Switching changes only projection.
    bool ProjectMapPoint(Vector3 mapPoint,Rect imageRect,Matrix4x4 rearWorldFromMap,out Vector2 screen)
    {
        screen=default;
        if(showRear){
            Vector3 p=rearCamera.WorldToScreenPoint(rearWorldFromMap.MultiplyPoint3x4(mapPoint));
            if(p.z<=rearCamera.nearClipPlane || !rearCamera.pixelRect.Contains(new Vector2(p.x,p.y)))return false;
            screen=new Vector2(p.x,Screen.height-p.y);return true;
        }
        Pose cameraPose=UnityPose(result.pose);
        Vector3 q=Quaternion.Inverse(cameraPose.rotation)*(mapPoint-cameraPose.position);
        if(q.z<=.05f || displayed==null || texture==null)return false;
        byte[] b=displayed.packet;
        float u=BitConverter.ToSingle(b,32)*q.x/q.z+BitConverter.ToSingle(b,40);
        float v=BitConverter.ToSingle(b,44)-BitConverter.ToSingle(b,36)*q.y/q.z;
        if(u<0 || v<0 || u>=texture.width || v>=texture.height)return false;
        screen=new Vector2(imageRect.x+u/texture.width*imageRect.width,imageRect.y+v/texture.height*imageRect.height);return true;
    }
    void DrawMapCube(Vector3 center,Rect r,Matrix4x4 rearWorldFromMap)
    {
        // A 5 cm wireframe cube centred on the SAME landmark, not a Unity prefab.
        var points=new Vector2[8];var visible=new bool[8];
        for(int i=0;i<8;i++)visible[i]=ProjectMapPoint(center+new Vector3((i&1)==0?-.025f:.025f,(i&2)==0?-.025f:.025f,(i&4)==0?-.025f:.025f),r,rearWorldFromMap,out points[i]);
        GUI.color=Color.cyan;
        for(int i=0;i<8;i++)for(int axis=1;axis<=4;axis*=2){int j=i^axis;if(j>i && visible[i] && visible[j])DrawLine(points[i],points[j]);}
        GUI.color=Color.white;
    }
    static void DrawLine(Vector2 a,Vector2 b)
    {
        Matrix4x4 old=GUI.matrix;Vector2 delta=b-a;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,a);
        GUI.DrawTexture(new Rect(a.x,a.y-1,delta.magnitude,2),Texture2D.whiteTexture);GUI.matrix=old;
    }
    void OnGUI()
    {
        var old=GUI.matrix;int depth=GUI.depth;GUI.depth=-1200;
        float scale=Mathf.Max(1,Screen.width/600f),top=410*scale;
        bool fresh=running && displayed!=null && Time.realtimeSinceStartupAsDouble-displayed.capturedAt<.6;
        Rect videoRect=default;
        if(!showRear && texture!=null && running){
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.blackTexture);
            float s=Mathf.Min(Screen.width/(float)texture.width,Mathf.Max(1,Screen.height-top)/texture.height);
            videoRect=new Rect((Screen.width-texture.width*s)/2,top,texture.width*s,texture.height*s);
            GUI.DrawTexture(videoRect,texture);
        }
        if(showRear && running){
            float x=Screen.width*.5f,y=Screen.height*.5f;GUI.color=Color.white;
            GUI.DrawTexture(new Rect(x-20,y-20,40,2),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x-20,y+18,40,2),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x-20,y-20,2,40),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x+18,y-20,2,40),Texture2D.whiteTexture);
        }
        bool canDraw=fresh && result.valid && result.pose?.Length==16 && (hasPlaneTarget || result.target?.Length==3);
        Matrix4x4 rearWorldFromMap=Matrix4x4.identity;
        if(showRear){
            canDraw &= !ignoreRear && RearGood && result.rearLinked && result.referenceEpoch==referenceEpoch && result.mapFromRearWorld?.Length==16;
            if(canDraw){Pose link=UnityPose(result.mapFromRearWorld);rearWorldFromMap=Matrix4x4.TRS(link.position,link.rotation,Vector3.one).inverse;}
        }
        viewStatus=canDraw?"Target outside view / behind camera":showRear?"Waiting for fresh AUTO rear coordinate link":"Waiting for valid front pose";
        if(canDraw){
            Vector3 target=hasPlaneTarget?planeTargetMap:new Vector3(result.target[0],-result.target[1],result.target[2]);
            if(ProjectMapPoint(target,videoRect,rearWorldFromMap,out Vector2 screen)){
                viewStatus="Same map target visible";GUI.color=Color.green;
                GUI.DrawTexture(new Rect(screen.x-15,screen.y-2,30,4),Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(screen.x-2,screen.y-15,4,30),Texture2D.whiteTexture);GUI.color=Color.white;
            }
            if(showCube)DrawMapCube(target,videoRect,rearWorldFromMap);
        }
        GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
        GUILayout.BeginArea(new Rect(5,5,Screen.width/scale-10,400),GUI.skin.box);
        GUILayout.Label("v8 PLANE TARGET — compare FRONT / REAR");
        GUILayout.BeginHorizontal();
        if(GUILayout.Button("Start",GUILayout.Height(35)))StartProbe();
        GUI.enabled=running;if(GUILayout.Button("Reset map",GUILayout.Height(35)))ResetMap();
        GUI.enabled=running && (verified>=5 || ignoreRear);
        if(GUILayout.Button(ignoreRear?"Return AUTO":"Test FRONT ONLY",GUILayout.Height(35))){
            ignoreRear=!ignoreRear;referenceEpoch++;verified=0;
            message=ignoreRear?"Rear input disabled. Move slowly to add new features; return to a saved view if lost.":"AUTO: rear fallback waits until both trackers confirm the coordinate link again.";
        }
        GUI.enabled=true;if(GUILayout.Button("Stop",GUILayout.Height(35)))StopProbe();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();GUI.enabled=running;
        if(GUILayout.Button(showRear?"Show FRONT":"Show REAR",GUILayout.Height(36)))showRear=!showRear;
        if(GUILayout.Button(showCube?"Cube OFF":"Cube ON",GUILayout.Height(36)))showCube=!showCube;
        GUI.enabled=true;GUILayout.EndHorizontal();
        GUI.enabled=showRear && RearLinkUsable;
        if(GUILayout.Button("Place on table (white reticle)",GUILayout.Height(36)))PlaceRearPlaneTarget();
        GUI.enabled=true;
        GUILayout.Label("Target: "+targetOrigin);
        GUILayout.Label($"VIEW: {(showRear?"REAR":"FRONT")} | {viewStatus}");
        GUILayout.Label($"Rear: {ARSession.state} / {ARSession.notTrackingReason} | input {(ignoreRear?"IGNORED":"AUTO")}\nFront: {result.state} | points {result.points}, inliers {result.inliers}");
        string quality=(result.state=="FRONT_PNP" || result.state=="FRONT_RELOCALIZED")?$"Reprojection RMS {result.value:F2} px":$"Bootstrap/aux value {result.value:F3}";
        GUILayout.Label($"Added {result.added} | Keyframes {result.keyframes}/12 | Recovered {result.relocalized} | Rear link {result.rearLinked}\nPose source: {PoseSource}");
        GUILayout.Label(quality+$" | verified {verified}/5\nRear agreement (last comparison): {agreementM:F3} m / {agreementDeg:F1} deg");
        if(!fresh && running)GUILayout.Label("NO FRESH RESULT — target hidden");
        GUILayout.Label(message,new GUIStyle(GUI.skin.label){wordWrap=true});
        if(!running && logPath!=null)GUILayout.Label("Log saved: "+Path.GetFileName(logPath));
        GUILayout.EndArea();GUI.matrix=old;GUI.depth=depth;
    }
}
