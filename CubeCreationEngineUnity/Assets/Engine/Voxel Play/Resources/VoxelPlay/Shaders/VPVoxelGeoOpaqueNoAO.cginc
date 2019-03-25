#include "VPCommon.cginc"

struct appdata {
	float4 vertex   : POSITION;
	float4 uv       : TEXCOORD0;
	VOXELPLAY_TINTCOLOR_DATA
};


struct g2f {
	float4 pos    : SV_POSITION;
	float3 uv     : TEXCOORD0;
	VOXELPLAY_LIGHT_DATA(1,2)
	VOXELPLAY_FOG_DATA(3)
	#if !defined(NO_SELF_SHADOWS)
	SHADOW_COORDS(4)
	#endif
	VOXELPLAY_TINTCOLOR_DATA
	VOXELPLAY_NORMAL_DATA
};

struct vertexInfo {
	float4 vertex;
};

void vert (inout appdata v) {
}


float3 worldCenter;

inline void PushCorner(inout g2f i, inout TriangleStream<g2f>o, float3 center, float3 corner, float3 uv) {
	vertexInfo v;
	v.vertex = float4(center + corner, 1.0);
	i.pos    = UnityObjectToClipPos(v.vertex);
	i.uv     = uv;
	#if !defined(NO_SELF_SHADOWS)
	TRANSFER_SHADOW(i);
	#endif
	o.Append(i);
}



#if VOXELPLAY_USE_TINTING
void PushVoxelNoAO(float3 center, float4 uv, fixed3 color, inout TriangleStream<g2f> o) {
#else
void PushVoxelNoAO(float3 center, float4 uv, inout TriangleStream<g2f> o) {
#endif
	// cube vertices
	worldCenter = mul(unity_ObjectToWorld, float4(center, 1.0)).xyz;
	float3 viewDir     = _WorldSpaceCameraPos - worldCenter;
	float3 normal      = sign(viewDir);
	float3 viewSide    = saturate(normal);

	float3 v0          = cubeVerts[0];
	float3 v1          = cubeVerts[1];
	float3 v2          = cubeVerts[2];
	float3 v3          = cubeVerts[3];
	float3 v4          = -v3;
	float3 v5          = -v2;
	float3 v7          = -v1;

	g2f i;
	VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_GEO(viewDir, normal);
	VOXELPLAY_SET_TINTCOLOR(color, i);

	float3 side;
	int iuvw = (int)uv.w;

	// Top/Bottom face
	float occludes  = lerp(iuvw & 8, iuvw & 4, viewSide.y);
	if (occludes == 0) {
		VOXELPLAY_SET_LIGHT(i, worldCenter, float3(0,normal.y,0))
		i.light  = light.y;
		side = lerp(1.0.xxx, float3(-1,-1,1), viewSide.y);
		float sideUV = lerp(uv.z, uv.y, viewSide.y);
		PushCorner(i, o, center, v0 * side, float3(0, 1, sideUV));
		PushCorner(i, o, center, v1 * side, float3(1, 1, sideUV));
		PushCorner(i, o, center, v4 * side, float3(0, 0, sideUV));
		PushCorner(i, o, center, v5 * side, float3(1, 0, sideUV));
		o.RestartStrip();
	}

	// Front/back face
	occludes  = lerp(iuvw & 1, iuvw & 2, viewSide.z);
	if (occludes == 0) {
		VOXELPLAY_SET_LIGHT(i, worldCenter, float3(0,0,normal.z))
		i.light = light.z;
		float3 side = lerp(1.0.xxx, float3(-1,1,-1), viewSide.z);
		PushCorner(i, o, center, v1 * side, float3(1, 0, uv.x));
		PushCorner(i, o, center, v0 * side, float3(0, 0, uv.x));
		PushCorner(i, o, center, v3 * side, float3(1, 1, uv.x));
		PushCorner(i, o, center, v2 * side, float3(0, 1, uv.x));
		o.RestartStrip();
	}

	// Left/right face
	occludes  = lerp(iuvw & 16, iuvw & 32, viewSide.x);
	if (occludes == 0) {
		VOXELPLAY_SET_LIGHT(i, worldCenter, float3(normal.x,0,0))
		i.light  = light.x;
		side = lerp(1.0.xxx, float3(-1,1,-1), viewSide.x);
		PushCorner(i, o, center, v0 * side, float3(1, 0, uv.x));
		PushCorner(i, o, center, v4 * side, float3(0, 0, uv.x));
		PushCorner(i, o, center, v2 * side, float3(1, 1, uv.x));
		PushCorner(i, o, center, v7 * side, float3(0, 1, uv.x));
		o.RestartStrip();
	}

}


[maxvertexcount(12)]
void geom(point appdata i[1], inout TriangleStream<g2f> o) {
#if VOXELPLAY_USE_TINTING
	PushVoxelNoAO(i[0].vertex.xyz, i[0].uv, i[0].color, o);
#else
	PushVoxelNoAO(i[0].vertex.xyz, i[0].uv, o);
#endif
}


fixed4 frag (g2f i) : SV_Target {

	// Diffuse
	fixed4 color   = VOXELPLAY_GET_TEXEL_GEO(i.uv.xyz);

	VOXELPLAY_APPLY_TINTCOLOR(color, i)

	VOXELPLAY_APPLY_LIGHTING(color, i);

//	VOXELPLAY_APPLY_FOG(color, i); // <-- disabled fog on clouds to let them appear on longer distances

	return color;
}

