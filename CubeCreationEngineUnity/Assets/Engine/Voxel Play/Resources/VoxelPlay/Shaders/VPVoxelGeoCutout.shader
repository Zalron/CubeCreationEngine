﻿Shader "Voxel Play/Voxels/Geo Cutout"
{
	Properties
	{
		[HideInInspector] _MainTex ("Main Texture Array", Any) = "white" {}
//		_VPSkyTint ("Sky Tint", Color) = (0.52, 0.5, 1.0)
//		_VPExposure("Exposure", Range(0, 8)) = 1.3
//		_VPFogAmount("Fog Amount", Range(0,1)) = 0.5
//		_VPFogData("Fog Data", Vector) = (10000, 0.00001, 0)
//		_VPAmbientLight ("Ambient Light", Float) = 0
		[HideInInspector] _VPParallaxStrength("__Elev", Float) = 0.2
		[HideInInspector] _VPParallaxIterations("__Iterations", Float) = 10
		[HideInInspector] _VPParallaxIterationsBinarySearch("__IterationsBinarySearch", Float) = 6
	}
	SubShader {

		Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" }
		Pass {
			Tags { "LightMode" = "ForwardBase" }
			CGPROGRAM
			#pragma target 4.0
			#pragma vertex   vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_fwdbase nolightmap nodynlightmap novertexlight nodirlightmap
			#pragma multi_compile _ VOXELPLAY_USE_AO
			#pragma multi_compile _ VOXELPLAY_GLOBAL_USE_FOG
			#pragma multi_compile _ VOXELPLAY_USE_NORMAL
			#pragma multi_compile _ VOXELPLAY_USE_AA VOXELPLAY_USE_PARALLAX
			#pragma multi_compile _ VOXELPLAY_PIXEL_LIGHTS
			#define VP_CUTOUT
			#include "VPVoxelGeoOpaque.cginc"
			ENDCG
		}

		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			CGPROGRAM
			#pragma target 4.0
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "VPVoxelGeoShadows.cginc"
			ENDCG
		}

	}
	Fallback Off
}