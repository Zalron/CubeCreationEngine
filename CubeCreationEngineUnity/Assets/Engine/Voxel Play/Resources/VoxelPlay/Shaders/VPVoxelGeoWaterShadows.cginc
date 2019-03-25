#include "VPCommon.cginc"


struct appdata {
	float4 vertex   : POSITION;
	float4 uv2		: TEXCOORD1;
};


struct g2f {
	V2F_SHADOW_CASTER;
};


struct vertexInfo {
	float4 vertex;
};


void vert (inout appdata v) {
}


float3 worldCenter;

inline void PushCorner(inout TriangleStream<g2f>o, float3 center, float3 corner) {
	vertexInfo v;
	v.vertex = float4(center + corner, 1.0);
	VOXELPLAY_MODIFY_VERTEX(v.vertex, worldCenter + corner)
	g2f i;
	TRANSFER_SHADOW_CASTER(i);
	o.Append(i);
}


void PushVoxel(float3 center, int4 occi, inout TriangleStream<g2f> o) {
	// cube vertices
	worldCenter = mul(unity_ObjectToWorld, float4(center, 1.0)).xyz;
	float3 viewDir     = _WorldSpaceCameraPos - worldCenter;
	float3 normal      = sign(viewDir);
	float3 viewSide    = saturate(normal);

	// cube vertices
	float3 v0          = cubeVerts[0];
	float3 v1          = cubeVerts[1];
	float3 v2          = cubeVerts[2];
	float3 v3          = cubeVerts[3];
	float3 v4          = -v3;
	float3 v5          = -v2;
	float3 v6          = -v0;
	float3 v7          = -v1;

	float  occFront    = occi.x & 1;
	float  occBack     = (occi.x>>1) & 1;
	float  occTop      = (occi.x>>2) & 1;
	float  occBottom   = (occi.x>>3) & 1;
	float  occLeft     = (occi.x>>4) & 1;
	float  occRight    = (occi.x>>5) & 1;
	float  vertSize    = (occi.y & 16) ? 1.0 : 0.8;

	// Front/back face
	float occ   = lerp( occFront, occBack, viewSide.z );
	if (occ==0) {
		float3 side = lerp(1.0.xxx, float3(-1,1,-1), viewSide.z);
		PushCorner(o, center, v3 * side);
		PushCorner(o, center, v1 * side);
		PushCorner(o, center, v2 * side);
		PushCorner(o, center, v0 * side);
		o.RestartStrip();
	}

	// Left/right face
	occ  = lerp( occLeft, occRight, viewSide.x );
	if (occ==0) {
		float3 side = lerp(1.0.xxx, float3(-1,1,-1), viewSide.x);
		PushCorner(o, center, v4 * side);
		PushCorner(o, center, v7 * side);
		PushCorner(o, center, v0 * side);
		PushCorner(o, center, v2 * side);
		o.RestartStrip();
	}

	// Top/Bottom face
	occ  = lerp( occBottom, occTop, viewSide.y );
	if (occ==0) {
		float3 side = lerp(float3(1.0,1.0,1.0), float3(-1,-1,1), viewSide.y);
		PushCorner(o, center, v4 * side);
		PushCorner(o, center, v0 * side);
		PushCorner(o, center, v5 * side);
		PushCorner(o, center, v1 * side);
		o.RestartStrip();
	}

}


//[maxvertexcount(24)]
[maxvertexcount(12)]
void geom(point appdata i[1], inout TriangleStream<g2f> o) {
	PushVoxel(i[0].vertex.xyz, (int4)i[0].uv2, o);
}


fixed4 frag (g2f i) : SV_Target {
	SHADOW_CASTER_FRAGMENT(i)
}

