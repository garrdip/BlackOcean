Shader "AnimatedSpriteOutline/Particles"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 uv       : TEXCOORD0;
			};

			struct v2f
			{
				float4 position : SV_POSITION;
				fixed4 color    : COLOR;
				float2 uv       : TEXCOORD0;
			};

			sampler2D _MainTex;
			fixed4 _Color;

			v2f vert(appdata_t i)
			{
				v2f o;
				o.position = UnityObjectToClipPos(i.vertex);
				o.uv = i.uv;
				o.color = i.color * _Color;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 c = tex2D(_MainTex, i.uv) * i.color;
				c.rgb = _Color.rgb * c.a * 2;
				return c;
			}

			ENDCG
		}
	}
}