#include "VPCommon.cginc"

#define FOAM_SIZE 0.12

struct appdata {
	float4 vertex   : POSITION;
	float4 uv       : TEXCOORD0;
	float4 uv2		: TEXCOORD1;
};


struct g2f {
	float4 pos    : SV_POSITION;
	float4 uv     : TEXCOORD0;
	VOXELPLAY_LIGHT_DATA(1,2)
	VOXELPLAY_FOG_DATA(3)
	fixed4 foam   : TEXCOORD4;
	fixed4 foamCorners : TEXCOORD5;
	float2 flow   : TEXCOORD6;
	#if defined(USE_SHADOWS)
	float4 grabPos: TEXCOORD7;
	SHADOW_COORDS(8)
	#endif
	VOXELPLAY_BUMPMAP_DATA(9)
	VOXELPLAY_PARALLAX_DATA(10)
	VOXELPLAY_NORMAL_DATA
};

struct vertexInfo {
	float4 vertex;
};

sampler2D _WaterBackgroundTexture;


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
	#if defined(USE_SHADOWS)
	TRANSFER_SHADOW(i);
	i.grabPos = ComputeGrabScreenPos(i.pos);
	#endif
	o.Append(i);
}
/* cube coords
   
  7+------+6
  /.   3 /|
2+------+ |
 |4.....|.+5
 |/     |/
0+------+1

UV/Bit/Face
x.0 front
x.1 back
x.2 top
x.3 bottom
x.4 left
x.5 right
*/


void PushVoxel(float3 center, float4 uv, int4 occi, inout TriangleStream<g2f> o) {
	// cube vertices
	worldCenter = mul(unity_ObjectToWorld, float4(center, 1.0)).xyz;
	float3 viewDir     = _WorldSpaceCameraPos - worldCenter;
	float3 normal      = sign(viewDir);
	float3 viewSide    = saturate(normal);

	// Face visibility
	float  occFront    = occi.x & 1;
	float  occBack     = (occi.x>>1) & 1;
	float  occTop      = (occi.x>>2) & 1;
	float  occBottom   = (occi.x>>3) & 1;
	float  occLeft     = (occi.x>>4) & 1;
	float  occRight    = (occi.x>>5) & 1;
	float  vertSizeFR  = (occi.w & 15) / 15.0;
	float  vertSizeBR  = ((occi.w >> 4) & 15) / 15.0;
	float  vertSizeBL  = ((occi.w >> 8) & 15) / 15.0;
	float  vertSizeFL  = ((occi.w >> 12) & 15) / 15.0;

	// wave effect
	float tr, tl;
	sincos(worldCenter.x * 3.1415927 * 1.5 + _Time.w, tr, tl);
	tr = tr * 0.025 + 0.028;
	tl = tl * 0.025 + 0.028;

	// Vertices
	float3 v0          = float3(-0.5, 0 * tl, -0.5);
	float3 v1          = float3( 0.5, 0 * tr, -0.5);
	float3 v2          = float3(-0.5, vertSizeBL + tl, -0.5);
	float3 v3          = float3( 0.5, vertSizeBR + tr, -0.5);
	float3 v4          = float3(-0.5, 0 * tl, 0.5);
	float3 v5          = float3( 0.5, 0 * tr, 0.5);
	float3 v6          = float3( 0.5, vertSizeFR + tr, 0.5);
	float3 v7          = float3(-0.5, vertSizeFL + tl, 0.5);

	g2f i;
	VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_GEO(viewDir, normal);
	i.foam        = 0.0.xxxx;
	i.foamCorners = 0.0.xxxx;
	i.flow        = float2(0, -1);

	// Front/back face
	float occ   = lerp( occFront, occBack, viewSide.z );
	if (occ==0) {
		norm  = float3(0,0,normal.z);
		VOXELPLAY_SET_TANGENT_SPACE(float3(-norm.z,0,0), norm)
//		VOXELPLAY_SET_LIGHT(i, worldCenter, norm)
		VOXELPLAY_SET_FACE_LIGHT(i, worldCenter, norm)
		i.light = light.z;
		PushCorner(i, o, center, lerp(v3, v7, viewSide.z), float4(1, 1, uv.x, uv.w));
		PushCorner(i, o, center, lerp(v1, v4, viewSide.z), float4(1, 0, uv.x, uv.w));
		PushCorner(i, o, center, lerp(v2, v6, viewSide.z), float4(0, 1, uv.x, uv.w));
		PushCorner(i, o, center, lerp(v0, v5, viewSide.z), float4(0, 0, uv.x, uv.w));
		o.RestartStrip();
	}

	// Left/right face
	occ  = lerp( occLeft, occRight, viewSide.x );
	if (occ==0) {
		norm  = float3(normal.x,0,0);
		VOXELPLAY_SET_TANGENT_SPACE(float3(0,0,norm.x), norm);
//		VOXELPLAY_SET_LIGHT(i, worldCenter, norm)
		VOXELPLAY_SET_FACE_LIGHT(i, worldCenter, norm)
		i.light  = light.x;
		PushCorner(i, o, center, lerp(v4, v1, viewSide.x), float4(0, 0, uv.x, uv.w));
		PushCorner(i, o, center, lerp(v7, v3, viewSide.x), float4(0, 1, uv.x, uv.w));
		PushCorner(i, o, center, lerp(v0, v5, viewSide.x), float4(1, 0, uv.x, uv.w));
		PushCorner(i, o, center, lerp(v2, v6, viewSide.x), float4(1, 1, uv.x, uv.w));
		o.RestartStrip();
	}

	// Top/Bottom face
	occ  = lerp( occBottom, occTop, viewSide.y );
	if (occ==0) {
		i.flow      = float2(((occi.y>>8) & 3) - 1.0, ((occi.y>>10) & 3) - 1.0);
		norm = float3(0,normal.y,0);
		VOXELPLAY_SET_TANGENT_SPACE(float3(norm.y,0,0), norm);
//		VOXELPLAY_SET_LIGHT(i, worldCenter, norm)
		VOXELPLAY_SET_FACE_LIGHT(i, worldCenter, norm)
		i.light  = light.y;
		i.foam.x = occi.y & 1;	// back
		i.foam.y = (occi.y>>1) & 1; // front
		i.foam.z = (occi.y>>2) & 1; // left
		i.foam.w = (occi.y>>3) & 1; // right
		i.foam *= FOAM_SIZE;

		i.foamCorners.x = (occi.y>>4) & 1; // BL
		i.foamCorners.y = (occi.y>>5) & 1; // FL
		i.foamCorners.z = (occi.y>>6) & 1; // FR
		i.foamCorners.w = (occi.y>>7) & 1; // BR

		float sideUV = lerp(uv.z, uv.y, viewSide.y);

		PushCorner(i, o, center, lerp(v4, v6, viewSide.y), float4(0  , 0, sideUV, uv.w));
		PushCorner(i, o, center, lerp(v0, v3, viewSide.y), float4(0  , 1.0, sideUV, uv.w));
		PushCorner(i, o, center, lerp(v5, v7, viewSide.y), float4(1.0, 0, sideUV, uv.w));
		PushCorner(i, o, center, lerp(v1, v2, viewSide.y), float4(1.0, 1.0, sideUV, uv.w));
		o.RestartStrip();
	}

	// Top face when under water surface
	if (occTop == 0) {
		i.flow      = float2(((occi.y>>8) & 3) - 1.0, ((occi.y>>10) & 3) - 1.0);
		norm = float3(0,1,0);
		VOXELPLAY_SET_TANGENT_SPACE(float3(norm.y,0,0), norm);
//		VOXELPLAY_SET_LIGHT(i, worldCenter, norm)
		VOXELPLAY_SET_FACE_LIGHT(i, worldCenter, norm)
		i.light  = 1.0;

		PushCorner(i, o, center, v6, float4(0  , 0, uv.z, uv.w));
		PushCorner(i, o, center, v7, float4(1.0, 0, uv.z, uv.w));
		PushCorner(i, o, center, v3, float4(0  , 1.0, uv.z, uv.w));
		PushCorner(i, o, center, v2, float4(1.0, 1.0, uv.z, uv.w));
		o.RestartStrip();
	}
}


[maxvertexcount(12)]
void geom(point appdata i[1], inout TriangleStream<g2f> o) {
	PushVoxel(i[0].vertex.xyz, i[0].uv, (int4)i[0].uv2, o);
}


fixed4 frag (g2f i) : SV_Target {

	// Foam at sides (x = right, y = left, z = front, w = back)
	i.uv.xy = saturate(i.uv.xy);
	fixed4 foamSides = fixed4(i.foam.w - i.uv.x, i.foam.z - (1.0 - i.uv.x), i.foam.y - i.uv.y, i.foam.x - (1.0 - i.uv.y));

	// Foam at corners
	fixed foamLeft = FOAM_SIZE - (1.0 - i.uv.x);
	fixed foamRight = FOAM_SIZE - i.uv.x;
	fixed foamBack = FOAM_SIZE - (1.0 - i.uv.y);
	fixed foamForward = FOAM_SIZE - i.uv.y;
	fixed4 foamCorners1 = fixed4(foamLeft, foamLeft, foamRight, foamRight);
	fixed4 foamCorners2 = fixed4(foamBack, foamForward, foamForward, foamBack);
	fixed4 foamCorners  = min(foamCorners1, foamCorners2) * i.foamCorners;

	// combine sides and corner foam
	fixed4 foam2 = max(foamSides, foamCorners);

	// final foam intensity
	fixed foam = max( max(foam2.x, foam2.y), max(foam2.z, foam2.w) );
	foam *= 4.0;

	// Animate
	i.uv.xy    = frac(i.uv.xy - _Time.yy * i.flow.xy + _Time.xx);

	// Diffuse
	VOXELPLAY_APPLY_PARALLAX(i);
	fixed4 color   = VOXELPLAY_GET_TEXEL_GEO(i.uv.xyz);
	color.rgb += saturate(foam);

	VOXELPLAY_APPLY_NORMAL(i);

	VOXELPLAY_APPLY_LIGHTING_AND_GI(color, i);

	VOXELPLAY_APPLY_FOG(color, i);

	// Blend transparency
	#if defined(USE_SHADOWS)
	fixed4 bgColor = tex2Dproj(_WaterBackgroundTexture, i.grabPos);
	color = lerp(bgColor, color, color.a);
	#endif

	return color;
}

