// MainLightShadow.hlsl
// Unity 6 (URP 17) - Shader Graph Custom Function용
// Unlit 그래프에서 메인 라이트(Directional Light) 그림자를 받기 위한 함수
//
// Custom Function 노드 설정
//   Type: File / Source: 이 파일 / Name: MainLightShadow
//   Precision: Single (float)
//   Inputs : WorldPos (Vector3)  <- Position 노드 (Space: World)
//   Outputs: Direction (Vector3), Color (Vector3), ShadowAtten (Float)

#ifndef CUSTOM_MAIN_LIGHT_SHADOW_INCLUDED
#define CUSTOM_MAIN_LIGHT_SHADOW_INCLUDED

#if !defined(SHADERGRAPH_PREVIEW)
    // Unlit 패스에는 라이팅 함수가 포함되지 않을 수 있으므로 직접 포함 (include guard로 중복 안전)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#endif

void MainLightShadow_float(float3 WorldPos, out float3 Direction, out float3 Color, out float ShadowAtten)
{
#if defined(SHADERGRAPH_PREVIEW)
    // 그래프 에디터 미리보기용 기본값
    Direction = normalize(float3(0.5, 0.5, -0.5));
    Color = float3(1.0, 1.0, 1.0);
    ShadowAtten = 1.0;
#else
    // 그림자 좌표 계산 (Screen Space Shadows 렌더러 피처 사용 시 대응)
#if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
    float4 clipPos = TransformWorldToHClip(WorldPos);
    float4 shadowCoord = ComputeScreenPos(clipPos);
#else
    float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
#endif

    Light mainLight = GetMainLight();
    Direction = mainLight.direction;
    Color = mainLight.color;

    // 거리 페이드 없이 실시간 그림자 값만 사용 -> 멀리서도 같은 진하기 유지
    // (Shadow Max Distance 밖에서는 그림자 맵이 없으므로 1 = 그림자 없음)
    ShadowAtten = MainLightRealtimeShadow(shadowCoord);
#endif
}

#endif // CUSTOM_MAIN_LIGHT_SHADOW_INCLUDED