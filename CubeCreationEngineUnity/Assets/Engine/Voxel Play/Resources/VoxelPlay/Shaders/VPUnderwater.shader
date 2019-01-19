Shader "Voxel Play/FX/Underwater"
{
	Properties
	{
		_Color ("Water Color", Color) = (0.4,0.4,1,0.5)
		_WaterLevel ("Water Level", Float) = 60
	}
	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma target 3.0
			#include "UnityCG.cginc"

			fixed4 _Color;
			float _WaterLevel;
			sampler2D _BackgroundTexture;

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 pos     : SV_POSITION;
				fixed light    : TEXCOORD0;
				float3 wpos    : TEXCOORD1;
			};


			v2f vert (appdata v)
			{
				v2f o;
				o.pos     = UnityObjectToClipPos(v.vertex);
				o.wpos 	  = mul(unity_ObjectToWorld, v.vertex);
	       		o.light   = saturate(max(0, _WorldSpaceLightPos0.y * 2.0));
				return o;
			}


			fixed4 frag (v2f i) : SV_Target {

				float dy = _WaterLevel - i.wpos.y;
				clip(dy);

				fixed4 color = _Color;

				// Sun light
				color.rgb *= i.light.x;

				return color;
			}
			ENDCG
		}
	}
}
