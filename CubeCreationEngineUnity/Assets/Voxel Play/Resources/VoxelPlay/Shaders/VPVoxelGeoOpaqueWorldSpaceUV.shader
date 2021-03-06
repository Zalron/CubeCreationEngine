﻿Shader "Voxel Play/Voxels/Override Examples/Geo Opaque World Space UV"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_OutlineColor ("Outline Color", Color) = (1,1,1,0.5)
		_OutlineThreshold("Outline Threshold", Float) = 0.48
	}
	SubShader {

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
			#pragma multi_compile _ VOXELPLAY_USE_OUTLINE
			#pragma multi_compile _ VOXELPLAY_GLOBAL_USE_FOG
			#pragma multi_compile _ VOXELPLAY_USE_AA
			#pragma multi_compile _ VOXELPLAY_PIXEL_LIGHTS
            #define NON_ARRAY_TEXTURE
            #define USE_WORLD_SPACE_UV
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