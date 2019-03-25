Shader "Voxel Play/Voxels/Geo Opaque No AO"
{
	Properties
	{
		[HideInInspector] _MainTex ("Main Texture Array", Any) = "white" {}
	}
	SubShader {

		Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
		Pass {
			Tags { "LightMode" = "ForwardBase" }
			ZTest Less
			CGPROGRAM
			#pragma target 4.0
			#pragma vertex   vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_fwdbase nolightmap nodynlightmap novertexlight nodirlightmap
			#pragma multi_compile _ VOXELPLAY_USE_TINTING
			#pragma multi_compile _ VOXELPLAY_GLOBAL_USE_FOG
			#pragma multi_compile _ VOXELPLAY_USE_AA
			#pragma multi_compile _ VOXELPLAY_PIXEL_LIGHTS
			#define NO_SELF_SHADOWS
			#define SUN_SCATTERING
			#include "VPVoxelGeoOpaqueNoAO.cginc"
			ENDCG
		}

		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			ZTest Less
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