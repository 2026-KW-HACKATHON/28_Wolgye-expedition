using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Mapbox/Others/Animation Curve Container")]
public class AnimationCurveContainer : ScriptableObject
{
    public AnimationCurve Curve;

    public float Evaluate(float zoom)
    {
        return Curve.Evaluate(zoom);
    }
}
