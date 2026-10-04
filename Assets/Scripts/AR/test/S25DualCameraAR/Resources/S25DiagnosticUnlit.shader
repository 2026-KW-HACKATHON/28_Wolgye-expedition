Shader "S25/DiagnosticUnlit"
{
    Properties { _Color ("Color", Color) = (0.05, 0.95, 0.8, 1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 position : SV_POSITION; };
            fixed4 _Color;
            v2f vert(appdata v) { v2f o; o.position = UnityObjectToClipPos(v.vertex); return o; }
            fixed4 frag(v2f i) : SV_Target { return fixed4(_Color.rgb, 1); }
            ENDCG
        }
    }
}
