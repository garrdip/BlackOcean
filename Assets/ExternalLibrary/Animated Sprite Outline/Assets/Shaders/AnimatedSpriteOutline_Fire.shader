Shader "AnimatedSpriteOutline/Outline Fire"
{
	Properties
	{
		_MainTex("_MainTex", 2D) = "white" {}
		_NoiseTexture1("_NoiseTexture1", 2D) = "white" {}
		_NoiseTexture2("_NoiseTexture2", 2D) = "white" {}
		_MaskUVScale1("_MaskUVScale1", Range(0, 2)) = 0
		_MaskUVScale2("_MaskUVScale2", Range(0, 2)) = 0
		_OutlineColor1("_OutlineColor1", COLOR) = (1, 1, 1, 1)
		_OutlineColor2("_OutlineColor2", COLOR) = (1, 1, 1, 1)
		_OutlineWidth1("_OutlineWidth1", Range(0, 1)) = 0
		_OutlineWidth2("_OutlineWidth2", Range(0, 1)) = 0
		_OutlineWeight1("_OutlineWeight1", Range(0, 4)) = 0
		_OutlineWeight2("_OutlineWeight2", Range(0, 4)) = 0
		_OutlineAccuracy("_OutlineAccuracy", Range(1, 16)) = 8
		_OutlineMaskAlpha("_OutlineMaskAlpha", Range(0, 1)) = 0
		_BloomFallOff("_BloomFallOff", Range(0, 1)) = 1
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
			"IgnoreProjector" = "true"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off

		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert
			#pragma fragment frag

			struct appdata_t
			{
				float4 vertex : POSITION;
				fixed4 color  : COLOR;
				float2 uv     : TEXCOORD0;
			};

			struct v2f
			{
				float4 position : SV_POSITION;
				fixed4 color    : COLOR;
				float2 uv       : TEXCOORD0;
			};

			sampler2D _MainTex;
			sampler2D _NoiseTexture1;
			sampler2D _NoiseTexture2;
			fixed4 _OutlineColor1;
			fixed4 _OutlineColor2;
			float _MaskUVScale1;
			float _MaskUVScale2;
			float _OutlineWidth1;
			float _OutlineWidth2;
			float _OutlineWeight1;
			float _OutlineWeight2;
			float _OutlineAccuracy;
			float _OutlineMaskAlpha;
			float _BloomFallOff;

			float4 Outline(sampler2D mainTex, float2 uv, fixed4 color, float width, float weight)
			{
				int sampleTimes = 0;
				float4 ret = float4(0, 0, 0, 0);
				float step = 8 / _OutlineAccuracy;
				for (float i = -16; i <= 16; i += step)
				{
					for (float j = -16; j <= 16; j += step)
					{
						float2 offset = 0.1 * float2(i, j) / 32 * width;
						float2 offsetUV = saturate(uv + offset);
						ret += tex2D(mainTex, offsetUV);
						++sampleTimes;
					}
				}

				ret = lerp(float4(0, 0, 0, 0), ret / sampleTimes, weight);
				ret.rgb = color.rgb;
				fixed4 c = tex2D(mainTex, uv);
				ret.rgb *= (ret.a - c.a) * 3;
				ret = lerp(ret, c, (c.a > _OutlineMaskAlpha ? 1 : c.a));
				return ret;
			}

			float Distortion(sampler2D noise, float2 uv)
			{
				float2 offsetUV = uv + (_Time.y * float2(0, -0.2));
				return tex2D(noise, offsetUV).r;
			}

			v2f vert(appdata_t i)
			{
				v2f o;
				o.position = UnityObjectToClipPos(i.vertex);
				o.uv = i.uv;
				o.color = i.color;
				return o;
			}

			float4 frag(v2f i) : COLOR
			{
				float4 outline1 = Outline(_MainTex, i.uv, _OutlineColor1, _OutlineWidth1, _OutlineWeight1);
				float4 outline2 = Outline(_MainTex, i.uv, _OutlineColor2, _OutlineWidth2, _OutlineWeight2);

				float4 c = tex2D(_MainTex, i.uv);
				outline1.a = clamp(1 * clamp(outline1.a - c.a, 0, 1) * Distortion(_NoiseTexture1, i.uv * _MaskUVScale1), 0, 1);
				outline2.a = clamp(1 * clamp(outline2.a - c.a - outline1.a, 0, 1) * Distortion(_NoiseTexture2, i.uv * _MaskUVScale2), 0, 1);

				fixed4 ret = (1 - c.a) * (outline1 + outline2) + c * c.a * i.color * 1 / (_BloomFallOff + 1);
				ret.a = clamp((outline1.a + outline2.a + c.a * i.color.a), 0, 1);
				return ret;
			}

			ENDCG
		}
	}

	Fallback "Sprites/Default"
}
