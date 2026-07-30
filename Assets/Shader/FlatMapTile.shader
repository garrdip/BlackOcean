// 3D 구체 맵 타일용 플랫 셰이더.
// 스페큘러/메탈릭 없이 면 방향에 따른 부드러운 음영만 주어 2D 아트 느낌에 맞춘다.
// MaterialPropertyBlock의 _Color를 지원한다 (SphereMapSystem이 타일 색을 이걸로 지정).
Shader "BlackOcean/FlatMapTile"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _ShadeStrength ("Shade Strength", Range(0, 1)) = 0.35
        _AmbientBoost ("Ambient Boost", Range(0, 1)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _ShadeStrength;
            float _AmbientBoost;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float shade : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                // 하프램버트풍: 빛을 등져도 완전히 어두워지지 않는 부드러운 음영
                float ndl = dot(worldNormal, _WorldSpaceLightPos0.xyz) * 0.5 + 0.5;
                o.shade = lerp(1.0 - _ShadeStrength, 1.0, ndl) + _AmbientBoost;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(_Color.rgb * saturate(i.shade), _Color.a);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
