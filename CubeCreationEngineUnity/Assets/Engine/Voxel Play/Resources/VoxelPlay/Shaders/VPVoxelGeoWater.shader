Shader "Voxel Play/Voxels/Geo Water"
{
	Properties
	{
		[HideInInspector] _MainTex ("Main Texture Array", Any) = "white" {}
	}
	SubShader {

		Tags { "Queue" = "Geometry+1" "RenderType" = "Opaque" }

		GrabPass { "_WaterBackgroundTexture" }

		Pass {
			Tags { "LightMode" = "ForwardBase" }
			ZWrite Off
			CGPROGRAM
			#pragma target 4.0
			#pragma vertex   vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma multi_compile_fwdbase nolightmap nodynlightmap novertexlight nodirlightmap
			#pragma multi_compile _ VOXELPLAY_GLOBAL_USE_FOG
			#pragma multi_compile _ VOXELPLAY_USE_NORMAL
			#pragma multi_compile _ VOXELPLAY_USE_AA VOXELPLAY_USE_PARALLAX
			#pragma multi_compile _ VOXELPLAY_PIXEL_LIGHTS
			#pragma fragmentoption ARB_precision_hint_fastest
			#define USE_SHADOWS
			#define USE_SPECULAR
			#include "VPVoxelGeoWater.cginc"
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
			#include "VPVoxelGeoWaterShadows.cginc"
			ENDCG
		}

	}
	Fallback Off
}