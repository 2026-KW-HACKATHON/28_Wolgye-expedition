using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public sealed partial class S25DualCameraAR
{
    private bool markerMode, selectingRear, markerBusy;
    private readonly List<Vector3> rearCorners = new List<Vector3>();
    private readonly List<Vector2> taps = new List<Vector2>();
    private readonly List<List<S25MarkerSolver.Point>> samples = new List<List<S25MarkerSolver.Point>>();
    private readonly List<Pose> samplePoses = new List<Pose>();
    private ARAnchor markerAnchor;
    private Vector3[] markerLocal;
    private Texture2D frozen;
    private Pose frozenPose;
    private Vector3[] frozenPoints;
    private float frozenFx, frozenFy, frozenCx, frozenCy;
    private string markerMessage = "145 mm marker: use your existing print. Put it flat on a detected table.";
    private double[] fitted;
    private bool validated;
    private TrackableId referencePlane;
    private int captureEpoch;

    private void CancelFrozen() { if(frozen!=null)Destroy(frozen); frozen=null; taps.Clear(); }
    private void CleanupMarker() { CancelFrozen(); if(markerAnchor!=null)Destroy(markerAnchor.gameObject); markerMode=false; }
    private void InvalidateMarkerSamples()
    {
        captureEpoch++; CancelFrozen(); samples.Clear(); samplePoses.Clear(); fitted=null; validated=false;
        rearCorners.Clear(); selectingRear=false;
        markerMessage="Tracking changed. Recheck rear A-D alignment, then capture samples again.";
    }
    private float MarkerTop => 180 * Mathf.Max(1, Screen.width / 600f);
    private Rect ImageRect(Texture t)
    {
        float s=Mathf.Min(Screen.width/(float)t.width,(Screen.height-MarkerTop-10f)/(float)t.height);
        return new Rect((Screen.width-t.width*s)/2,MarkerTop+(Screen.height-MarkerTop-10-t.height*s)/2,t.width*s,t.height*s);
    }
    private bool Stable()
    {
        if(ARSession.state!=ARSessionState.SessionTracking || history.Count<2)return false;
        double now=Time.realtimeSinceStartupAsDouble;
        if(now-history[0].time<0.7)return false;
        Pose latest=history[history.Count-1].pose;
        foreach(var p in history)if(now-p.time<0.7 &&
            (Vector3.Distance(p.pose.position,latest.position)>0.008f || Quaternion.Angle(p.pose.rotation,latest.rotation)>0.8f))return false;
        return true;
    }
    private void FreezeMarker()
    {
        if(!running || !haveImage || markerAnchor==null || markerAnchor.trackingState!=TrackingState.Tracking)
        {markerMessage="Start front and register rear A-D first. Anchor must be Tracking.";return;}
        if(!Stable() || sensorAgeMs<0 || sensorAgeMs>500 || Time.realtimeSinceStartup-lastUploadedTime>0.3f)
        {markerMessage="Hold still for 1 second with rear tracking active, then Freeze.";return;}
        CancelFrozen();
        frozen=new Texture2D(videoTexture.width,videoTexture.height,TextureFormat.RGBA32,false);
        frozen.LoadRawTextureData(rgba); frozen.Apply();
        frozenPose=FindPose(imageTime+poseTimeAdjustmentMs/1000.0);
        frozenFx=fx;frozenFy=fy;frozenCx=cx;frozenCy=cy;
        frozenPoints=new Vector3[4];
        for(int i=0;i<4;i++) frozenPoints[i]=Quaternion.Inverse(frozenPose.rotation*Quaternion.Euler(0,180,0)) *
            (markerAnchor.transform.TransformPoint(markerLocal[i])-frozenPose.position);
        showFront=true;
        markerMessage="FROZEN: tap printed A, B, C, D CROSS CENTRES in that order.";
    }
    private async void RegisterMarker()
    {
        // A-B-C-D clockwise around a 145 mm square, all points on the same plane.
        Vector3 a=rearCorners[0],b=rearCorners[1],c=rearCorners[2],d=rearCorners[3];
        Vector3 right=((b-a)+(c-d)).normalized, down=((d-a)+(c-b)).normalized;
        if(Mathf.Abs(Vector3.Dot(right,down))>0.15f) {markerMessage="Not square: restart rear registration.";return;}
        for(int i=0;i<4;i++)if(Mathf.Abs(Vector3.Distance(rearCorners[i],rearCorners[(i+1)%4])-0.145f)>0.0145f)
        {markerMessage="Edge is not 145 +/-14.5 mm. Check printing / AR plane, then restart.";return;}
        Vector3 center=(a+b+c+d)/4;
        down=(down-Vector3.Dot(down,right)*right).normalized;
        var world=new[]{center+(-right-down)*0.0725f,center+(right-down)*0.0725f,
            center+(right+down)*0.0725f,center+(-right+down)*0.0725f};
        for(int i=0;i<4;i++)if(Vector3.Distance(world[i],rearCorners[i])>0.008f)
        {markerMessage="Corner mismatch >8 mm: restart rear registration.";return;}
        if(anchorManager==null){markerMessage="Assign AR Anchor Manager.";return;}
        int token=lifecycle,epoch=captureEpoch; markerBusy=true;
        try
        {
            var result=await anchorManager.TryAddAnchorAsync(new Pose(center,Quaternion.identity));
            if(this==null || lifecycle!=token || epoch!=captureEpoch || !isActiveAndEnabled)
            {if(result.value!=null)Destroy(result.value.gameObject);return;}
            if(!result.status.IsSuccess()){markerMessage="Anchor failed: "+result.status;return;}
            if(markerAnchor!=null)Destroy(markerAnchor.gameObject);
            markerAnchor=result.value;markerLocal=new Vector3[4];
            for(int i=0;i<4;i++)
            {
                markerLocal[i]=markerAnchor.transform.InverseTransformPoint(world[i]);
                var dot=GameObject.CreatePrimitive(PrimitiveType.Sphere);dot.name="Marker "+"ABCD"[i];dot.layer=monsterLayer;
                Destroy(dot.GetComponent<Collider>());dot.transform.SetParent(markerAnchor.transform,false);
                dot.transform.localPosition=markerLocal[i];dot.transform.localScale=Vector3.one*0.012f;
                Shader shader=Resources.Load<Shader>("S25DiagnosticUnlit");
                if(demoMaterial==null && shader!=null){demoMaterial=new Material(shader);demoMaterial.SetColor("_Color",Color.cyan);}
                if(demoMaterial!=null)dot.GetComponent<Renderer>().sharedMaterial=demoMaterial;
            }
            samples.Clear();samplePoses.Clear();fitted=null;validated=false;
            markerMessage="Rear registered. Aim FRONT lens at paper. Capture 3 different views, then a 4th check.";
        }
        catch(Exception e){markerMessage=e.Message;}
        finally{markerBusy=false;}
    }
    private void FinishSample()
    {
        var points=new List<S25MarkerSolver.Point>();
        for(int i=0;i<4;i++)points.Add(new S25MarkerSolver.Point{x=frozenPoints[i].x,y=frozenPoints[i].y,z=frozenPoints[i].z,
            u=taps[i].x,v=taps[i].y,fx=frozenFx,fy=frozenFy,cx=frozenCx,cy=frozenCy});
        // Reject tiny/degenerate targets and taps outside the expected perimeter order.
        float area=0;float sign=0;
        for(int i=0;i<4;i++)
        {
            Vector2 x=taps[(i+1)%4]-taps[i],y=taps[(i+2)%4]-taps[(i+1)%4];
            float cross=x.x*y.y-x.y*y.x;
            if(i==0)sign=Mathf.Sign(cross);
            if(Mathf.Sign(cross)!=sign || x.magnitude<20){markerMessage="Bad corner order / marker too small. Retry.";CancelFrozen();return;}
            area+=taps[i].x*taps[(i+1)%4].y-taps[(i+1)%4].x*taps[i].y;
        }
        if(Mathf.Abs(area)/2<900){markerMessage="Move closer; marker needs more image pixels.";CancelFrozen();return;}
        foreach(Pose p in samplePoses)if(Vector3.Distance(p.position,frozenPose.position)<0.12f && Quaternion.Angle(p.rotation,frozenPose.rotation)<12)
        {markerMessage="View too similar. Move >=12 cm or change angle >=12 degrees.";CancelFrozen();return;}
        if(fitted==null)
        {
            samples.Add(points);samplePoses.Add(frozenPose);
            if(samples.Count==3)
            {
                var all=new List<S25MarkerSolver.Point>();foreach(var view in samples)all.AddRange(view);
                fitted=S25MarkerSolver.Fit(all);
                double error=S25MarkerSolver.Rms(all,fitted);
                markerMessage=$"Fit RMS {error:F2} source px. Capture a NEW 4th view to validate.";
                if(error>3 || Math.Abs(fitted[3])>0.149 || Math.Abs(fitted[4])>0.149 || Math.Abs(fitted[5])>0.149 ||
                    Math.Exp(fitted[6])<0.61 || Math.Exp(fitted[6])>1.59)
                {fitted=null;samples.Clear();samplePoses.Clear();markerMessage="Fit rejected. Check A-D / rear registration; retry 3 views.";}
            }
            else markerMessage=$"Training {samples.Count}/3. Change distance AND sideways angle; hold still.";
        }
        else
        {
            double error=S25MarkerSolver.Rms(points,fitted);
            validated=error<=4;
            markerMessage=$"INDEPENDENT CHECK RMS {error:F2} source px. "+(validated?"PASS: Apply is enabled.":"FAIL: do not apply. Re-register / retry.");
        }
        CancelFrozen();
    }
    private void ApplyMarkerFit()
    {
        if(!validated || fitted==null)return;
        var rv=new Vector3((float)fitted[0],(float)fitted[1],(float)fitted[2]);
        frontAngleCorrection=(rv.magnitude<1e-8f?Quaternion.identity:Quaternion.AngleAxis(rv.magnitude*Mathf.Rad2Deg,rv.normalized)).eulerAngles;
        frontOffsetMeters=Quaternion.Euler(0,180,0)*new Vector3((float)fitted[3],(float)fitted[4],(float)fitted[5]);
        focalScale=(float)Math.Exp(fitted[6]);principalShift=Vector2.zero;useManualFov=false;usePoseHistory=true;mirrorSelfie=false;
        markerMessage="APPLIED for this run. Cyan spheres and yellow + should coincide with printed A-D. Test motion separately.";
        Debug.Log($"S25 calibration offset={frontOffsetMeters:F6}, Euler={frontAngleCorrection:F6}, focal={focalScale:F6}");
    }
    private void MarkerGUI()
    {
        Matrix4x4 old=GUI.matrix; int depth=GUI.depth; GUI.matrix=Matrix4x4.identity;GUI.depth=-1100;
        bool front=showFront && haveImage && videoTexture!=null;
        Rect r=default;
        if(front || frozen!=null)
        {
            Texture t=frozen!=null?frozen:videoTexture;r=ImageRect(t);
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.blackTexture);
            GUI.DrawTexture(r,t,ScaleMode.StretchToFill,false);
            if(frozen==null && virtualTexture!=null && ARSession.state==ARSessionState.SessionTracking)
                GUI.DrawTexture(r,virtualTexture,ScaleMode.StretchToFill,true);
        }
        if(markerAnchor!=null && frozen==null)
        {
            Camera cam=front?frontCamera:rearCamera;
            for(int i=0;i<4;i++)
            {
                Vector3 v=cam.WorldToViewportPoint(markerAnchor.transform.TransformPoint(markerLocal[i]));
                if(v.z<=0)continue;
                Vector2 q=front?new Vector2(r.x+v.x*r.width,r.y+(1-v.y)*r.height):new Vector2(v.x*Screen.width,(1-v.y)*Screen.height);
                GUI.color=Color.yellow;GUI.Label(new Rect(q.x-8,q.y-12,80,30),"+ "+"ABCD"[i]);GUI.color=Color.white;
            }
        }
        for(int i=0;i<taps.Count && frozen!=null;i++)
            GUI.Label(new Rect(r.x+taps[i].x/frozen.width*r.width-8,r.y+taps[i].y/frozen.height*r.height-12,70,30),"+ "+"ABCD"[i]);
        float scale=Mathf.Max(1,Screen.width/600f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
        GUILayout.BeginArea(new Rect(5,5,Screen.width/scale-10,170),GUI.skin.box);
        GUILayout.Label("v4 MARKER 145mm | "+markerMessage,new GUIStyle(GUI.skin.label){wordWrap=true,fontSize=13});
        GUILayout.BeginHorizontal();
        GUI.enabled=!markerBusy;
        if(GUILayout.Button("Rear A-D",GUILayout.Height(30)))
        {captureEpoch++;CancelFrozen();showFront=false;selectingRear=true;rearCorners.Clear();samples.Clear();samplePoses.Clear();fitted=null;validated=false;markerMessage="Tap printed cross centres A, B, C, D on the same detected plane.";}
        if(GUILayout.Button("Front",GUILayout.Height(30))){selectingRear=false;showFront=true;}
        if(GUILayout.Button("Freeze",GUILayout.Height(30)))FreezeMarker();
        GUI.enabled=validated;if(GUILayout.Button("Apply",GUILayout.Height(30)))ApplyMarkerFit();
        GUI.enabled=true;
        if(GUILayout.Button("Exit",GUILayout.Height(30))){CancelFrozen();markerMode=false;selectingRear=false;}
        GUILayout.EndHorizontal();
        if(frozen!=null && GUILayout.Button("Retry this photo"))CancelFrozen();
        GUILayout.EndArea();GUI.matrix=Matrix4x4.identity;
        Event e=Event.current;
        if(e.type==EventType.MouseDown && e.button==0 && e.mousePosition.y>MarkerTop)
        {
            if(frozen!=null && r.Contains(e.mousePosition))
            {
                taps.Add(new Vector2((e.mousePosition.x-r.x)/r.width*frozen.width,(e.mousePosition.y-r.y)/r.height*frozen.height));
                if(taps.Count==4)FinishSample();e.Use();
            }
            else if(selectingRear && !showFront && rearCorners.Count<4)
            {
                if(ARSession.state==ARSessionState.SessionTracking && raycastManager!=null &&
                    raycastManager.Raycast(new Vector2(e.mousePosition.x,Screen.height-e.mousePosition.y),hits,TrackableType.PlaneWithinPolygon))
                {
                    if(rearCorners.Count==0)referencePlane=hits[0].trackableId;
                    if(hits[0].trackableId!=referencePlane)markerMessage="Different plane. Tap same tabletop.";
                    else {rearCorners.Add(hits[0].pose.position);markerMessage=$"Rear {rearCorners.Count}/4";
                        if(rearCorners.Count==4){selectingRear=false;RegisterMarker();}}
                }
                else markerMessage="No tracked plane at tap. Scan table first.";
                e.Use();
            }
        }
        GUI.matrix=old;GUI.depth=depth;
    }
}
