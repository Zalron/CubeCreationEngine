#include "VPCommon.cginc"

struct appdata {
	float4 vertex   : POSITION;
	float3 uv       : TEXCOORD0;
};


struct g2f {
	V2F_SHADOW_CASTER;
	float3 uv     : TEXCOORD1;
};

struct vertexInfo {
	float4 vertex;
};

void vert (inout appdata v) {
}


float3 worldCenter;
inline void PushCorner(inout TriangleStream<g2f>o, float3 center, float3 corner, float3 uv) {
	vertexInfo v;
	v.vertex = float4(center + corner, 1.0);
	VOXELPLAY_MODIFY_VERTEX(v.vertex, worldCenter + corner)
	g2f i;
	i.uv     = uv;
	TRANSFER_SHADOW_CASTER(i);
	o.Append(i);
}



void PushVoxel(float3 center, float3 uv, inout TriangleStream<g2f> o) {
	// cube vertices
	float3 v0          = cubeVerts[0];
	float3 v1          = cubeVerts[1];
	float3 v2          = cubeVerts[2];
	float3 v3          = cubeVerts[3];
	float3 v4          = -v3;
	float3 v5          = -v2;
	float3 v6          = -v0;
	float3 v7          = -v1;

	// wind effect
	worldCenter = mul(unity_ObjectToWorld, float4(center, 1.0)).xyz;
	float disp = sin(worldCenter.x + _Time.w) * 0.01;
	v7.x += disp;
	v6.x += disp;
	v2.x += disp;
	v3.x += disp;

	// Front/back face
	PushCorner(o, center, v1, float3(1, 0, uv.x));
	PushCorner(o, center, v4, float3(0, 0, uv.x));
	PushCorner(o, center, v3, float3(1, 1, uv.x));
	PushCorner(o, center, v7, float3(0, 1, uv.x));
	o.RestartStrip();

	// Left/right face
	PushCorner(o, center, v0, float3(1, 0, uv.x));
	PushCorner(o, center, v5, float3(0, 0, uv.x));
	PushCorner(o, center, v2, float3(1, 1, uv.x));
	PushCorner(o, center, v6, float3(0, 1, uv.x));
	o.RestartStrip();

}


[maxvertexcount(8)]
void geom(point appdata i[1], inout TriangleStream<g2f> o) {
	PushVoxel(i[0].vertex.xyz, i[0].uv, o);
}


fixed4 frag (g2f i) : SV_Target {
	fixed4 color   = UNITY_SAMPLE_TEX2DARRAY(_MainTex, i.uv.xyz);
	clip(color.a - 0.25);
	SHADOW_CASTER_FRAGMENT(i)
}

