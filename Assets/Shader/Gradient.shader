Shader "Custom/GradientWithSmokeAndTransparentMask"
{
    Properties
    {
        _ColorTop ("Top Color", Color) = (1, 0, 0, 1) // 상단 색상
        _ColorBottom ("Bottom Color", Color) = (0, 0, 1, 1) // 하단 색상
        _Speed ("Speed", Float) = 1.0 // 애니메이션 속도
        _NoiseTex ("Noise Texture", 2D) = "white" {} // 노이즈 텍스처
        _NoiseStrength ("Noise Strength", Float) = 0.5 // 노이즈 강도
        _NoiseScale ("Noise Scale", Float) = 1.0 // 노이즈 스케일
        _MaskTex ("Mask Texture", 2D) = "white" {} // 마스크 텍스처
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha // 알파 블렌딩 설정

        Pass
        {
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

            float4 _ColorTop;
            float4 _ColorBottom;
            float _Speed;
            float _NoiseStrength;
            float _NoiseScale;
            sampler2D _NoiseTex;
            sampler2D _MaskTex;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // 그라데이션 계산
                float gradientPosition = abs(sin(_Time.y * _Speed + i.uv.y * 3.14159));
                float4 baseColor = lerp(_ColorBottom, _ColorTop, gradientPosition);

                // 노이즈 텍스처 샘플링
                float2 noiseUV = i.uv * _NoiseScale + float2(_Time.y * _Speed, _Time.y * _Speed);
                float noiseValue = tex2D(_NoiseTex, noiseUV).r;

                // 노이즈와 그라데이션을 결합
                float4 colorWithNoise = lerp(baseColor, float4(1, 1, 1, 1), noiseValue * _NoiseStrength);

                // 마스크 텍스처 적용
                float maskValue = tex2D(_MaskTex, i.uv).a; // 마스크의 알파 채널 사용

                // 마스크 외부 투명하게 설정
                float4 finalColor = colorWithNoise;
                finalColor.a *= maskValue; // 알파 채널 조정으로 투명도 조절

                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}