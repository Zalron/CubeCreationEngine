#include "VPCommon.cginc"

struct appdata {
	float4 vertex   : POSITION;
};


struct g2f {
	V2F_SHADOW_CASTER;
};


struct vertexInfo {
	float4 vertex;
};


void vert (inout appdata v) {
}


inline void PushCorner(inout TriangleStream<g2f>o, float3 center, float3 corner) {
	vertexInfo v;
	v.vertex = float4(center + corner, 1.0);
	VOXELPLAY_MODIFY_VERTEX_NO_WPOS(v.vertex)
	g2f i;
	TRANSFER_SHADOW_CASTER(i);
	o.Append(i);
}



void PushVoxel(float3 center, inout TriangleStream<g2f> o) {
	// cube vertices
	float3 v0          = cubeVerts[0];
	float3 v1          = cubeVerts[1];
	float3 v2          = cubeVerts[2];
	float3 v3          = cubeVerts[3];
	float3 v4          = -v3;
	float3 v5          = -v2;
	float3 v6          = -v0;
	float3 v7          = -v1;

	// Front/back face
	PushCorner(o, center, v3);
	PushCorner(o, center, v1);
	PushCorner(o, center, v2);
	PushCorner(o, center, v0);
	o.RestartStrip();

	PushCorner(o, center, v7);
	PushCorner(o, center, v4);
	PushCorner(o, center, v6);
	PushCorner(o, center, v5);
	o.RestartStrip();

	// Left/right face
	PushCorner(o, center, v4);
	PushCorner(o, center, v7);
	PushCorner(o, center, v0);
	PushCorner(o, center, v2);
	o.RestartStrip();

	PushCorner(o, center, v1);
	PushCorner(o, center, v3);
	PushCorner(o, center, v5);
	PushCorner(o, center, v6);
	o.RestartStrip();

	// Top/Bottom face
	PushCorner(o, center, v0);
	PushCorner(o, center, v1);
	PushCorner(o, center, v4);
	PushCorner(o, center, v5);
	o.RestartStrip();

	PushCorner(o, center, v3);
	PushCorner(o, center, v2);
	PushCorner(o, center, v6);
	PushCorner(o, center, v7);
	o.RestartStrip();

}


[maxvertexcount(24)]
void geom(point appdata i[1], inout TriangleStream<g2f> o) {
	PushVoxel(i[0].vertex.xyz, o);
}


fixed4 frag (g2f i) : SV_Target {
	SHADOW_CASTER_FRAGMENT(i)
}

