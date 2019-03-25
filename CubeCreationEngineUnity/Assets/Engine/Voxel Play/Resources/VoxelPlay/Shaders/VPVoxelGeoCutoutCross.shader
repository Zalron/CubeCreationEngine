Shader "Voxel Play/Voxels/Geo Cutout Cross"
{
	Properties
	{
		[HideInInspector] _MainTex ("Main Texture Array", Any) = "white" {}
	}
	SubShader {

		Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" }
		Pass {
			AlphaToMask On
			Tags { "LightMode" = "ForwardBase" }
			Cull Off
			CGPROGRAM
			#pragma target 4.0
			#pragma vertex   vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_fwdbase nolightmap nodynlightmap novertexlight nodirlightmap
			#pragma multi_compile _ VOXELPLAY_GLOBAL_USE_FOG
			#pragma multi_compile _ VOXELPLAY_USE_AA
			#include "VPVoxelGeoCutoutCross.cginc"
			ENDCG
		}

		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			Cull Off  // neccessary to avoid shadows overwrite the grass; remove to improve performance a bit
			CGPROGRAM
			#pragma target 4.0
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "VPVoxelGeoCutoutCrossShadows.cginc"
			ENDCG
		}

	}
	Fallback Off
}