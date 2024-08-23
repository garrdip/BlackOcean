Shader "Custom/GradientWithSmokeAndTransparentMask"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 0, 0, 1) // 기본 색상
        _Speed ("Speed", Float) = 1.0 // 애니메이션 속도
        _NoiseTex ("Noise Texture", 2D) = "white" {} // 노이즈 텍스처
        _NoiseStrength ("Noise Strength", Float) = 0.5 // 노이즈 강도
        _NoiseScale ("Noise Scale", Float) = 1.0 // 노이즈 스케일
        _MaskTex ("Mask Texture", 2D) = "white" {} // 마스크 텍스처
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha // 알파 블렌딩 설정
        ZWrite Off // Z-버퍼 쓰기 비활성화
        ZTest LEqual // 깊이 테스트 설정

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

            float4 _BaseColor;
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
                // 기본 색상에 노이즈 애니메이션 추가
                float2 noiseUV = i.uv * _NoiseScale + float2(_Time.y * _Speed, _Time.y * _Speed);
                float noiseValue = tex2D(_NoiseTex, noiseUV).r;

                // 노이즈와 기본 색상을 결합
                float4 colorWithNoise = lerp(_BaseColor, float4(1, 1, 1, 1), noiseValue * _NoiseStrength);

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