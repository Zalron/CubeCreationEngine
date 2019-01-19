#include "VPCommon.cginc"


struct appdata {
	float4 vertex   : POSITION;
	float4 uv       : TEXCOORD0;
	float4 uv2		: TEXCOORD1;
	VOXELPLAY_TINTCOLOR_DATA
};


struct g2f {
	float4 pos    : SV_POSITION;
	float4 uv     : TEXCOORD0;
	VOXELPLAY_LIGHT_DATA(1,2)
	VOXELPLAY_FOG_DATA(3)
	VOXELPLAY_TINTCOLOR_DATA
	VOXELPLAY_NORMAL_DATA
};

struct vertexInfo {
	float4 vertex;
};


void vert (inout appdata v) {
}


fixed colorVariation;
float3 worldCenter, norm;

inline void PushCorner(inout g2f i, inout TriangleStream<g2f>o, float3 center, float3 corner, float4 uv) {
	vertexInfo v;
	v.vertex = float4(center + corner, 1.0);
	i.pos    = UnityObjectToClipPos(v.vertex);
	i.uv     = uv;
	VOXELPLAY_SET_VERTEX_LIGHT(i, worldCenter + corner, norm)
	o.Append(i);
}



#if VOXELPLAY_USE_TINTING
void PushVoxel(float3 center, int4 iuv, int4 occi, fixed3 color, inout TriangleStream<g2f> o) {
#else
void PushVoxel(float3 center, int4 iuv, int4 occi, inout TriangleStream<g2f> o) {
#endif
	// cube vertices
	worldCenter = mul(unity_ObjectToWorld, float4(center, 1.0)).xyz;
	float3 viewDir     = _WorldSpaceCameraPos - worldCenter;
	float3 normal      = sign(viewDir);
	float3 viewSide    = saturate(normal);

	float  occFront    = occi.x & 1;
	float  occBack     = (occi.x>>1) & 1;
	float  occTop      = (occi.x>>2) & 1;
	float  occBottom   = (occi.x>>3) & 1;
	float  occLeft     = (occi.x>>4) & 1;
	float  occRight    = (occi.x>>5) & 1;

	float3 v0          = cubeVerts[0];
	float3 v1          = cubeVerts[1];
	float3 v2          = cubeVerts[2];
	float3 v3          = cubeVerts[3];
	float3 v4          = -v3;
	float3 v5          = -v2;
	float3 v7          = -v1;

	g2f i;
	VOXELPLAY_SET_TINTCOLOR(color, i);
	VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_GEO(viewDir, normal);
	// Light contribution w/o AO
	float gi = iuv.w / 15.0;
	float3 side;

	#if !defined(DOUBLE_SIDED_GLASS) 

		// Front/back face
		float occ   = lerp( occFront, occBack, viewSide.z );
		if (occ==0) {
			norm = float3(0,0,normal.z);
			VOXELPLAY_SET_FACE_LIGHT(i, worldCenter, norm)
			i.light = light.z;
			side = lerp(1.0.xxx, float3(-1,1,-1), viewSide.z);
			float sideUV = lerp(iuv.x & 0xFFF, iuv.x >> 12, viewSide.z); // low 12 bits contains back, next 12 bits forward texture indices
			PushCorner(i, o, center, v1 * side, float4(1, 0, sideUV, gi));
			PushCorner(i, o, center, v0 * side, float4(0, 0, sideUV, gi));
			PushCorner(i, o, center, v3 * side, float4(1, 1, sideUV, gi));
			PushCorner(i, o, center, v2 * side, float4(0, 1, sideUV, gi));
			o.RestartStrip();
		}

		// Left/right face
		occ  = lerp( occLeft, occRight, viewSide.x );
		if (occ==0) {
			norm = float3(normal.x,0,0);
			VOXELPLAY_SET_FACE_LIGHT(i, worldCenter, norm)
			i.light  = light.x;
			side = lerp(1.0.xxx, float3(-1,1,-1), viewSide.x);
			float sideUV = (viewSide.x ? iuv.y : iuv.z) >> 12; // bits 13 and up contains left/right texture indices
			PushCorner(i, o, center, v0 * side, float4(1, 0, sideUV, gi));
			PushCorner(i, o, center, v4 * side, float4(0, 0, sideUV, gi));
			PushCorner(i, o, center, v2 * side, float4(1, 1, sideUV, gi));
			PushCorner(i, o, center, v7 * side, float4(0, 1, sideUV, gi));
			o.RestartStrip();
		}

		// Top/Bottom face
		occ  = lerp( occBottom, occTop, viewSide.y );
		if (occ==0) {
			norm = float3(0,normal.y,0);
			VOXELPLAY_SET_FACE_LIGHT(i, worldCenter, norm)
			i.light  = light.y;
			side = lerp(1.0.xxx, float3(-1,-1,1), viewSide.y);
			float sideUV = (viewSide.y ? iuv.y : iuv.z) & 0xFFF; // low 12 bits contains bottom/top texture indices
			PushCorner(i, o, center, v0 * side, float4(1, 1, sideUV, gi));
			PushCorner(i, o, center, v1 * side, float4(1, 0, sideUV, gi));
			PushCorner(i, o, center, v4 * side, float4(0, 1, sideUV, gi));
			PushCorner(i, o, center, v5 * side, float4(0, 0, sideUV, gi));
			o.RestartStrip();
		}
		occ  = lerp( occBottom, occTop, viewSide.z );
		if (occ==0) {
			norm = float3(0,normal.y,0);
			VOXELPLAY_SET_FACE_LIGHT(i, worldCenter, norm)
			i.light  = light.y;
			side = lerp(1.0.xxx, float3(-1,-1,1), viewSide.z);
			float sideUV = (viewSide.z ? iuv.y : iuv.z) & 0xFFF; // low 12 bits contains bottom/top texture indices
			PushCorner(i, o, center, v0 * side, float4(1, 1, sideUV, gi));
			PushCorner(i, o, center, v1 * side, float4(1, 0, sideUV, gi));
			PushCorner(i, o, center, v4 * side, float4(0, 1, sideUV, gi));
			PushCorner(i, o, center, v5 * side, float4(0, 0, sideUV, gi));
			o.RestartStrip();
		}
#else
	for (int s=0;s<2;s++) {
		// Front/back face
		float occ   = lerp( occFront, occBack, s );
		if (occ==0) {
			norm = float3(0,0,normal.z);
			VOXELPLAY_SET_FACE_LIGHT(i, worldCenter, norm)
			i.light = light.z;
			side = lerp(1.0.xxx, float3(-1,1,-1), s);
			float sideUV = lerp(iuv.x & 0xFFF, iuv.x >> 12, s); // low 12 bits contains back, next 12 bits forward texture indices
			PushCorner(i, o, center, v1 * side, float4(1, 0, sideUV, gi));
			PushCorner(i, o, center, v0 * side, float4(0, 0, sideUV, gi));
			PushCorner(i, o, center, v3 * side, float4(1, 1, sideUV, gi));
			PushCorner(i, o, center, v2 * side, float4(0, 1, sideUV, gi));
			o.RestartStrip();
		}

		// Left/right face
		occ  = lerp( occLeft, occRight, s );
		if (occ==0) {
			norm = float3(normal.x,0,0);
			VOXELPLAY_SET_FACE_LIGHT(i, worldCenter, norm)
			i.light  = light.x;
			side = lerp(1.0.xxx, float3(-1,1,-1), s);
			float sideUV = (s ? iuv.y : iuv.z) >> 12; // bits 13 and up contains left/right texture indices
			PushCorner(i, o, center, v0 * side, float4(1, 0, sideUV, gi));
			PushCorner(i, o, center, v4 * side, float4(0, 0, sideUV, gi));
			PushCorner(i, o, center, v2 * side, float4(1, 1, sideUV, gi));
			PushCorner(i, o, center, v7 * side, float4(0, 1, sideUV, gi));
			o.RestartStrip();
		}

		// Top/Bottom face
		occ  = lerp( occBottom, occTop, s );
		if (occ==0) {
			norm = float3(0,normal.y,0);
			VOXELPLAY_SET_FACE_LIGHT(i, worldCenter, norm)
			i.light  = light.y;
			side = lerp(1.0.xxx, float3(-1,-1,1), s);
			float sideUV = (s ? iuv.y : iuv.z) & 0xFFF; // low 12 bits contains bottom/top texture indices
			PushCorner(i, o, center, v0 * side, float4(1, 1, sideUV, gi));
			PushCorner(i, o, center, v1 * side, float4(1, 0, sideUV, gi));
			PushCorner(i, o, center, v4 * side, float4(0, 1, sideUV, gi));
			PushCorner(i, o, center, v5 * side, float4(0, 0, sideUV, gi));
			o.RestartStrip();
		}
	}
#endif
}


[maxvertexcount(24)]
void geom(point appdata i[1], inout TriangleStream<g2f> o) {
#if VOXELPLAY_USE_TINTING
	PushVoxel(i[0].vertex.xyz, (int4)i[0].uv, (int4)i[0].uv2, i[0].color, o);
#else
	PushVoxel(i[0].vertex.xyz, (int4)i[0].uv, (int4)i[0].uv2, o);
#endif
}

fixed4 frag (g2f i) : SV_Target {

	// Diffuse
	fixed4 color   = VOXELPLAY_GET_TEXEL_GEO(i.uv.xyz);

	#if defined(SHINING)
	color.ba += (1.0 - color.a) * 0.1 * (frac(_Time.y * 0.2)>0.9) * (frac(_Time.y + (i.uv.x + i.uv.y) * 0.1) > 0.9);
	#endif

	VOXELPLAY_APPLY_TINTCOLOR(color, i);

	VOXELPLAY_APPLY_OUTLINE(color, i);

	VOXELPLAY_APPLY_LIGHTING_AND_GI(color, i);

	VOXELPLAY_APPLY_FOG(color, i);

	return color;
}

