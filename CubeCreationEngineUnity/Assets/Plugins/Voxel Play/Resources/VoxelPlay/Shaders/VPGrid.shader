Shader "Voxel Play/Misc/Grid"
{
	Properties
	{
		_Color ("Color", Color) = (1,0,0,0.5)
		_Size ("Grid Size", Float) = 16
	}
	SubShader
	{
		Tags { "Queue"="Geometry+1" "RenderType"="Opaque" }
		Cull Front
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _Color;
			float _Size;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv     : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv     : TEXCOORD0;
			};


			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv * _Size;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float2 grd = abs(frac(i.uv + 0.5) - 0.5);
				grd /= fwidth(i.uv);
				float  lin = min(grd.x, grd.y);
				float4 col = float4(min(lin.xxx * 0.75 , 1.0), 1.0);
				return col;
			}
			ENDCG
		}
	}
}
