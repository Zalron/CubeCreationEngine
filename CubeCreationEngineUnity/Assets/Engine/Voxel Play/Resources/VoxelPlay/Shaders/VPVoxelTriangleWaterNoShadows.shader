Shader "Voxel Play/Voxels/Triangle Water No Shadows"
{
	Properties
	{
		[HideInInspector] _MainTex ("Main Texture Array", Any) = "white" {}
//		_VPSkyTint ("Sky Tint", Color) = (0.52, 0.5, 1.0)
//		_VPExposure("Exposure", Range(0, 8)) = 1.3
//		_VPFogAmount("Fog Amount", Range(0,1)) = 0.5
//		_VPFogData("Fog Data", Vector) = (10000, 0.00001, 0)
//		_VPAmbientLight ("Ambient Light", Float) = 0
//		_VPDaylightShadowAtten ("Daylight Shadow Atten", Float) = 0.95
	}
	SubShader {

		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
		Pass {
			Tags { "LightMode" = "ForwardBase" }
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			CGPROGRAM
			#pragma target 3.5
			#pragma vertex   vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile _ VOXELPLAY_GLOBAL_USE_FOG
			#pragma multi_compile _ VOXELPLAY_USE_NORMAL
			#pragma multi_compile _ VOXELPLAY_USE_AA VOXELPLAY_USE_PARALLAX
			#pragma multi_compile _ VOXELPLAY_PIXEL_LIGHTS
			#define USE_SPECULAR
			#include "VPVoxelTriangleWater.cginc"
			ENDCG
		}
	}
	Fallback Off
}