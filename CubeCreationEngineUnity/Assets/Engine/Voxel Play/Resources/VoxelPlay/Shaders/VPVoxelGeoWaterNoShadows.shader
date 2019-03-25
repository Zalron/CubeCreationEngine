Shader "Voxel Play/Voxels/Geo Water No Shadows"
{
	Properties
	{
		[HideInInspector] _MainTex ("Main Texture Array", Any) = "white" {}
	}
	SubShader {

		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }

		Pass {
			Tags { "LightMode" = "ForwardBase" }
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			CGPROGRAM
			#pragma target 4.0
			#pragma vertex   vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile _ VOXELPLAY_GLOBAL_USE_FOG
			#pragma multi_compile _ VOXELPLAY_USE_NORMAL
			#pragma multi_compile _ VOXELPLAY_USE_AA VOXELPLAY_USE_PARALLAX
			#pragma multi_compile _ VOXELPLAY_PIXEL_LIGHTS
			#define USE_SPECULAR
			#include "VPVoxelGeoWater.cginc"
			ENDCG
		}
	}
	Fallback Off
}