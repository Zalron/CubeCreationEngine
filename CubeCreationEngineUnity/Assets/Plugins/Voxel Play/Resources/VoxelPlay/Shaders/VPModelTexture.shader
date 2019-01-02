Shader "Voxel Play/Models/Texture"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
//		[HideInInspector] _VPSkyTint ("Sky Tint", Color) = (0.52, 0.5, 1.0)
//		[HideInInspector] _VPExposure("Exposure", Range(0, 8)) = 1.3
//		[HideInInspector] _VPFogAmount("Fog Amount", Range(0,1)) = 0.5
//		[HideInInspector] _VPFogData("Fog Data", Vector) = (10000, 0.00001, 0)
//		[HideInInspector] _VPAmbientLight ("Ambient Light", Float) = 0
		_Color ("Tint Color", Color) = (1,1,1,1)
		_VoxelLight ("Voxel Light", Range(0,1)) = 1
	}
	SubShader {

		Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
		Pass {
			Tags { "LightMode" = "ForwardBase" }
			CGPROGRAM
			#pragma target 3.5
			#pragma vertex   vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_fwdbase nolightmap nodynlightmap novertexlight nodirlightmap
			#pragma multi_compile _ VOXELPLAY_GLOBAL_USE_FOG
			#pragma multi_compile_instancing
			#define SUBTLE_SELF_SHADOWS
			#define USE_TEXTURE
			#define NON_ARRAY_TEXTURE
			#include "VPModel.cginc"
			ENDCG
		}

		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			CGPROGRAM
			#pragma target 3.5
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#pragma multi_compile_instancing
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "VPModelShadows.cginc"
			ENDCG
		}

	}
	Fallback Off
}