using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

// Experimental live local map. Portrait presentation; sensor images remain unmirrored.
// Front localization remains independent of rear tracking after the map is frozen.
[DefaultExecutionOrder(1000)]
public sealed class S25LiveSceneAR : MonoBehaviour
{
    [SerializeField] ARCameraManager rearCameraManager;
    [SerializeField] ARSession arSession;
    [SerializeField] string frontCameraId="1";
    [SerializeField,Range(640,1280)] int trackingMaxDimension=960;
    [SerializeField] Texture2D monsterTexture;
    [SerializeField,Range(.05f,.5f)] float monsterHeightMeters=.16f;
    Camera rearCamera;
    S25FloorTarget floorTarget;
    S25ModelOverlay modelOverlay=new S25ModelOverlay();
    Texture2D actorPreview;
    bool actorVisible,usingFloorActor;
    bool FloorMode => floorTarget!=null && floorTarget.Ready;
    AndroidJavaObject front,localizer;
    bool running,frontView,frozen,placeRequested,wantPlace;
    int points,epoch;long sequence,lastRearStamp,lastFrontStamp;
    double lastRearReceive,lastStatus;
    IntPtr nativeSession;
    bool smoothReady;int smoothCamera,smoothEpoch;long smoothStamp;float displayU,displayV,displayDepth;
    bool lastVisible;string lastVisibilityReason="";
    string message="Start, then slowly move sideways while looking at static furniture.",rawStatus="",rearStatus="";
    Frame queuedRear,queuedFront,inFlight,shown,liveFrame;
    Texture2D livePreview;
    S25LiveSceneHUD hud;
    int viewGeneration;double switchedAt,lastFrontArrival;bool grayFallback;string cameraMessage="";
    Result result=new Result();
    Texture2D preview,defaultMonster;
    string folder="",savedPhoto="",savedBundle="";
    StreamWriter log;
    readonly int[] savedCounts=new int[2];
    readonly double[] lastSaved={-99,-99};
    sealed class Frame {public int camera,w,h,rotation,tw,th,epoch,viewGeneration;public long seq,stamp;public byte[] gray,rgba,track;public double[] k,tk,pose,target,plane;public double received;}
    [Serializable] sealed class Result {public int epoch,camera,points,pending,keyframes,inliers,matches,confirmations;public long seq,timestampNs;public bool valid,frozen,hasTarget,targetVisible,displayOnly,floorMode;public float displayDepth,holdAgeMs;public float u,v,rms,holdout,processingMs;public string state="IDLE",reason;public double[] worldToCamera,target;}
    [Serializable] sealed class FrameMeta {public int camera,width,height,rotation,epoch;public long sequence,timestampNs;public double[] intrinsics,worldToCamera,target;public string pixels,coordinates="ARCore OpenGL session world; raw image top-left origin";}
    void OnEnable(){
        if(rearCameraManager==null)rearCameraManager=FindFirstObjectByType<ARCameraManager>();
        if(arSession==null)arSession=FindFirstObjectByType<ARSession>();
        if(rearCameraManager!=null){rearCamera=rearCameraManager.GetComponent<Camera>();rearCameraManager.frameReceived+=Rear;}
        defaultMonster=MakeMonster();hud=new S25LiveSceneHUD(transform);hud.StartButton.onClick.AddListener(Begin);hud.StopButton.onClick.AddListener(()=>StopRuntime(true));hud.SwitchButton.onClick.AddListener(SwitchView);hud.PlaceButton.onClick.AddListener(()=>wantPlace=true);hud.PhotoButton.onClick.AddListener(SavePhoto);
    }
    void Begin(){
#if UNITY_ANDROID && !UNITY_EDITOR
        if(rearCameraManager==null || arSession==null || rearCamera==null){message="AR Session and rear AR Camera Manager required.";return;}
        if(!Permission.HasUserAuthorizedPermission(Permission.Camera)){Permission.RequestUserPermission(Permission.Camera);message="Allow camera access, then Start again.";return;}
        bool reuseFront=running && front!=null;
        StopRuntime(false,reuseFront);
        foreach(var c in FindObjectsByType<S25JointMapRuntime>(FindObjectsSortMode.None))c.enabled=false;
        foreach(var c in FindObjectsByType<S25FrontMapProbe>(FindObjectsSortMode.None))c.enabled=false;
        foreach(var c in FindObjectsByType<S25DualCameraAR>(FindObjectsSortMode.None))c.enabled=false;
        foreach(var c in FindObjectsByType<S25UnifiedCapture>(FindObjectsSortMode.None))c.enabled=false;
        try{
            floorTarget=S25FloorTarget.Active;
            viewGeneration++;lastFrontArrival=switchedAt=Time.realtimeSinceStartupAsDouble;grayFallback=false;cameraMessage="";usingFloorActor=false;epoch++;points=0;frozen=false;frontView=false;placeRequested=wantPlace=false;result=new Result();nativeSession=IntPtr.Zero;lastRearStamp=lastFrontStamp=0;
            Array.Clear(savedCounts,0,savedCounts.Length);lastSaved[0]=lastSaved[1]=-99;
            folder=Path.Combine(Application.persistentDataPath,"live-scene-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));Directory.CreateDirectory(folder);
            log=new StreamWriter(Path.Combine(folder,"events.jsonl")){AutoFlush=true};
            localizer=new AndroidJavaObject("com.example.s25dualcamera.LiveMapLocalizer",(long)epoch,true);
            using var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer");using var activity=player.GetStatic<AndroidJavaObject>("currentActivity");
            if(front==null){front=new AndroidJavaObject("com.example.s25dualcamera.FrontFrameSource",activity);front.Call("setRawEnabled",true);front.Call("setRawColorEnabled",false);front.Call("start",frontCameraId);}
            else front.Call("setRawColorEnabled",false);
            running=true;message="Scan: move sideways 10-30 cm slowly. Keep the same furniture visible.";
        }catch(Exception e){message=e.Message;StopRuntime(false);}
#else
        message="Build for Android / ARCore.";
#endif
    }
    void Rear(ARCameraFrameEventArgs args){
        if(!running || frontView || Time.realtimeSinceStartupAsDouble-lastRearReceive<.12)return;
        if(!S25RearNativeFrame.TryRead(arSession,rearCameraManager,rearCamera,out var native,out rearStatus))return;
        if(nativeSession!=IntPtr.Zero && nativeSession!=native.session){message="AR session changed. Start a new scan.";StopRuntime(false);return;}nativeSession=native.session;
        if(!rearCameraManager.TryAcquireLatestCpuImage(out XRCpuImage im))return;
        try{
            long stamp=(long)Math.Round(im.timestamp*1e9);
            // Only floating-point timestamp roundoff is tolerated; never pair adjacent frames.
            if(Math.Abs(stamp-native.timestamp)>1000){rearStatus="REAR_FRAME_TIMESTAMP_MISMATCH";return;}
            if(native.timestamp<=lastRearStamp)return;
            if(im.width!=native.width || im.height!=native.height){rearStatus="REAR_INTRINSICS_SIZE_MISMATCH";return;}
            int w=im.width,h=im.height;var p=im.GetPlane(0);byte[] gray=new byte[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)gray[y*w+x]=p.data[y*p.rowStride+x*p.pixelStride];
            queuedRear=new Frame{camera=1,w=w,h=h,rotation=90,epoch=epoch,seq=++sequence,stamp=native.timestamp,gray=gray,k=native.k,pose=native.worldToCamera,received=Time.realtimeSinceStartupAsDouble,target=FloorMode?floorTarget.SessionPoint():null,plane=FloorMode?floorTarget.FloorGeometry():null,viewGeneration=viewGeneration};
            if(!frontView){liveFrame=queuedRear;WritePreview(liveFrame,ref livePreview);}
            lastRearStamp=native.timestamp;lastRearReceive=queuedRear.received;
        }catch(Exception e){message=e.Message;}finally{im.Dispose();}
    }
    void Update(){
        if(!running)return;
        if(usingFloorActor && !FloorMode){message="Floor actor lost. Start a new scan.";StopRuntime(false);return;}
        try{
            if(Time.realtimeSinceStartupAsDouble-lastStatus>.5){lastStatus=Time.realtimeSinceStartupAsDouble;rawStatus=front.Call<string>("rawStatus");}
            string action=localizer.Call<string>("takeAction");
            if(!string.IsNullOrEmpty(action)){
                placeRequested=false;message=action;log?.WriteLine(JsonUtility.ToJson(new ActionLog{action=action}));
                if(action=="TARGET_PLACED"){usingFloorActor=FloorMode;frozen=true;message=FloorMode?"Dalsoo linked. Show FRONT and aim at the same surroundings.":"Target placed. Show FRONT and aim the front lens at the same furniture.";queuedFront=null;}
                else if(action.StartsWith("SAVE_ERROR")){message="Map export failed. Start a new scan. "+action;StopRuntime(false);return;}
            }
            string json=localizer.Call<string>("takeResult");
            if(!string.IsNullOrEmpty(json)){
                log?.WriteLine(json);var r=JsonUtility.FromJson<Result>(json);
                if(inFlight!=null && r.epoch==epoch && r.seq==inFlight.seq && r.camera==inFlight.camera){
                    points=r.points;if(!r.floorMode)frozen=r.frozen;
                    if((frontView?0:1)==r.camera && inFlight.viewGeneration==viewGeneration){shown=inFlight;result=r;UpdateDisplay(shown);ShowFrame(shown);UpdateActor(shown);}
                }
                inFlight=null;
            }
            byte[] b=front.Call<byte[]>("takeRawFrame");
            if(b!=null && b.Length>48){
                int magic=BitConverter.ToInt32(b,0),w=BitConverter.ToInt32(b,4),h=BitConverter.ToInt32(b,8);
                bool color=magic==0x37415253;
                if((!color && magic!=0x35415253) || w<=0 || h<=0 || b.Length!=48L+(color?5L:1L)*w*h)throw new Exception("Invalid paired front frame");
                long id=BitConverter.ToInt64(b,24);
                double captureAge=(front.Call<long>("clockNs")-BitConverter.ToInt64(b,16))*1e-9;
                if(captureAge<-.1 || captureAge>.35){rawStatus="FRONT_FRAME_TOO_OLD";queuedFront=null;}
                else if(id>lastFrontStamp){lastFrontStamp=id;byte[] gray=new byte[w*h],rgba=color?new byte[w*h*4]:null;Buffer.BlockCopy(b,48,gray,0,gray.Length);if(color)Buffer.BlockCopy(b,48+gray.Length,rgba,0,rgba.Length);
                    queuedFront=new Frame{camera=0,w=w,h=h,rotation=BitConverter.ToInt32(b,12),epoch=epoch,seq=++sequence,stamp=BitConverter.ToInt64(b,16),gray=gray,rgba=rgba,k=new double[]{BitConverter.ToSingle(b,32),BitConverter.ToSingle(b,36),BitConverter.ToSingle(b,40),BitConverter.ToSingle(b,44)},received=Time.realtimeSinceStartupAsDouble-Math.Max(0,captureAge),viewGeneration=viewGeneration};
                    lastFrontArrival=Time.realtimeSinceStartupAsDouble;if(frontView){liveFrame=queuedFront;WritePreview(liveFrame,ref livePreview);cameraMessage="FRONT_VIDEO_READY";}}
            }
            if(!frozen && !frontView && !FloorMode && S25FloorTarget.Active!=null)floorTarget=S25FloorTarget.Active;
            if(wantPlace && inFlight==null && !placeRequested){
                placeRequested=FloorMode?localizer.Call<bool>("place",Path.Combine(folder,"map.json"),floorTarget.SessionPoint()):localizer.Call<bool>("place",Path.Combine(folder,"map.json"));
                if(placeRequested)wantPlace=false;
            }
            if(placeRequested || wantPlace || inFlight!=null)return;
            Frame f=frontView?queuedFront:queuedRear;if(f==null)return;
            if(Time.realtimeSinceStartupAsDouble-f.received>.25){if(frontView)queuedFront=null;else queuedRear=null;return;}
            if(FloorMode && f.camera==0){f.target=floorTarget.SessionPoint();f.plane=floorTarget.FloorGeometry();}
            Prepare(f);
            if(frozen && floorTarget!=null && !FloorMode){message="Floor actor lost. Start a new scan.";StopRuntime(false);return;}
            if(localizer.Call<bool>("submit",f.camera,f.seq,f.stamp,f.track,f.tw,f.th,f.tk,f.pose??Array.Empty<double>(),f.target??Array.Empty<double>(),f.plane??Array.Empty<double>())){
                inFlight=f;SaveFrame(f);if(frontView)queuedFront=null;else queuedRear=null;
            }
        }catch(Exception e){message=e.Message;StopRuntime(false);}
    }
    [Serializable] sealed class VisibilityLog {public string type="visibility",reason;public bool visible;public long seq;public float frameAgeMs,processingMs;}
    void LateUpdate(){
        RefreshHUD();if(!running)return;
        bool visible=preview!=null && (!FloorMode || actorVisible) && TargetRect(new Rect(0,0,preview.width,preview.height),out Rect unused);
        string cause=FloorMode && result.valid && result.targetVisible && !actorVisible?"ACTOR_RENDER_UNAVAILABLE":visible?result.state:(!Fresh?"FRAME_STALE":(!result.valid?result.state+(string.IsNullOrEmpty(result.reason)?"":": "+result.reason):"TARGET_OUT_OF_VIEW"));
        if(visible!=lastVisible || cause!=lastVisibilityReason){
            log?.WriteLine(JsonUtility.ToJson(new VisibilityLog{visible=visible,reason=cause,seq=shown==null?0:shown.seq,frameAgeMs=shown==null?-1:(float)((Time.realtimeSinceStartupAsDouble-shown.received)*1000),processingMs=result.processingMs}));
            lastVisible=visible;lastVisibilityReason=cause;
        }
    }
    [Serializable] sealed class ActionLog {public string action;}
    void Prepare(Frame f){
        if(f.track!=null)return;double s=Math.Min(1,Mathf.Clamp(trackingMaxDimension,640,1280)/(double)Math.Max(f.w,f.h));
        f.tw=Math.Max(1,(int)Math.Round(f.w*s));f.th=Math.Max(1,(int)Math.Round(f.h*s));double sx=f.tw/(double)f.w,sy=f.th/(double)f.h;
        f.tk=new[]{f.k[0]*sx,f.k[1]*sy,(f.k[2]+.5)*sx-.5,(f.k[3]+.5)*sy-.5};
        if(f.tw==f.w && f.th==f.h){f.track=f.gray;return;}
        f.track=new byte[f.tw*f.th];
        for(int y=0;y<f.th;y++)for(int x=0;x<f.tw;x++){
            double px=Math.Max(0,Math.Min(f.w-1,(x+.5)/sx-.5)),py=Math.Max(0,Math.Min(f.h-1,(y+.5)/sy-.5));int x0=(int)px,y0=(int)py,x1=Math.Min(x0+1,f.w-1),y1=Math.Min(y0+1,f.h-1);double a=px-x0,b=py-y0;
            f.track[y*f.tw+x]=(byte)Math.Round((f.gray[y0*f.w+x0]*(1-a)+f.gray[y0*f.w+x1]*a)*(1-b)+(f.gray[y1*f.w+x0]*(1-a)+f.gray[y1*f.w+x1]*a)*b);
        }
    }
    Result RearFloorResult(Frame f){
        var p=f.pose;var t=f.target;
        if(p==null || t==null)return new Result{state="NO_FLOOR_TARGET"};
        double z=p[8]*t[0]+p[9]*t[1]+p[10]*t[2]+p[11];
        float u=z>.05?(float)(f.tk[0]*(p[0]*t[0]+p[1]*t[1]+p[2]*t[2]+p[3])/z+f.tk[2]):-1;
        float v=z>.05?(float)(f.tk[1]*(p[4]*t[0]+p[5]*t[1]+p[6]*t[2]+p[7])/z+f.tk[3]):-1;
        return new Result{epoch=epoch,camera=1,seq=f.seq,timestampNs=f.stamp,valid=z>.05,frozen=true,hasTarget=true,target=t,worldToCamera=p,displayDepth=(float)z,u=u,v=v,targetVisible=z>.05&&u>=0&&v>=0&&u<f.tw&&v<f.th,state="REAR_ARCORE_FLOOR"};
    }
    void UpdateActor(Frame f){
        actorVisible=false;
        if(!FloorMode || !result.targetVisible || !(result.valid || result.displayOnly))return;
        float u=(float)((result.u+.5)*f.w/f.tw-.5),v=(float)((result.v+.5)*f.h/f.th-.5);
        Vector3 oldPosition=floorTarget.transform.position;
        try{
            double[] actorPoint=result.floorMode && result.target!=null?result.target:f.target;
            if(actorPoint!=null)floorTarget.transform.position=floorTarget.SessionRoot.TransformPoint(new Vector3((float)actorPoint[0],(float)actorPoint[1],(float)-actorPoint[2]));
            byte[] raw=modelOverlay.Draw(floorTarget,result.worldToCamera,f.k,f.w,f.h,u,v,result.valid);
            if(raw==null)return;
            int w=preview.width,h=preview.height;
            if(actorPreview==null || actorPreview.width!=w || actorPreview.height!=h){if(actorPreview!=null)Destroy(actorPreview);actorPreview=new Texture2D(w,h,TextureFormat.RGBA32,false);}
            byte[] output=new byte[w*h*4];
            int dx=Mathf.RoundToInt((displayU-result.u)*f.w/f.tw),dy=Mathf.RoundToInt((displayV-result.v)*f.h/f.th);
            for(int y=0;y<f.h;y++)for(int x=0;x<f.w;x++){
                int sx=x-dx,sy=y-dy;if(sx<0||sy<0||sx>=f.w||sy>=f.h)continue;
                Rotate(x,y,f.w,f.h,f.rotation,out int ox,out int oy);Buffer.BlockCopy(raw,(sy*f.w+sx)*4,output,((h-1-oy)*w+ox)*4,4);
            }
            actorPreview.LoadRawTextureData(output);actorPreview.Apply(false);actorVisible=true;
        }catch(Exception e){message="Actor render: "+e.Message;modelOverlay.Clear();}
        finally{if(floorTarget!=null)floorTarget.transform.position=oldPosition;}
    }
    void ShowFrame(Frame f){WritePreview(f,ref preview);}
    void WritePreview(Frame f,ref Texture2D preview){
        bool swap=f.rotation%180!=0;int w=swap?f.h:f.w,h=swap?f.w:f.h;
        if(preview==null || preview.width!=w || preview.height!=h){if(preview!=null)Destroy(preview);preview=new Texture2D(w,h,TextureFormat.RGBA32,false);}
        byte[] rgba=new byte[w*h*4];
        for(int y=0;y<f.h;y++)for(int x=0;x<f.w;x++){
            Rotate(x,y,f.w,f.h,f.rotation,out int ox,out int oy);int o=((h-1-oy)*w+ox)*4,i=(y*f.w+x)*4;
            if(f.rgba!=null){rgba[o]=f.rgba[i];rgba[o+1]=f.rgba[i+1];rgba[o+2]=f.rgba[i+2];}else rgba[o]=rgba[o+1]=rgba[o+2]=f.gray[y*f.w+x];rgba[o+3]=255;
        }preview.LoadRawTextureData(rgba);preview.Apply(false);
    }
    static void Rotate(float x,float y,int w,int h,int rotation,out float ox,out float oy){ox=x;oy=y;if(rotation==90){ox=h-1-y;oy=x;}else if(rotation==180){ox=w-1-x;oy=h-1-y;}else if(rotation==270){ox=y;oy=w-1-x;}}
    static void Rotate(int x,int y,int w,int h,int rotation,out int ox,out int oy){Rotate((float)x,y,w,h,rotation,out float a,out float b);ox=(int)a;oy=(int)b;}
    bool Fresh => running && shown!=null && shown.epoch==epoch && shown.camera==(frontView?0:1) && shown.viewGeneration==viewGeneration && Time.realtimeSinceStartupAsDouble-shown.received<.6;
    // Updated exactly once per paired image, shared by preview and saved photograph.
    void UpdateDisplay(Frame f){
        bool allowed=result.targetVisible && (result.valid || result.displayOnly);
        if(!allowed){smoothReady=false;return;}
        float u=result.u,v=result.v,z=result.displayDepth;
        if(float.IsNaN(u)||float.IsInfinity(u)||float.IsNaN(v)||float.IsInfinity(v)||!(z>.05f)||float.IsInfinity(z)){smoothReady=false;return;}
        double dt=(f.stamp-smoothStamp)*1e-9;
        if(!(FloorMode && f.camera==1) && smoothReady && smoothCamera==f.camera && smoothEpoch==f.epoch && dt>0 && dt<.35 && !result.displayOnly){
            float limit=8f*Math.Max(f.tw,f.th)/640f;
            Vector2 delta=new Vector2(displayU-u,displayV-v);
            // Large camera motion snaps immediately; small jitter gets bounded smoothing.
            if(delta.magnitude<limit*3){
                float weight=(float)Math.Exp(-dt/.12);
                delta=Vector2.ClampMagnitude(delta*weight,limit);u+=delta.x;v+=delta.y;
                z=Mathf.Clamp(Mathf.Lerp(z,displayDepth,weight),z*.95f,z*1.05f);
            }
        }
        displayU=u;displayV=v;displayDepth=z;smoothCamera=f.camera;smoothEpoch=f.epoch;smoothStamp=f.stamp;smoothReady=true;
    }
    bool TargetRect(Rect box,out Rect targetRect){
        targetRect=default;if(!Fresh || !smoothReady || !(result.valid || result.displayOnly) || !result.targetVisible)return false;
        // A display-only bridge expires relative to the last verified capture, not each redraw.
        if(result.displayOnly && result.holdAgeMs+(Time.realtimeSinceStartupAsDouble-shown.received)*1000>250)return false;
        float u=(float)((displayU+.5)*shown.w/shown.tw-.5),v=(float)((displayV+.5)*shown.h/shown.th-.5);
        Rotate(u,v,shown.w,shown.h,shown.rotation,out float x,out float y);
        float scale=box.width/preview.width,side=(float)(Math.Min(shown.k[0],shown.k[1])*monsterHeightMeters/displayDepth)*scale;
        if((float.IsNaN(side)||float.IsInfinity(side))||side<1||side>box.height)return false;
        targetRect=new Rect(box.x+x*scale-side*.5f,box.y+y*scale-side*.5f,side,side);return true;
    }
    void SaveFrame(Frame f){
        // Bounded diagnostics, not every frame. Save map-building and front samples with exact K/pose.
        if(savedCounts[f.camera]>=36 || f.received-lastSaved[f.camera]<.35)return;lastSaved[f.camera]=f.received;savedCounts[f.camera]++;
        string name=(f.camera==0?"front-":"rear-")+f.seq;
        using(var stream=new FileStream(Path.Combine(folder,name+".pgm"),FileMode.Create)){
            byte[] header=System.Text.Encoding.ASCII.GetBytes($"P5\n{f.w} {f.h}\n255\n");stream.Write(header,0,header.Length);stream.Write(f.gray,0,f.gray.Length);}
        File.WriteAllText(Path.Combine(folder,name+".json"),JsonUtility.ToJson(new FrameMeta{camera=f.camera,width=f.w,height=f.h,rotation=f.rotation,epoch=f.epoch,sequence=f.seq,timestampNs=f.stamp,intrinsics=f.k,worldToCamera=f.pose,target=f.target,pixels=name+".pgm"},true));
    }
    void SavePhoto(){
        if(!result.valid || preview==null || !TargetRect(new Rect(0,0,preview.width,preview.height),out Rect r)){message="Photo requires a currently verified target.";return;}
        if(FloorMode){
            if(!actorVisible || actorPreview==null){message="No verified actor image.";return;}
            Color32[] background=preview.GetPixels32(),overlay=actorPreview.GetPixels32();
            for(int i=0;i<background.Length;i++){Color32 a=overlay[i],b=background[i];float alpha=a.a/255f;background[i]=new Color32((byte)(a.r*alpha+b.r*(1-alpha)),(byte)(a.g*alpha+b.g*(1-alpha)),(byte)(a.b*alpha+b.b*(1-alpha)),255);}
            var composed=new Texture2D(preview.width,preview.height,TextureFormat.RGBA32,false);composed.SetPixels32(background);composed.Apply();savedPhoto=Path.Combine(folder,"photo-"+DateTime.UtcNow.ToString("HHmmssfff")+".png");File.WriteAllBytes(savedPhoto,composed.EncodeToPNG());Destroy(composed);message="Dalsoo photo saved: "+Path.GetFileName(savedPhoto);return;
        }
        Texture2D tex=monsterTexture!=null?monsterTexture:defaultMonster;
        // Readable custom sprite required for deterministic CPU composition.
        if(!tex.isReadable){message="Enable Read/Write on the monster texture to save photos.";return;}
        Color32[] dst=preview.GetPixels32(),src=tex.GetPixels32();int w=preview.width,h=preview.height;
        for(int y=Math.Max(0,(int)r.y);y<Math.Min(h,(int)Math.Ceiling(r.yMax));y++)for(int x=Math.Max(0,(int)r.x);x<Math.Min(w,(int)Math.Ceiling(r.xMax));x++){
            int sx=Mathf.Clamp((int)((x-r.x)/r.width*tex.width),0,tex.width-1),sy=Mathf.Clamp((int)((1-(y-r.y)/r.height)*tex.height),0,tex.height-1);Color32 c=src[sy*tex.width+sx];int i=(h-1-y)*w+x;float a=c.a/255f;Color32 d=dst[i];dst[i]=new Color32((byte)(c.r*a+d.r*(1-a)),(byte)(c.g*a+d.g*(1-a)),(byte)(c.b*a+d.b*(1-a)),255);
        }
        var photo=new Texture2D(w,h,TextureFormat.RGBA32,false);photo.SetPixels32(dst);photo.Apply();savedPhoto=Path.Combine(folder,"photo-"+DateTime.UtcNow.ToString("HHmmssfff")+".png");File.WriteAllBytes(savedPhoto,photo.EncodeToPNG());Destroy(photo);message="Photo saved: "+Path.GetFileName(savedPhoto);
    }
    void StopRuntime(bool bundle,bool keepCamera=false){
        if(floorTarget!=null)floorTarget.SetFrontPresentation(false);actorVisible=false;modelOverlay.Clear();
        smoothReady=false;running=false;placeRequested=wantPlace=false;result=new Result();queuedFront=queuedRear=inFlight=shown=null;
        try{if(!keepCamera)front?.Call("dispose");localizer?.Call("dispose");}catch(Exception e){Debug.LogWarning(e.Message);}
        if(!keepCamera){front?.Dispose();front=null;}localizer?.Dispose();localizer=null;
        log?.Dispose();log=null;
        if(bundle && !string.IsNullOrEmpty(folder) && Directory.Exists(folder)){
            try{savedBundle=folder+".zip";if(File.Exists(savedBundle))File.Delete(savedBundle);ZipFile.CreateFromDirectory(folder,savedBundle);message="Saved: "+savedBundle;}catch(Exception e){message="Save failed: "+e.Message;}
        }
    }
    void OnApplicationPause(bool pause){if(pause && running){StopRuntime(!placeRequested);message="Paused. Start a new scan when returning.";}}
    void OnDisable(){StopRuntime(false);if(rearCameraManager!=null)rearCameraManager.frameReceived-=Rear;if(preview!=null)Destroy(preview);if(defaultMonster!=null)Destroy(defaultMonster);if(actorPreview!=null)Destroy(actorPreview);if(livePreview!=null)Destroy(livePreview);modelOverlay.Dispose();hud?.Dispose();hud=null;}
    void SwitchView(){
        if(!running || !(FloorMode || frozen))return;
        try{
            bool next=!frontView;
            // Keep the latest image until a frame from the requested camera arrives.
            front.Call("setRawColorEnabled",next);
            if(FloorMode)floorTarget.SetFrontPresentation(next);
            usingFloorActor=FloorMode;frontView=next;viewGeneration++;switchedAt=Time.realtimeSinceStartupAsDouble;grayFallback=false;
            smoothReady=false;actorVisible=false;modelOverlay.Clear();result=new Result{state=next?"SEARCHING_FLOOR":"FLOOR_SCAN_MOVE_SIDEWAYS"};queuedFront=queuedRear=null;
            cameraMessage=next?"WAITING_FRONT_FRAME (previous image held)":"WAITING_REAR_FRAME (previous image held)";
            message=next?"Aim at the previously scanned floor. Spawns automatically when confirmed.":"Scan the floor sideways. Show FRONT needs no Link button.";
        }catch(Exception e){message="Camera switch failed: "+e.Message;}
    }
    void RefreshHUD(){
        if(hud==null)return;
        Rect targetBox=default;bool visible=preview!=null && TargetRect(new Rect(0,0,preview.width,preview.height),out targetBox) && (!FloorMode || actorVisible);
        Texture2D image=visible?preview:livePreview;
        bool currentVideo=liveFrame!=null && liveFrame.camera==(frontView?0:1) && liveFrame.viewGeneration==viewGeneration;
        string video=running?(currentVideo?(frontView?"FRONT":"REAR")+" video | age "+(Time.realtimeSinceStartupAsDouble-liveFrame.received).ToString("F1")+"s":cameraMessage):"STOPPED: "+message;
        string status="U7.4 AUTO FLOOR\n"+(frontView?"FRONT":"REAR")+" | "+result.state+" | floor points "+points+"\n"+video+"\n"+(FloorMode?"Actor: existing Dalsoo":"Actor: test sprite")+"\n"+message+"\nFront: "+rawStatus+"\n"+rearStatus;
        hud.UpdateView(image,visible?(FloorMode?actorPreview:(monsterTexture!=null?monsterTexture:defaultMonster)):null,FloorMode,visible?targetBox:default,status);
        hud.SwitchButton.interactable=running && (FloorMode || frozen);hud.SetSwitchLabel(frontView?"Show REAR":"Show FRONT");
        hud.PlaceButton.gameObject.SetActive(!FloorMode);hud.PlaceButton.interactable=running&&!frozen&&points>=50&&!placeRequested&&!wantPlace&&shown!=null&&shown.camera==1&&Fresh;
        hud.PhotoButton.interactable=visible && result.valid;hud.StopButton.interactable=running&&!placeRequested;
        if(running && frontView && !grayFallback && Time.realtimeSinceStartupAsDouble-Math.Max(switchedAt,lastFrontArrival)>2){
            grayFallback=true;try{front.Call("setRawColorEnabled",false);cameraMessage="FRONT_GRAY_RETRY";message="Waiting for front frames; trying grayscale capture.";}catch(Exception e){message="Front capture: "+e.Message;}
        }
    }
    static Texture2D MakeMonster(){
        const int n=96;var t=new Texture2D(n,n,TextureFormat.RGBA32,false);var p=new Color32[n*n];
        for(int y=0;y<n;y++)for(int x=0;x<n;x++){
            float dx=(x-48)/35f,dy=(y-43)/33f;bool body=dx*dx+dy*dy<1,ear=(x>18&&x<33&&y>62&&y<88)||(x>63&&x<78&&y>62&&y<88);
            if(body||ear)p[y*n+x]=new Color32(75,215,170,255);
            if(((x-35)*(x-35)+(y-49)*(y-49)<35)||((x-61)*(x-61)+(y-49)*(y-49)<35))p[y*n+x]=new Color32(255,255,255,255);
            if(((x-35)*(x-35)+(y-49)*(y-49)<9)||((x-61)*(x-61)+(y-49)*(y-49)<9))p[y*n+x]=new Color32(20,35,45,255);
            if(y>30&&y<34&&x>40&&x<56)p[y*n+x]=new Color32(20,70,65,255);
        }t.SetPixels32(p);t.Apply();return t;
    }
}
