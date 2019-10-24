Shader "Voxel Play/Voxels/Geo/Transp Double Sided"
{
	Properties
	{
		[HideInInspector] _MainTex ("Main Texture Array", Any) = "white" {}
		_VPSeeThroughScreenMask ("Screen Mask", 2D) = "white" {}
		_OutlineColor ("Outline Color", Color) = (1,1,1,0.5)
		_OutlineThreshold("Outline Threshold", Float) = 0.48
	}
	SubShader {

		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }

		Pass {
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