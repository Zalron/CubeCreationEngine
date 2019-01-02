#ifndef VOXELPLAY_COMMON
#define VOXELPLAY_COMMON

#include "UnityCG.cginc"
#include "AutoLight.cginc"
#include "Lighting.cginc"


/* cube coords
   
  7+------+6
  /.   3 /|
2+------+ |
 |4.....|.+5
 |/     |/
0+------+1

*/

const static float3 cubeVerts[8] = { 
	-0.5,	-0.5,	-0.5,
	 0.5,	-0.5,	-0.5,
	-0.5,	 0.5,	-0.5,
	 0.5,	 0.5,	-0.5,
	-0.5,	-0.5,	 0.5,
	 0.5,	-0.5,	 0.5,
	 0.5,	 0.5,	 0.5,
	-0.5,	 0.5,	 0.5
};


fixed _VPAmbientLight;

#ifndef NON_ARRAY_TEXTURE
UNITY_DECLARE_TEX2DARRAY(_MainTex); 
#endif
float4 _MainTex_TexelSize;
fixed4 _OutlineColor;
fixed _OutlineThreshold;


#if VOXELPLAY_USE_TINTING
	#define VOXELPLAY_TINTCOLOR_DATA fixed3 color : COLOR;
	#define VOXELPLAY_SET_TINTCOLOR(color, i) i.color = color;
	#define VOXELPLAY_OUTPUT_TINTCOLOR(o) o.color = v.color;
	#define VOXELPLAY_APPLY_TINTCOLOR(color, i) color.rgb *= i.color;
#else
	#define VOXELPLAY_TINTCOLOR_DATA
	#define VOXELPLAY_SET_TINTCOLOR(color, i)
	#define VOXELPLAY_OUTPUT_TINTCOLOR(o)
	#define VOXELPLAY_APPLY_TINTCOLOR(color, i)
#endif


#if VOXELPLAY_USE_AA
	#if defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_METAL)
		#define UNITY_SAMPLE_TEX2DARRAY_GRAD(tex,coord,dx,dy) tex.SampleGrad (sampler##tex,coord,dx,dy)
	#elif defined(UNITY_COMPILER_HLSL2GLSL) || defined(SHADER_TARGET_SURFACE_ANALYSIS)
		#define UNITY_SAMPLE_TEX2DARRAY_GRAD(tex,coord,dx,dy) tex2DArray(tex,coord,dx,dy)
	#else
		#define UNITY_SAMPLE_TEX2DARRAY_GRAD(tex,coord,dx,dy) UNITY_SAMPLE_TEX2DARRAY(tex,coord)
	#endif

	inline fixed4 ReadSmoothTexel(float3 uv) {
		float2 ruv = uv.xy * _MainTex_TexelSize.zw - 0.5;
		float2 f = fwidth(ruv);
		uv.xy = (floor(ruv) + 0.5 + saturate( (frac(ruv) - 0.5 + f ) / f)) / _MainTex_TexelSize.zw;	
		return UNITY_SAMPLE_TEX2DARRAY(_MainTex, uv);
	}

	inline fixed4 ReadSmoothTexelWithDerivatives(float3 uv) {
		float2 ruv = frac(uv.xy) * _MainTex_TexelSize.zw - 0.5;
		float2 f = fwidth(ruv);
		float2 nuv = (floor(ruv) + 0.5 + saturate( (frac(ruv) - 0.5 + f ) / f)) / _MainTex_TexelSize.zw;	
		return UNITY_SAMPLE_TEX2DARRAY_GRAD(_MainTex, float3(nuv, uv.z), ddx(uv.xy), ddy(uv.xy));
	}

	#define VOXELPLAY_GET_TEXEL_GEO(uv) ReadSmoothTexel(uv);
	#define VOXELPLAY_GET_TEXEL(uv) ReadSmoothTexel(uv);
	#define VOXELPLAY_GET_TEXEL_DD(uv) ReadSmoothTexelWithDerivatives(uv);

	inline void ApplyOutline(inout fixed4 color, float2 uv) {
		float2 grd = abs(frac(uv + 0.5) - 0.5);
		grd /= fwidth(uv);
		float  lin = 1.0 - saturate(min(grd.x, grd.y) * _OutlineThreshold);
		color.rgb = lerp(color.rgb, _OutlineColor.rgb, lin * _OutlineColor.a);
	}

#else // no AA pixels

	#define VOXELPLAY_GET_TEXEL_GEO(uv) UNITY_SAMPLE_TEX2DARRAY(_MainTex, uv.xyz);
	#define VOXELPLAY_GET_TEXEL(uv) UNITY_SAMPLE_TEX2DARRAY(_MainTex, uv.xyz);
	#define VOXELPLAY_GET_TEXEL_DD(uv) UNITY_SAMPLE_TEX2DARRAY(_MainTex, uv.xyz);

	inline void ApplyOutline(inout fixed4 color, float2 uv) {
		float2 grd = abs(uv - 0.5);
		float  lin = max(grd.x, grd.y) > _OutlineThreshold;
		color.rgb = lerp(color.rgb, _OutlineColor.rgb, lin * _OutlineColor.a);
	}

#endif

#if !VOXELPLAY_USE_OUTLINE || defined(VP_CUTOUT)
	#define VOXELPLAY_APPLY_OUTLINE(color, i)
#else
	#define VOXELPLAY_APPLY_OUTLINE(color, i) ApplyOutline(color, i.uv);
#endif

#define VOXELPLAY_NEEDS_TANGENT_SPACE VOXELPLAY_USE_PARALLAX || VOXELPLAY_USE_NORMAL
#if VOXELPLAY_NEEDS_TANGENT_SPACE
	float3x3 objectToTangent;
	#define VOXELPLAY_SET_TANGENT_SPACE(tang,norm) objectToTangent = float3x3( tang, cross(tang, norm), norm );
#else
	#define VOXELPLAY_SET_TANGENT_SPACE(tang,norm)
#endif

inline float3 GetLight() {
	return saturate(1.0 + _WorldSpaceLightPos0.y * 2.0).xxx;
}


#if VOXELPLAY_USE_NORMAL

	inline float GetPerVoxelNdotL(float3 normal) {
		return saturate(1.0 + _WorldSpaceLightPos0.y * 2.0);
	}

	inline float3 GetLight(float3 normal) {
		return saturate(1.0 + _WorldSpaceLightPos0.y * 2.0).xxx;
	}

	#define VOXELPLAY_BUMPMAP_DATA(idx1) float3 tlightDir : TEXCOORD##idx1;
	#define VOXELPLAY_OUTPUT_NORMAL_DATA(uv, i) i.tlightDir = mul(objectToTangent, _WorldSpaceLightPos0.xyz);

	fixed GetPerPixelNdotL(float3 tlightDir, float3 uv) {
		float3 nrm  = UNITY_SAMPLE_TEX2DARRAY_LOD(_MainTex, uv, 0).xyz;
		nrm = (nrm * 2.0) - 1.0;
		nrm.y*=-1.0;
		// diffuse wrap
		return saturate(dot(nrm, tlightDir) * 0.5 + 0.5);
	}

	#define VOXELPLAY_APPLY_NORMAL(i) i.light *= GetPerPixelNdotL(i.tlightDir, float3(i.uv.xy, i.uv.z+1));

#else
	inline float GetPerVoxelNdotL(float3 normal) {
		float NdotL = saturate(dot(_WorldSpaceLightPos0.xyz, normal) * 0.5 + 0.5);
		return NdotL * saturate(1.0 + _WorldSpaceLightPos0.y * 2.0);
	}

	inline float3 GetLight(float3 normal) {
		float3 NdotL3 = saturate(_WorldSpaceLightPos0.xyz * normal * 0.5 + 0.5);
		return NdotL3 * saturate(1.0 + _WorldSpaceLightPos0.y * 2.0);
	}

	#define VOXELPLAY_BUMPMAP_DATA(idx1)
	#define VOXELPLAY_OUTPUT_NORMAL_DATA(uv, i)
	#define VOXELPLAY_APPLY_NORMAL(i)
#endif


CBUFFER_START(VoxelPlayLightBuffers)
float4 _VPPointLightPosition[32];
float4 _VPPointLightColor[32];
CBUFFER_END

float3 ShadePointLights(float3 worldPos, float3 normal) {
	float3 color = 0;
	for (int k=0;k<32;k++) {
		float3 toLight = _VPPointLightPosition[k].xyz - worldPos;
		float dist = dot(toLight, toLight);
		toLight /= dist + 0.0001;
		float atten = dist / _VPPointLightPosition[k].w;
		float NdL = saturate((dot(normal, toLight) - 1.0) * _VPPointLightColor[k].a + 1.0);
		color += _VPPointLightColor[k].rgb * (NdL / (1.0 + atten));
	}
	return color;
}


float3 ShadePointLightsWithoutNormal(float3 worldPos) {
	fixed3 color = 0;
	for (int k=0;k<32;k++) {
		float3 toLight = _VPPointLightPosition[k].xyz - worldPos;
		float dist = dot(toLight, toLight);
		toLight /= dist + 0.0001;
		float atten = dist / _VPPointLightColor[k].w;
		color += _VPPointLightColor[k].rgb / (1.0 + atten);
	}
	return color;
}


#if VOXELPLAY_GLOBAL_USE_FOG
half3 _VPSkyTint;
float _VPFogAmount;
half _VPExposure;
float3 _VPFogData;


fixed3 getSkyColor(float3 ray) {
	float3 delta  = _WorldSpaceLightPos0.xyz - ray;
	float dist    = dot(delta, delta);
	float y = abs(ray.y);

	// sky base color
	half3 skyColor = _VPSkyTint;

	// fog
	half fog = saturate(_VPFogAmount - y) / (1.0001 - _VPFogAmount);
	skyColor = lerp(skyColor, 1.0.xxx, fog);

	// sky tint
	float hy = abs(_WorldSpaceLightPos0.y) + y;
	half t = saturate( (0.4 - hy) * 2.2) / (1.0 + dist * 0.8);
	skyColor.r = lerp(skyColor.r, 1.0, t);
	skyColor.b = lerp(skyColor.b, 0.0, t);

	// daylight + obscure opposite side of sky
	fixed dayLightDir = 1.0 + _WorldSpaceLightPos0.y * 2.0;
	half daylight = saturate(dayLightDir - dist * 0.03);
	skyColor *= daylight;

	// exposure
	skyColor *= _VPExposure * _LightColor0.rgb;

	// gamma
	#if defined(UNITY_COLORSPACE_GAMMA)
	skyColor = sqrt(skyColor);
	#endif

	return skyColor;
}


#define VOXELPLAY_FOG_DATA(idx1) fixed4 skyColor: TEXCOORD##idx1;
#define VOXELPLAY_APPLY_FOG(color, i) color.rgb = lerp(color.rgb, i.skyColor.rgb, i.skyColor.a);

#if defined(SUN_SCATTERING)
#define COMPUTE_SUN_SCATTERING(nviewDir) float scattering = max(0, dot(nviewDir, _WorldSpaceLightPos0.xyz)); light += scattering * 0.75;
#else
#define COMPUTE_SUN_SCATTERING(nviewDir)
#endif

#define VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_GEO(viewDir, normal) fixed3 light = GetLight(normal); float3 nviewDir = normalize(-viewDir); COMPUTE_SUN_SCATTERING(nviewDir); i.skyColor = fixed4(getSkyColor(nviewDir), saturate( (dot(viewDir, viewDir) - _VPFogData.x) / _VPFogData.y ));
#define VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_SIMPLE(viewDir) fixed3 light = GetLight(); i.skyColor = fixed4(getSkyColor(normalize(-viewDir)), saturate( (dot(viewDir, viewDir) - _VPFogData.x) / _VPFogData.y ));
#define VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_NORMAL(worldPos, normal) float3 viewDir = worldPos - _WorldSpaceCameraPos; float3 nviewDir = normalize(viewDir); COMPUTE_SUN_SCATTERING(nviewDir); o.skyColor = fixed4(getSkyColor(nviewDir), saturate( (dot(viewDir, viewDir) - _VPFogData.x) / _VPFogData.y)); o.light = GetPerVoxelNdotL(normal);
#define VOXELPLAY_INITIALIZE_LIGHT_AND_FOG(worldPos) float3 viewDir = worldPos - _WorldSpaceCameraPos; float3 nviewDir = normalize(viewDir); COMPUTE_SUN_SCATTERING(nviewDir); float3 normal = -nviewDir; o.skyColor = fixed4(getSkyColor(nviewDir), saturate((dot(viewDir, viewDir) - _VPFogData.x) / _VPFogData.y)); o.light = GetPerVoxelNdotL(normal);

#else // fallbacks when fog is disabled

#define VOXELPLAY_FOG_DATA(idx1)
#define VOXELPLAY_APPLY_FOG(color, i)
#define VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_GEO(viewDir, normal) fixed3 light = GetLight(normal);
#define VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_SIMPLE(viewDir) fixed3 light = GetLight();
#define VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_NORMAL(worldPos, normal) o.light = GetPerVoxelNdotL(normal);
#define VOXELPLAY_INITIALIZE_LIGHT_AND_FOG(worldPos) float3 viewDir = _WorldSpaceCameraPos - worldPos; float3 normal = normalize(viewDir); o.light = GetPerVoxelNdotL(normal);

#endif // VOXELPLAY_GLOBAL_USE_FOG

#if VOXELPLAY_PIXEL_LIGHTS
	#define VOXELPLAY_LIGHT_DATA(idx1,idx2) fixed light: TEXCOORD##idx1; float3 wpos: TEXCOORD##idx2;
	#define VOXELPLAY_NORMAL_DATA float3 norm: NORMAL;
	#if VOXELPLAY_USE_AO
		#define VOXELPLAY_SET_VERTEX_LIGHT(i, worldPos, normal) i.wpos = worldPos; i.norm = normal;
		#define VOXELPLAY_SET_VERTEX_LIGHT_WITHOUT_NORMAL(i, worldPos) i.wpos = worldPos;
		#define VOXELPLAY_SET_FACE_LIGHT(i, worldPos, normal)
	#else
		#define VOXELPLAY_SET_VERTEX_LIGHT(i, worldPos, normal)
		#define VOXELPLAY_SET_VERTEX_LIGHT_WITHOUT_NORMAL(i, worldPos)
		#define VOXELPLAY_SET_FACE_LIGHT(i, worldPos, normal) i.wpos = worldPos; i.norm = normal;
	#endif
	#define VOXELPLAY_SET_LIGHT(i, worldPos, normal) i.wpos = worldPos; i.norm = normal;
	#define VOXELPLAY_SET_LIGHT_WITHOUT_NORMAL(i, worldPos) i.wpos = worldPos;
	#define VOXELPLAY_VERTEX_LIGHT_COLOR ShadePointLights(i.wpos, i.norm)
#else
	#define VOXELPLAY_LIGHT_DATA(idx1,idx2) fixed light: TEXCOORD##idx1; fixed3 vertexLightColor: TEXCOORD##idx2;
	#define VOXELPLAY_NORMAL_DATA
	#if VOXELPLAY_USE_AO
		#define VOXELPLAY_SET_VERTEX_LIGHT(i, worldPos, normal) i.vertexLightColor = ShadePointLights(worldPos, normal);
		#define VOXELPLAY_SET_VERTEX_LIGHT_WITHOUT_NORMAL(i, worldPos) i.vertexLightColor = ShadePointLights(worldPos, normal);
		#define VOXELPLAY_SET_FACE_LIGHT(i, worldPos, normal)
	#else
		#define VOXELPLAY_SET_VERTEX_LIGHT(i, worldPos, normal)
		#define VOXELPLAY_SET_VERTEX_LIGHT_WITHOUT_NORMAL(i, worldPos)
		#define VOXELPLAY_SET_FACE_LIGHT(i, worldPos, normal) i.vertexLightColor = ShadePointLightsWithoutNormal(worldPos);
	#endif
	#define VOXELPLAY_SET_LIGHT(i, worldPos, normal) i.vertexLightColor = ShadePointLights(worldPos, normal);
	#define VOXELPLAY_SET_LIGHT_WITHOUT_NORMAL(i, worldPos) i.vertexLightColor = ShadePointLightsWithoutNormal(worldPos);
	#define VOXELPLAY_VERTEX_LIGHT_COLOR i.vertexLightColor
#endif

#if defined(NO_SELF_SHADOWS)
#define VOXELPLAY_LIGHT_ATTENUATION(i) saturate(1.0 + _WorldSpaceLightPos0.y * _VPDaylightShadowAtten) * i.light + _VPAmbientLight
#else
#define VOXELPLAY_SHADOW_ATTENUATION(i) min(1, SHADOW_ATTENUATION(i) + max(0, LinearEyeDepth( i.pos.z ) * _LightShadowData.z + _LightShadowData.w ) )
#define VOXELPLAY_LIGHT_ATTENUATION(i) saturate( VOXELPLAY_SHADOW_ATTENUATION(i) + _WorldSpaceLightPos0.y * _VPDaylightShadowAtten) * i.light + _VPAmbientLight
#endif

#if defined(SUBTLE_SELF_SHADOWS)
#define _VPDaylightShadowAtten 0.65
#else
fixed _VPDaylightShadowAtten;
#endif

#define VOXELPLAY_APPLY_LIGHTING(color,i) fixed atten = VOXELPLAY_LIGHT_ATTENUATION(i); color.rgb *= atten * _LightColor0.rgb + VOXELPLAY_VERTEX_LIGHT_COLOR;
#define VOXELPLAY_APPLY_LIGHTING_AO_AND_GI(color,i) fixed atten = VOXELPLAY_LIGHT_ATTENUATION(i); float ao = i.uv.w; ao = 1.0-(1.0-ao)*(1.0-ao); color.rgb *= (atten * ao) * _LightColor0 + VOXELPLAY_VERTEX_LIGHT_COLOR;
#define VOXELPLAY_APPLY_LIGHTING_AND_GI(color,i) fixed atten = VOXELPLAY_LIGHT_ATTENUATION(i); color.rgb *= (atten * i.uv.w) * _LightColor0.rgb + VOXELPLAY_VERTEX_LIGHT_COLOR;

#if defined(USE_EMISSION)
fixed _VPEmissionIntensity;
#define VOXELPLAY_COMPUTE_EMISSION(color) fixed3 emissionColor = color.rgb * ( _VPEmissionIntensity * (1.0 - color.a) );
#define VOXELPLAY_ADD_EMISSION(color) color.rgb += emissionColor;
#else
#define VOXELPLAY_COMPUTE_EMISSION(color)
#define VOXELPLAY_ADD_EMISSION(color)
#endif // EMISSION


#define VOXELPLAY_OUTPUT_UV(uv, i) i.uv = uv;



#if VOXELPLAY_USE_PARALLAX

	float _VPParallaxStrength;
	int _VPParallaxIterations, _VPParallaxIterationsBinarySearch;

	float GetParallaxHeight (float3 uv, float2 uvOffset) {
		return UNITY_SAMPLE_TEX2DARRAY_LOD(_MainTex, float3(uv.xy + uvOffset, uv.z), 0).a;
	}

	void ApplyParallax(float3 tviewDir, inout float3 uv) {

		tviewDir = normalize(tviewDir);
		float2 uvDir = tviewDir.xy / (tviewDir.z + 0.42);
		float stepSize = 1.0 / _VPParallaxIterations;
		float2 uvInc = uvDir * (stepSize * _VPParallaxStrength);

		float2 uvOffset = 0;

		float stepHeight = 1;

		// get the texture index for displacement map
		uv.z ++;
		float surfaceHeight = UNITY_SAMPLE_TEX2DARRAY_LOD(_MainTex, uv, 0).a;

		float2 prevUVOffset = uvOffset;
		float prevStepHeight = stepHeight;
		float prevSurfaceHeight = surfaceHeight;

		for (int i1 = 1; i1 < _VPParallaxIterations && stepHeight > surfaceHeight; i1++) {
			prevUVOffset = uvOffset;
			prevStepHeight = stepHeight;
			prevSurfaceHeight = surfaceHeight;
			uvOffset -= uvInc;
			stepHeight -= stepSize;
			surfaceHeight = GetParallaxHeight(uv, uvOffset);
		}

		for (int i2 = 0; i2 < _VPParallaxIterationsBinarySearch; i2++) {
			uvInc *= 0.5;
			stepSize *= 0.5;

			if (stepHeight < surfaceHeight) {
				uvOffset += uvInc;
				stepHeight += stepSize;
			} else {
				uvOffset -= uvInc;
				stepHeight -= stepSize;
			}
			surfaceHeight = GetParallaxHeight(uv, uvOffset);
		}

		uv.xy += uvOffset;
		uv.z --;
	}

	#define VOXELPLAY_PARALLAX_DATA(idx1) float3 tviewDir : TEXCOORD##idx1; 
	#define VOXELPLAY_OUTPUT_PARALLAX_DATA(v, uv, i) float3 invViewDir = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1)).xyz - v.vertex.xyz; i.tviewDir = mul(objectToTangent, invViewDir);
	#define VOXELPLAY_APPLY_PARALLAX(i) ApplyParallax(i.tviewDir, i.uv.xyz);
#else
	#define VOXELPLAY_PARALLAX_DATA(idx1) 
	#define VOXELPLAY_OUTPUT_PARALLAX_DATA(worldPos, uv, i) 
	#define VOXELPLAY_APPLY_PARALLAX(i)
#endif // VOXELPLAY_USE_PARALLAX

#endif // VOXELPLAY_COMMON

