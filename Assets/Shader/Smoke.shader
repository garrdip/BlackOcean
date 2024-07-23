Shader "Custom/AdvancedSmokeEffectButton"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}          // 기본 텍스처
        _SmokeTex ("Smoke Texture", 2D) = "black" {}      // 연기 텍스처
        _Color ("Color", Color) = (1,1,1,1)               // 기본 색상
        _Speed ("Speed", Float) = 1.0                     // 연기 움직임 속도
        _Scale ("Scale", Float) = 1.0                     // 연기 스케일 조절
        _Direction ("Direction", Vector) = (1.0, 0.0, 0.0, 0.0) // 연기 움직임 방향
        _NoiseIntensity ("Noise Intensity", Float) = 0.1  // 노이즈 강도
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 200

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _SmokeTex;
            float4 _MainTex_ST;
            float4 _SmokeTex_ST;
            fixed4 _Color;
            float _Speed;
            float _Scale;
            float4 _Direction;
            float _NoiseIntensity;

            // Perlin Noise Function
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            // Vertex Shader
            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // Fragment Shader
            fixed4 frag (v2f i) : SV_Target
            {
                // 기본 텍스처와 색상 샘플링
                fixed4 baseColor = tex2D(_MainTex, i.uv) * _Color;

                // 시간에 따른 연기 UV 좌표 변형
                float2 smokeUV = i.uv;
                smokeUV += (_Direction.xy * _Time.y * _Speed);

                // Perlin 노이즈를 이용한 랜덤 UV 변형
                float noiseValue = noise(smokeUV * _Scale) * _NoiseIntensity;
                smokeUV.x += noiseValue;
                smokeUV.y += noiseValue;

                // 연기 텍스처 샘플링
                fixed4 smokeColor = tex2D(_SmokeTex, smokeUV);

                // 연기 텍스처의 알파 채널을 기반으로 기본 색상에 연기 효과 추가
                fixed4 finalColor = baseColor;
                finalColor.rgb = lerp(baseColor.rgb, smokeColor.rgb, smokeColor.a * 0.5);

                return finalColor;
            }
            ENDCG
        }
    }
}