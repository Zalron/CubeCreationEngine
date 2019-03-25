#include "VPCommon.cginc"


struct appdata {
	float4 vertex   : POSITION;
	float4 uv       : TEXCOORD0;
};


struct g2f {
	float4 pos    : SV_POSITION;
	float4 uv     : TEXCOORD0;
	VOXELPLAY_LIGHT_DATA(1,2)
	VOXELPLAY_FOG_DATA(3)
	SHADOW_COORDS(4)
};

struct vertexInfo {
	float4 vertex;
};

void vert (inout appdata v) {
}

float3 worldCenter;
inline void PushCorner(inout g2f i, inout TriangleStream<g2f>o, float3 center, float3 corner, float4 uv) {
	vertexInfo v;
	v.vertex = float4(center + corner, 1.0);
	VOXELPLAY_MODIFY_VERTEX(v.vertex, worldCenter + corner)
	i.pos    = UnityObjectToClipPos(v.vertex);
	VOXELPLAY_OUTPUT_UV(uv, i);
	TRANSFER_SHADOW(i);
	o.Append(i);
}


void PushVoxel(float3 center, float4 uv, inout TriangleStream<g2f> o) {
	// cube vertices
	worldCenter = mul(unity_ObjectToWorld, float4(center, 1.0)).xyz;
	float3 viewDir     = _WorldSpaceCameraPos - worldCenter;

	float3 v0          = cubeVerts[0];
	float3 v1          = cubeVerts[1];
	float3 v2          = cubeVerts[2];
	float3 v3          = cubeVerts[3];
	float3 v4          = -v3;
	float3 v5          = -v2;
	float3 v6          = -v0;
	float3 v7          = -v1;

	// wind effect
	float disp = sin(worldCenter.x + _Time.w) * 0.01;
	v7.x += disp;
	v6.x += disp;
	v2.x += disp;
	v3.x += disp;

	g2f i;
	VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_SIMPLE(viewDir);
	VOXELPLAY_SET_LIGHT_WITHOUT_NORMAL(i, worldCenter);

	// Front/back face
	i.light = light.z;
	PushCorner(i, o, center, v1, float4(1, 0, uv.x, uv.w));
	PushCorner(i, o, center, v4, float4(0, 0, uv.x, uv.w));
	PushCorner(i, o, center, v3, float4(1, 0.99, uv.x, uv.w));
	PushCorner(i, o, center, v7, float4(0, 0.99, uv.x, uv.w));
	o.RestartStrip();

	// Left/right face
	i.light  = light.x;
	PushCorner(i, o, center, v0, float4(1, 0, uv.x, uv.w));
	PushCorner(i, o, center, v5, float4(0, 0, uv.x, uv.w));
	PushCorner(i, o, center, v2, float4(1, 0.99, uv.x, uv.w));
	PushCorner(i, o, center, v6, float4(0, 0.99, uv.x, uv.w));
	o.RestartStrip();

}


[maxvertexcount(8)]
void geom(point appdata i[1], inout TriangleStream<g2f> o) {
	PushVoxel(i[0].vertex.xyz, i[0].uv, o);
}


fixed4 frag (g2f i) : SV_Target {

	// Diffuse
	fixed4 color   = VOXELPLAY_GET_TEXEL_GEO(i.uv.xyz);
	clip(color.a - 0.25);

	// AO
	color *= saturate((abs(i.uv.x - 0.5) + 0.33) * 2.0);
	color *= saturate(i.uv.y * 3.0 + 0.82);

	VOXELPLAY_APPLY_LIGHTING_AND_GI(color, i);

	VOXELPLAY_APPLY_FOG(color, i);
	return color;
}

