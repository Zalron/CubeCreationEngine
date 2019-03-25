Shader "Voxel Play/Voxels/Geo Transp Double Sided"
{
	Properties
	{
		[HideInInspector] _MainTex ("Main Texture Array", Any) = "white" {}
//		_VPSkyTint ("Sky Tint", Color) = (0.52, 0.5, 1.0)
//		_VPExposure("Exposure", Range(0, 8)) = 1.3
//		_VPFogAmount("Fog Amount", Range(0,1)) = 0.5
//		_VPFogData("Fog Data", Vector) = (10000, 0.00001, 0)
//		_VPAmbientLight ("Ambient Light", Float) = 0
		_OutlineColor ("Outline Color", Color) = (1,1,1,0.5)
		_OutlineThreshold("Outline Threshold", Float) = 0.48
	}
	SubShader {

		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }

		Pass {
			Tags { "LightMode" = "ForwardBase" }
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Off
			CGPROGRAM
			#pragma target 4.0
			#pragma vertex   vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile _ VOXELPLAY_USE_AO
			#pragma multi_compile _ VOXELPLAY_USE_TINTING
			#pragma multi_compile _ VOXELPLAY_USE_OUTLINE
			#pragma multi_compile _ VOXELPLAY_GLOBAL_USE_FOG
			#pragma multi_compile _ VOXELPLAY_USE_AA
			#pragma multi_compile _ VOXELPLAY_PIXEL_LIGHTS
			#define DOUBLE_SIDED_GLASS
			#pragma multi_compile _ VOXELPLAY_TRANSP_BLING
			#include "VPVoxelGeoTransp.cginc"
			ENDCG
		}

	}
	Fallback Off
}