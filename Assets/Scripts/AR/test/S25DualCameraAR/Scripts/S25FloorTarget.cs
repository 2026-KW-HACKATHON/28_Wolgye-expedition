using UnityEngine;
using UnityEngine.XR.ARFoundation;

// Added by the supplied ARRandomSpawner integration after the actor is initialized.
public sealed class S25FloorTarget : MonoBehaviour
{
    public static S25FloorTarget Active { get; private set; }
    public ARPlane Plane { get; private set; }
    public Transform SessionRoot { get; private set; }
    double[] frontPoint,frontGeometry;
    public bool Ready => isActiveAndEnabled && SessionRoot!=null && (Plane!=null || frontGeometry!=null);
    public void Initialize(ARPlane plane){
        Plane=plane;SessionRoot=plane!=null?plane.transform.parent:null;Active=this;
    }
    public double[] SessionPoint(){
        if(!Ready)return null;
        if(frontPoint!=null)return (double[])frontPoint.Clone();
        Vector3 p=SessionRoot.InverseTransformPoint(transform.position);
        return new double[]{p.x,p.y,-p.z};
    }
    public double[] FloorGeometry(){
        if(!Ready)return null;
        if(frontGeometry!=null)return (double[])frontGeometry.Clone();
        while(Plane.subsumedBy!=null)Plane=Plane.subsumedBy;
        if(Plane.alignment!=UnityEngine.XR.ARSubsystems.PlaneAlignment.HorizontalUp)return null;
        var boundary=Plane.boundary;if(!boundary.IsCreated || boundary.Length<3)return null;
        Vector3 o=SessionRoot.InverseTransformPoint(Plane.transform.position);
        Vector3 u=SessionRoot.InverseTransformVector(Plane.transform.right).normalized;
        Vector3 v=SessionRoot.InverseTransformVector(Plane.transform.forward).normalized;
        Vector3 n=SessionRoot.InverseTransformDirection(Plane.transform.up).normalized;
        Vector3 actor=SessionRoot.InverseTransformPoint(transform.position);
        double[] data=new double[11+2*boundary.Length];
        data[0]=o.x;data[1]=o.y;data[2]=-o.z;data[3]=u.x;data[4]=u.y;data[5]=-u.z;data[6]=v.x;data[7]=v.y;data[8]=-v.z;
        data[9]=Vector3.Dot(actor-o,n);data[10]=boundary.Length;
        for(int i=0;i<boundary.Length;i++){
            Vector3 p=SessionRoot.InverseTransformPoint(Plane.transform.TransformPoint(new Vector3(boundary[i].x,0,boundary[i].y)))-o;
            data[11+i*2]=Vector3.Dot(p,u);data[12+i*2]=Vector3.Dot(p,v);
        }
        return data;
    }
    public void SetFrontPresentation(bool active){
        if(active && frontPoint==null){frontGeometry=FloorGeometry();frontPoint=SessionPoint();}else if(!active){frontPoint=null;frontGeometry=null;}
        // Optional integration method on the supplied behavior script; no hard dependency on game types.
        gameObject.BroadcastMessage("SetTrackingPresentationPaused",active,SendMessageOptions.DontRequireReceiver);
    }
    void OnDisable(){if(Active==this)Active=null;}
}
