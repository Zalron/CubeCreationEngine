#include "VPCommon.cginc"
#include "VPCommonRealisticWater.cginc"

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
	float3 viewDir : TEXCOORD4;
	float4 bumpuv : TEXCOORD5;
	float2 flow   : TEXCOORD6;
	float4 grabPos: TEXCOORD7;
	half4 foam   : TEXCOORD8;
	half4 foamCorners : TEXCOORD9;
	#if defined(USE_SHADOWS)
		SHADOW_COORDS(10)
	#endif
	VOXELPLAY_NORMAL_DATA
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
	float3 wpos = worldCenter + corner;
	VOXELPLAY_MODIFY_VERTEX(v.vertex, wpos)
	i.pos    = UnityObjectToClipPos(v.vertex);
	VOXELPLAY_SET_VERTEX_LIGHT(i, worldCenter + corner, norm)
	VOXELPLAY_OUTPUT_NORMAL_DATA(uv, i)
	VOXELPLAY_OUTPUT_UV(uv, i)
	i.grabPos = ComputeGrabScreenPos(i.pos);

	#if defined(USE_SHADOWS)
		TRANSFER_SHADOW(i);
	#endif

	// scroll waves normal
	float4 wavesOffset = _Time.xxxx * (i.flow.xyxy + float4(1,1,-0.4,-0.45) * _WaveSpeed);
	float4 temp = wpos.xzxz * _WaveScale * float4(2.0,2.0,3.0,3.0) + wavesOffset;
	i.bumpuv = temp.xywz;

	i.viewDir = WorldSpaceViewDir(v.vertex);

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
    tr *= _WaveAmplitude;
    tl *= _WaveAmplitude;

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


half4 frag (g2f i) : SV_Target {

	half3 worldViewDir = normalize(i.viewDir);

	// combine two scrolling bumpmaps into one
	half3 bump1 = GetWaterNormal(i.bumpuv.xy); 
	half3 bump2 = GetWaterNormal(i.bumpuv.zw);
	//half3 normal = (bump1 + bump2).xzy * 0.5;
    half3 normal = BlendNormals(bump1, bump2).xzy;

	// Fresnel factor
	float fresnelFac = dot( worldViewDir, normal * _NormalStrength );

    // Distored grabpos
    #if defined(USE_SHADOWS)
    float4 distortedUV = i.grabPos;
    i.grabPos.xy += normal.xz * _RefractionDistortion;
    #endif

    // Water color
/*  half4 color;
    fresnelFac = saturate(fresnelFac + _Fresnel);
    half4 water = tex2D( _ReflectiveColor, float2(fresnelFac,fresnelFac) );
    color.rgb = lerp(water, _WaterColor.rgb, water.a);
    color.a   = _WaterColor.a; */

    half4 color;
    fresnelFac = saturate(fresnelFac + _Fresnel);
    half4 water = tex2D( _ReflectiveColor, float2(fresnelFac,fresnelFac) );

    // Ocean foam
    half4 ofoam = tex2D(_FoamTex, normal.xz * 0.01);
    half3 oceanWater = _WaterColor.rgb + saturate(ofoam.rgb - _OceanWave.x) * _OceanWave.y;

    color.rgb = lerp(water.rgb, oceanWater, water.a);
    color.a   = _WaterColor.a;

	// Underwater fog
	half screenDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, i.grabPos));
    half depthInWater = saturate( (screenDepth - i.grabPos.z) * _UnderWaterFogColor.a );
    color.rgb = _UnderWaterFogColor.rgb * (depthInWater * (1.0 - color.a)) + color.rgb;
    color.a = saturate(depthInWater + color.a);

	// Specular
	float3 h = normalize (_WorldSpaceLightPos0.xyz + worldViewDir);
	h *= (sign(_WorldSpaceLightPos0.y) + 1.0) * 0.5; // avoid specular under the horizon
    float nh = max (0, dot (normal, h));
    float spec = pow (nh, _SpecularPower);
    #if defined(USE_SHADOWS)
    spec *= SHADOW_ATTENUATION(i);
    #endif
    color.rgb += (_SpecularIntensity * spec) * _LightColor0.rgb;

	// Foam at sides (x = right, y = left, z = front, w = back)
	i.uv.xy = saturate(i.uv.xy);
	half4 foamSides = half4(i.foam.w - i.uv.x, i.foam.z - (1.0 - i.uv.x), i.foam.y - i.uv.y, i.foam.x - (1.0 - i.uv.y));

	// Foam at corners
	half foamLeft = FOAM_SIZE - (1.0 - i.uv.x);
	half foamRight = FOAM_SIZE - i.uv.x;
	half foamBack = FOAM_SIZE - (1.0 - i.uv.y);
	half foamForward = FOAM_SIZE - i.uv.y;
	half4 foamCorners1 = half4(foamLeft, foamLeft, foamRight, foamRight);
	half4 foamCorners2 = half4(foamBack, foamForward, foamForward, foamBack);
	half4 foamCorners  = min(foamCorners1, foamCorners2) * i.foamCorners;

	// combine sides and corner foam
	half4 foam2 = max(foamSides, foamCorners);

	// final foam intensity
	half foamIntensity = max( max(foam2.x, foam2.y), max(foam2.z, foam2.w) );
	foamIntensity *= 2.0;

    half3 foamGradient = 1 - tex2D(_FoamGradient, float2(foamIntensity - _Time.y*0.2, 0) + normal.xz * 0.2);
    float2 foamDistortUV = normal.xz;
    half3 foamColor = tex2D(_FoamTex, i.bumpuv.xy * 7.0 + foamDistortUV).rgb * _FoamColor;
    color.rgb += foamGradient * foamIntensity * foamColor;

    // Depth-based foam
    half depthFoam = 1.0 - foamIntensity;
    half depthDiff = screenDepth - i.grabPos.z;
    half foamAmount = depthFoam * (depthDiff>0) * saturate( (1.0 - depthDiff) * 2.0  );
    color.rgb += foamAmount * foamColor;

	VOXELPLAY_APPLY_LIGHTING_AND_GI(color, i);

	VOXELPLAY_APPLY_FOG(color, i);

    // Blend transparency
    #if defined(USE_SHADOWS)
    half4 bgColor = tex2Dproj(_WaterBackgroundTexture, i.grabPos);
    color = lerp(bgColor, color, color.a);
    #endif

	return color;
}
