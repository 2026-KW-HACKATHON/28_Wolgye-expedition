using System;
using UnityEngine;
using UnityEngine.Rendering;

// Render only the existing actor, at its current world transform. Never clones game logic.
public sealed class S25ModelOverlay : IDisposable
{
    Camera camera;RenderTexture rt;Texture2D pixels;byte[] cached;
    float cachedU,cachedV;
    public byte[] Draw(S25FloorTarget actor,double[] pose,double[] k,int w,int h,float u,float v,bool verified){
        if(actor==null || !actor.Ready)return null;
        if(rt==null || rt.width!=w || rt.height!=h){ReleaseTexture();rt=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32);rt.Create();pixels=new Texture2D(w,h,TextureFormat.RGBA32,false);cached=null;}
        if(verified){
            if(pose==null || pose.Length!=12)return null;
            if(camera==null){var go=new GameObject("S25 actor capture camera");go.hideFlags=HideFlags.HideAndDontSave;camera=go.AddComponent<Camera>();camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.allowHDR=false;camera.allowMSAA=false;}
            Vector3 center=new Vector3(
                (float)-(pose[0]*pose[3]+pose[4]*pose[7]+pose[8]*pose[11]),
                (float)-(pose[1]*pose[3]+pose[5]*pose[7]+pose[9]*pose[11]),
                (float)(pose[2]*pose[3]+pose[6]*pose[7]+pose[10]*pose[11]));
            Vector3 forward=new Vector3((float)pose[8],(float)pose[9],(float)-pose[10]);
            Vector3 up=new Vector3((float)-pose[4],(float)-pose[5],(float)pose[6]);
            Transform root=actor.SessionRoot;
            camera.transform.SetPositionAndRotation(root.TransformPoint(center),Quaternion.LookRotation(root.TransformDirection(forward),root.TransformDirection(up)));
            float n=.01f;camera.nearClipPlane=n;camera.farClipPlane=100;
            camera.projectionMatrix=Matrix4x4.Frustum((float)(-(k[2]+.5)/k[0]*n),(float)((w-k[2]-.5)/k[0]*n),(float)(-(h-k[3]-.5)/k[1]*n),(float)((k[3]+.5)/k[1]*n),n,100);
            camera.targetTexture=rt;camera.cullingMask=1<<31;
            // Layer changes exist only during this synchronous off-screen render and are restored even on failure.
            var renderers=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int[] layers=new int[renderers.Length];bool[] forced=new bool[renderers.Length];
            RenderTexture previous=RenderTexture.active;
            try{
                for(int i=0;i<renderers.Length;i++){layers[i]=renderers[i].gameObject.layer;forced[i]=renderers[i].forceRenderingOff;}
                for(int i=0;i<renderers.Length;i++){var r=renderers[i];
                    bool own=r.transform.IsChildOf(actor.transform);if(own){r.gameObject.layer=31;}else if(layers[i]==31)r.forceRenderingOff=true;}
                if(GraphicsSettings.currentRenderPipeline==null)camera.Render();
                else {var request=new RenderPipeline.StandardRequest{destination=rt};if(!RenderPipeline.SupportsRenderRequest(camera,request))throw new InvalidOperationException("Render pipeline does not support StandardRequest");RenderPipeline.SubmitRenderRequest(camera,request);}
                RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,w,h),0,0,false);pixels.Apply(false);cached=pixels.GetRawTextureData<byte>().ToArray();cachedU=u;cachedV=v;
            }finally{
                RenderTexture.active=previous;for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null){renderers[i].gameObject.layer=layers[i];renderers[i].forceRenderingOff=forced[i];}
            }
        }
        if(cached==null)return null;
        // Return top-left sensor pixels. For a brief hold, transport the last model image with verified flow.
        byte[] result=new byte[w*h*4];int dx=verified?0:Mathf.RoundToInt(u-cachedU),dy=verified?0:Mathf.RoundToInt(v-cachedV);
        for(int y=0;y<h;y++)for(int x=0;x<w;x++){int sx=x-dx,sy=y-dy;if(sx<0||sy<0||sx>=w||sy>=h)continue;Buffer.BlockCopy(cached,((h-1-sy)*w+sx)*4,result,(y*w+x)*4,4);}
        return result;
    }
    public void Clear(){cached=null;}
    void ReleaseTexture(){if(rt!=null){rt.Release();UnityEngine.Object.Destroy(rt);rt=null;}if(pixels!=null){UnityEngine.Object.Destroy(pixels);pixels=null;}}
    public void Dispose(){ReleaseTexture();if(camera!=null)UnityEngine.Object.Destroy(camera.gameObject);camera=null;cached=null;}
}
