Shader "Voxel Play/Voxels/Geo Cutout Cross"
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

		Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" }
		Pass {
//			AlphaToMask On
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