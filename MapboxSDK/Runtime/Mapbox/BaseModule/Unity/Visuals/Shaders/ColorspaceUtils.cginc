#ifndef ColorspaceUtils_INCLUDED
#define ColorspaceUtils_INCLUDED

void LinearGammaColorSpace_float(float3 In, out float3 Out)
{
    Out = In;
    #ifdef UNITY_COLORSPACE_GAMMA
    
    #else
        float3 sRGBLo = In * 12.92;
        float3 sRGBHi = (pow(max(abs(In), 1.192092896e-07), float3(1.0 / 2.4, 1.0 / 2.4, 1.0 / 2.4)) * 1.055) - 0.055;
        Out = float3(In <= 0.0031308) ? sRGBLo : sRGBHi;
    #endif
}
#endif