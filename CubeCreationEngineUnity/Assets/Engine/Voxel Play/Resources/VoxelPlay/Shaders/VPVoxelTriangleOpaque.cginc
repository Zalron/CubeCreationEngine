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
	SHADOW_COORDS(4)
	VOXELPLAY_TINTCOLOR_DATA
	VOXELPLAY_BUMPMAP_DATA(5)
	VOXELPLAY_PARALLAX_DATA(6)
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
	#if defined(VP_CUTOUT)
		int iuvz = (int)uv.z;
		float disp = (iuvz>>16) * sin(worldPos.x + worldPos.y + _Time.w) * 0.005;
		v.vertex.xy += disp;
		uv.z = iuvz & 65535; // remove wind animation flag
	#endif

	o.pos    = UnityObjectToClipPos(v.vertex);

	VOXELPLAY_OUTPUT_TINTCOLOR(o);
	VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_NORMAL(worldPos, v.normal);
	VOXELPLAY_SET_LIGHT(o, worldPos, v.normal);
	TRANSFER_SHADOW(o);

	float3 tang = float3( dot(float3(0,1,-1), v.normal), 0, dot(float3(1,0,0), v.normal) );
	VOXELPLAY_SET_TANGENT_SPACE(tang, v.normal)
	VOXELPLAY_OUTPUT_PARALLAX_DATA(v, uv, o)
	VOXELPLAY_OUTPUT_NORMAL_DATA(uv, o)
	VOXELPLAY_OUTPUT_UV(uv, o)

	return o;
}


fixed4 frag (v2f i) : SV_Target {

	VOXELPLAY_APPLY_PARALLAX(i);

	// Diffuse
	fixed4 color   = VOXELPLAY_GET_TEXEL_DD(i.uv.xyz);

	#if defined(VP_CUTOUT)
	clip(color.a - 0.5);
	#endif

	VOXELPLAY_COMPUTE_EMISSION(color)

	VOXELPLAY_APPLY_NORMAL(i);

	VOXELPLAY_APPLY_TINTCOLOR(color, i);

	VOXELPLAY_APPLY_OUTLINE_SIMPLE(color, i);

	VOXELPLAY_APPLY_LIGHTING_AO_AND_GI(color, i);

	VOXELPLAY_ADD_EMISSION(color)

	VOXELPLAY_APPLY_FOG(color, i);

	return color;
}

