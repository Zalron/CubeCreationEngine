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
	SHADOW_COORDS(4)
	VOXELPLAY_TINTCOLOR_DATA
	VOXELPLAY_BUMPMAP_DATA(5)
	VOXELPLAY_PARALLAX_DATA(6)
	VOXELPLAY_NORMAL_DATA
	VOXELPLAY_OUTLINE_DATA(7)
};

struct vertexInfo {
	float4 vertex;
};


void vert (inout appdata v) {
}


float3 worldCenter, norm;


inline void PushCorner(inout g2f i, inout TriangleStream<g2f>o, float3 center, float3 corner, float4 uv) {
	vertexInfo v;
	v.vertex = float4(center + corner, 1.0);
	VOXELPLAY_MODIFY_VERTEX(v.vertex, worldCenter + corner)
	i.pos    = UnityObjectToClipPos(v.vertex);
	VOXELPLAY_SET_VERTEX_LIGHT(i, worldCenter + corner, norm)
	VOXELPLAY_OUTPUT_PARALLAX_DATA(v, uv, i)
	VOXELPLAY_OUTPUT_NORMAL_DATA(uv, i)
	VOXELPLAY_OUTPUT_UV(uv, i)
	TRANSFER_SHADOW(i);
	o.Append(i);
}



#if VOXELPLAY_USE_TINTING
void PushVoxel(float3 center, int4 iuv, int4 occi, fixed3 color, inout TriangleStream<g2f> o) {
#else
void PushVoxel(float3 center, int4 iuv, int4 occi, inout TriangleStream<g2f> o) {
#endif
	// cube vertices
	worldCenter        = mul(unity_ObjectToWorld, float4(center, 1.0)).xyz;
	float3 viewDir     = _WorldSpaceCameraPos - worldCenter;
	float3 normal      = sign(viewDir);
	float3 viewSide    = saturate(normal);

	float3 v0          = cubeVerts[0];
	float3 v1          = cubeVerts[1];
	float3 v2          = cubeVerts[2];
	float3 v3          = cubeVerts[3];

	#if defined(VP_CUTOUT)
		float disp = (iuv.w >> 16) * sin(worldCenter.x + worldCenter.y + _Time.w) * 0.005;
		v0.xy += disp;
		v1.xy += disp;
		v2.xy += disp;
		v3.xy += disp;
		float colorVariation = ((iuv.w>>6) & 511) / 255.0; // uv.w / 16320.0; // (uv.w >> 6) / 255.0;
		float shrink = 1.0 - disp * 4.0;
	#endif

	float3 v4          = -v3;
	float3 v5          = -v2;
	float3 v7          = -v1;

	g2f i;
	VOXELPLAY_SET_TINTCOLOR(color, i)
	VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_GEO(viewDir, normal)
	VOXELPLAY_INITIALIZE_OUTLINE(i)
	float3 side;

	#if !VOXELPLAY_USE_AO
		// Light contribution w/o AO
		float occ;
		float lightBack   = occi.x & 15;
		float lightForward= (occi.x>>4) & 15;
		float lightTop    = (occi.x>>8) & 15;
		float lightLeft   = (occi.x>>12) & 15;
		float lightRight  = (occi.x>>16) & 15;
		float lightBottom = (occi.x>>20) & 15;

		// Top/Bottom face
		float occludes  = lerp(iuv.w & 8, iuv.w & 4, viewSide.y);
		if (occludes == 0) {
			VOXELPLAY_SET_OUTLINE( lerp (float4(iuv.w & 32, iuv.w & 2, iuv.w & 16, iuv.w & 1), float4(iuv.w & 16, iuv.w & 2, iuv.w & 32, iuv.w & 1), viewSide.y) )
			norm = float3(0,normal.y,0);
			VOXELPLAY_SET_TANGENT_SPACE(float3(norm.y,0,0), norm);
			VOXELPLAY_SET_FACE_LIGHT(i, worldCenter, norm)
			i.light  = light.y;
			side = lerp(1.0.xxx, float3(-1,-1,1), viewSide.y);
			#if defined(VP_CUTOUT)
				side.y *= shrink;
			#endif
			float sideUV = (viewSide.y ? iuv.y : iuv.z) & 0xFFF; // low 12 bits contains bottom/top texture indices plus norm/disp flags (bits 10/11)
			occ  = lerp(lightBottom, lightTop, viewSide.y);
			occ /= 15.0;
			#if defined(VP_CUTOUT)
				occ *= colorVariation;
			#endif
			PushCorner(i, o, center, v0 * side, float4(1, 1, sideUV, occ));
			PushCorner(i, o, center, v1 * side, float4(1, 0, sideUV, occ));
			PushCorner(i, o, center, v4 * side, float4(0, 1, sideUV, occ));
			PushCorner(i, o, center, v5 * side, float4(0, 0, sideUV, occ));
			o.RestartStrip();
		 }

		// Front/back face
		occludes  = lerp(iuv.w & 1, iuv.w & 2, viewSide.z);
		if (occludes == 0) {
			VOXELPLAY_SET_OUTLINE( lerp(float4(iuv.w & 16, iuv.w & 4, iuv.w & 32, iuv.w & 8), float4(iuv.w & 32, iuv.w & 4, iuv.w & 16, iuv.w & 8), viewSide.z ) )
			norm = float3(0,0,normal.z);
			VOXELPLAY_SET_TANGENT_SPACE(float3(-norm.z,0,0), norm)
			VOXELPLAY_SET_FACE_LIGHT(i, worldCenter, norm)
			i.light = light.z;
			side = lerp(1.0.xxx, float3(-1,1,-1), viewSide.z);
			#if defined(VP_CUTOUT)
				side.z *= shrink;
				float sideUV = iuv.x;
			#else
				float sideUV = lerp(iuv.x & 0xFFF, iuv.x >> 12, viewSide.z); // low 12 bits contains back, next 12 bits forward texture indices
			#endif
			occ  = lerp(lightBack, lightForward, viewSide.z);
	       	occ /= 15.0;
			#if defined(VP_CUTOUT)
			occ *= colorVariation;
			#endif
			PushCorner(i, o, center, v1 * side, float4(1, 0, sideUV, occ));
			PushCorner(i, o, center, v0 * side, float4(0, 0, sideUV, occ));
			PushCorner(i, o, center, v3 * side, float4(1, 1, sideUV, occ));
			PushCorner(i, o, center, v2 * side, float4(0, 1, sideUV, occ));
			o.RestartStrip();
		}

		// Left/right face
		occludes  = lerp(iuv.w & 16, iuv.w & 32, viewSide.x);
		if (occludes == 0) {
			VOXELPLAY_SET_OUTLINE( lerp(float4(iuv.w & 2, iuv.w & 4, iuv.w & 1, iuv.w & 8), float4(iuv.w & 1, iuv.w & 4, iuv.w & 2, iuv.w & 8), viewSide.x ) )
			norm = float3(normal.x,0,0);
			VOXELPLAY_SET_TANGENT_SPACE(float3(0,0,norm.x), norm);
			VOXELPLAY_SET_FACE_LIGHT(i, worldCenter, norm)
			i.light  = light.x;
			side = lerp(1.0.xxx, float3(-1,1,-1), viewSide.x);
			#if defined(VP_CUTOUT)
				side.x *= shrink;
				float sideUV = iuv.x;
			#else
				float sideUV = (viewSide.x ? iuv.y : iuv.z) >> 12; // bits 13 and up contains left/right texture indices
			#endif
			occ  = lerp( lightLeft, lightRight, viewSide.x);
			occ /= 15.0;
			#if defined(VP_CUTOUT)
			occ *= colorVariation;
			#endif
			PushCorner(i, o, center, v0 * side, float4(1, 0, sideUV, occ));
			PushCorner(i, o, center, v4 * side, float4(0, 0, sideUV, occ));
			PushCorner(i, o, center, v2 * side, float4(1, 1, sideUV, occ));
			PushCorner(i, o, center, v7 * side, float4(0, 1, sideUV, occ));
			o.RestartStrip();
		}
	#else
		// Light contribution with AO
		float4 occ;
		float  occ0        = occi.x & 15;
		float  occ1        = (occi.x>>4) & 15;
		float  occ2        = (occi.x>>8) & 15;
		float  occ3        = (occi.x>>12) & 15;
		float  occ5        = (occi.x>>16) & 15;
		float  occ4        = (occi.x>>20) & 15;

		float  occ6        = occi.y & 15;
		float  occ7        = (occi.y>>4) & 15;
		float  occ_t2      = (occi.y>>8) & 15;
		float  occ_t3      = (occi.y>>12) & 15;
		float  occ_t7      = (occi.y>>16) & 15;
		float  occ_t6      = (occi.y>>20) & 15;

		float occ2_0       = occi.z & 15;
		float occ2_4       = (occi.z>>4) & 15;
		float occ2_2       = (occi.z>>8) & 15;
		float occ2_7       = (occi.z>>12) & 15;
		float occ2_1       = (occi.z>>16) & 15;
		float occ2_5       = (occi.z>>20) & 15;

		float occ2_3       = occi.w & 15;
		float occ2_6       = (occi.w>>4) & 15;
		float occ2_b0      = (occi.w>>8) & 15;
		float occ2_b1      = (occi.w>>12) & 15;
		float occ2_b4      = (occi.w>>16) & 15;
		float occ2_b5      = (occi.w>>20) & 15;

		// Top/Bottom face
		float occludes  = lerp(iuv.w & 8, iuv.w & 4, viewSide.y);
		if (occludes == 0) {
			VOXELPLAY_SET_OUTLINE( lerp (float4(iuv.w & 32, iuv.w & 2, iuv.w & 16, iuv.w & 1), float4(iuv.w & 16, iuv.w & 2, iuv.w & 32, iuv.w & 1), viewSide.y) )
			norm = float3(0,normal.y,0);
			VOXELPLAY_SET_TANGENT_SPACE(float3(norm.y,0,0), norm);
			VOXELPLAY_SET_FACE_LIGHT(i, worldCenter, norm)
			i.light  = light.y;
			side = lerp(1.0.xxx, float3(-1,-1,1), viewSide.y);
			#if defined(VP_CUTOUT)
				side.y *= shrink;
			#endif
			float sideUV = (viewSide.y ? iuv.y : iuv.z) & 0xFFF; // low 12 bits contains bottom/top texture indices plus normal/disp flags (bits 10 and 11)
			occ  = lerp( float4(occ2_b0, occ2_b1, occ2_b4, occ2_b5), float4(occ_t3, occ_t2, occ_t6, occ_t7), viewSide.y );
			occ /= 15.0;
			#if defined(VP_CUTOUT)
			occ *= colorVariation;
			#endif

			if (occ.x + occ.w < occ.y + occ.z) {
				PushCorner(i, o, center, v0 * side, float4(1, 0, sideUV, occ.x));
				PushCorner(i, o, center, v1 * side, float4(0, 0, sideUV, occ.y));
				PushCorner(i, o, center, v4 * side, float4(1, 1, sideUV, occ.z));
				PushCorner(i, o, center, v5 * side, float4(0, 1, sideUV, occ.w));
			} else {
				PushCorner(i, o, center, v4 * side, float4(1, 1, sideUV, occ.z));
				PushCorner(i, o, center, v0 * side, float4(1, 0, sideUV, occ.x));
				PushCorner(i, o, center, v5 * side, float4(0, 1, sideUV, occ.w));
				PushCorner(i, o, center, v1 * side, float4(0, 0, sideUV, occ.y));
			}
			o.RestartStrip();
	 	}

		// Front/back face
		occludes  = lerp(iuv.w & 1, iuv.w & 2, viewSide.z);
		if (occludes == 0) {
			VOXELPLAY_SET_OUTLINE( lerp(float4(iuv.w & 16, iuv.w & 4, iuv.w & 32, iuv.w & 8), float4(iuv.w & 32, iuv.w & 4, iuv.w & 16, iuv.w & 8), viewSide.z ) )
			norm  = float3(0,0,normal.z);
			VOXELPLAY_SET_TANGENT_SPACE(float3(-norm.z,0,0), norm)
			VOXELPLAY_SET_FACE_LIGHT(i, worldCenter, norm)
			i.light = light.z;
			side = lerp(1.0.xxx, float3(-1,1,-1), viewSide.z);
			#if defined(VP_CUTOUT)
				side.z *= shrink;
				float sideUV = iuv.x;
			#else
		       	float sideUV = lerp(iuv.x & 0xFFF, iuv.x >> 12, viewSide.z); // low 12 bits contains back, next 12 bits forward texture indices
			#endif
			occ  = lerp( float4(occ1, occ0, occ3, occ2), float4(occ4, occ5, occ7, occ6), viewSide.z );
	       	occ /= 15.0;
			#if defined(VP_CUTOUT)
			occ *= colorVariation;
			#endif
			if (occ.y + occ.z < occ.x + occ.w) {
				PushCorner(i, o, center, v1 * side, float4(1, 0, sideUV, occ.x));
				PushCorner(i, o, center, v0 * side, float4(0, 0, sideUV, occ.y));
				PushCorner(i, o, center, v3 * side, float4(1, 1, sideUV, occ.z));
				PushCorner(i, o, center, v2 * side, float4(0, 1, sideUV, occ.w));
			} else {
				PushCorner(i, o, center, v3 * side, float4(1, 1, sideUV, occ.z));
				PushCorner(i, o, center, v1 * side, float4(1, 0, sideUV, occ.x));
				PushCorner(i, o, center, v2 * side, float4(0, 1, sideUV, occ.w));
				PushCorner(i, o, center, v0 * side, float4(0, 0, sideUV, occ.y));
			}
			o.RestartStrip();
		}

		// Left/right face
		occludes  = lerp(iuv.w & 16, iuv.w & 32, viewSide.x);
		if (occludes == 0) {
			VOXELPLAY_SET_OUTLINE( lerp(float4(iuv.w & 2, iuv.w & 4, iuv.w & 1, iuv.w & 8), float4(iuv.w & 1, iuv.w & 4, iuv.w & 2, iuv.w & 8), viewSide.x ) )
			norm  = float3(normal.x,0,0);
			VOXELPLAY_SET_TANGENT_SPACE(float3(0,0,norm.x), norm);
			VOXELPLAY_SET_FACE_LIGHT(i, worldCenter, norm)
			i.light  = light.x;
			side = lerp(1.0.xxx, float3(-1,1,-1), viewSide.x);
			#if defined(VP_CUTOUT)
				side.x *= shrink;
				float sideUV = iuv.x;
			#else
				float sideUV = (viewSide.x ? iuv.y : iuv.z) >> 12; // bits 13 and up contains left/right texture indices
			#endif
			occ  = lerp( float4(occ2_0, occ2_4, occ2_2, occ2_7), float4(occ2_5, occ2_1, occ2_6, occ2_3), viewSide.x );
			occ /= 15.0;
			#if defined(VP_CUTOUT)
			occ *= colorVariation;
			#endif
			if (occ.y + occ.z < occ.x + occ.w) {
				PushCorner(i, o, center, v0 * side, float4(1, 0, sideUV, occ.x));
				PushCorner(i, o, center, v4 * side, float4(0, 0, sideUV, occ.y));
				PushCorner(i, o, center, v2 * side, float4(1, 1, sideUV, occ.z));
				PushCorner(i, o, center, v7 * side, float4(0, 1, sideUV, occ.w));
			} else {
				PushCorner(i, o, center, v4 * side, float4(0, 0, sideUV, occ.y));
				PushCorner(i, o, center, v7 * side, float4(0, 1, sideUV, occ.w));
				PushCorner(i, o, center, v0 * side, float4(1, 0, sideUV, occ.x));
				PushCorner(i, o, center, v2 * side, float4(1, 1, sideUV, occ.z));
			}
			o.RestartStrip();
		}
	#endif

}


[maxvertexcount(12)]
void geom(point appdata i[1], inout TriangleStream<g2f> o) {
#if VOXELPLAY_USE_TINTING
	PushVoxel(i[0].vertex.xyz, (int4)i[0].uv, (int4)i[0].uv2, i[0].color,  o);
#else
	PushVoxel(i[0].vertex.xyz, (int4)i[0].uv, (int4)i[0].uv2, o);
#endif
}

fixed4 frag (g2f i) : SV_Target {

	VOXELPLAY_APPLY_PARALLAX(i);

	fixed4 color = VOXELPLAY_GET_TEXEL_GEO(i.uv.xyz);

	#if defined(VP_CUTOUT)
	clip(color.a - 0.5);
	#endif

	VOXELPLAY_COMPUTE_EMISSION(color)

	VOXELPLAY_APPLY_NORMAL(i);

	VOXELPLAY_APPLY_TINTCOLOR(color, i);

	VOXELPLAY_APPLY_OUTLINE(color, i);

	VOXELPLAY_APPLY_LIGHTING_AO_AND_GI(color, i);

	VOXELPLAY_ADD_EMISSION(color)

	VOXELPLAY_APPLY_FOG(color, i);

	return color;
}
