#include "VPCommon.cginc"

struct appdata {
	float4 vertex   : POSITION;
	float4 uv       : TEXCOORD0;
	float3 normal   : NORMAL;
	VOXELPLAY_TINTCOLOR_DATA
};


struct v2f {
	float4 pos     : SV_POSITION;
	float4 uv      : TEXCOORD0;
	VOXELPLAY_LIGHT_DATA(1,2)
	VOXELPLAY_FOG_DATA(3)
	VOXELPLAY_TINTCOLOR_DATA
	VOXELPLAY_NORMAL_DATA
};

struct vertexInfo {
	float4 vertex;
};

v2f vert (appdata v) {
	v2f o;
	float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
	VOXELPLAY_MODIFY_VERTEX(v.vertex, worldPos)

	float4 uv = v.uv;

	o.pos    = UnityObjectToClipPos(v.vertex);
	o.uv     = uv;

	VOXELPLAY_OUTPUT_TINTCOLOR(o);
	VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_NORMAL(worldPos, v.normal);
	VOXELPLAY_SET_LIGHT(o, worldPos, v.normal);
	return o;
}


fixed4 frag (v2f i) : SV_Target {

	// Diffuse
//	i.uv.xy = frac(i.uv.xy);
//	fixed4 color   = UNITY_SAMPLE_TEX2DARRAY(_MainTex, i.uv.xyz);

	fixed4 color   = VOXELPLAY_GET_TEXEL_DD(i.uv.xyz);

	#if VOXELPLAY_TRANSP_BLING
	color.ba += (1.0 - color.a) * 0.1 * (frac(_Time.x)>0.99) * (frac(_Time.y + (i.uv.x + i.uv.y) * 0.1) > 0.9);
	#endif

	VOXELPLAY_APPLY_TINTCOLOR(color, i);

	VOXELPLAY_APPLY_LIGHTING_AND_GI(color, i);

	VOXELPLAY_APPLY_FOG(color, i);

	return color;
}

