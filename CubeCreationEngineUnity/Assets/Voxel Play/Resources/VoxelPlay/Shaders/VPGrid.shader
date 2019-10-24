Shader "Voxel Play/Misc/Grid"
{
	Properties
	{
		_Color ("Color", Color) = (1,0,0,0.5)
		_Size ("Grid Size", Vector) = (16, 16, 16)
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
			float3 _Size;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv     : TEXCOORD0;
                float3 normal : NORMAL;
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
                if (v.normal.z != 0) {
                    o.uv = v.uv * float2(_Size.x, _Size.y);
                } else if (v.normal.y != 0) {
                    o.uv = v.uv * float2(_Size.x, _Size.z);
                } else {
                    o.uv = v.uv * float2(_Size.z, _Size.y);
                }
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
