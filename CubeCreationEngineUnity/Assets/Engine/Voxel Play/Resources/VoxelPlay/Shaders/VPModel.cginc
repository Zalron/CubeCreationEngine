// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

#include "VPCommon.cginc"

struct appdata {
	UNITY_VERTEX_INPUT_INSTANCE_ID
	float4 vertex   : POSITION;
	float3 normal   : NORMAL;
	fixed4 color    : COLOR;
	#if defined(USE_TEXTURE)
	float2 uv       : TEXCOORD0;
	#endif
};


struct v2f {
	UNITY_VERTEX_INPUT_INSTANCE_ID
	float4 pos    : SV_POSITION;
	VOXELPLAY_LIGHT_DATA(0,1)
	SHADOW_COORDS(2)
	#if defined(USE_TEXTURE)
	float2 uv     : TEXCOORD3;
	#endif
	fixed4 color  : COLOR;
	VOXELPLAY_FOG_DATA(4)
	VOXELPLAY_NORMAL_DATA
};


//fixed4 _Color;
//fixed _VoxelLight;
sampler _MainTex;

UNITY_INSTANCING_BUFFER_START(Props)
	UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
#define _Color_arr Props
	UNITY_DEFINE_INSTANCED_PROP(fixed, _VoxelLight)
#define _VoxelLight_arr Props
UNITY_INSTANCING_BUFFER_END(Props)

inline fixed4 ReadSmoothTexel2D(float2 uv) {
	float2 ruv = uv.xy * _MainTex_TexelSize.zw - 0.5;
	float2 f = fwidth(ruv);
	uv.xy = (floor(ruv) + 0.5 + saturate( (frac(ruv) - 0.5 + f ) / f)) / _MainTex_TexelSize.zw;	
	return tex2D(_MainTex, uv);
}

#define VOXELPLAY_GET_TEXEL_2D(uv) ReadSmoothTexel2D(uv)



v2f vert (appdata v) {
	v2f o;
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_TRANSFER_INSTANCE_ID(v, o);
	o.pos    = UnityObjectToClipPos(v.vertex);
	o.color  = v.color;
	#if defined(USE_TEXTURE)
	o.uv     = v.uv;
	#endif

	float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
	float3 wsNormal = UnityObjectToWorldNormal(v.normal);
	VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_NORMAL(worldPos, wsNormal);
	VOXELPLAY_SET_LIGHT(o, worldPos, wsNormal);

	TRANSFER_SHADOW(o);
	return o;
}

fixed4 frag (v2f i) : SV_Target {
	UNITY_SETUP_INSTANCE_ID(i);

	// Diffuse
	#if defined(USE_TEXTURE)
	fixed4 color = VOXELPLAY_GET_TEXEL_2D(i.uv) * i.color * UNITY_ACCESS_INSTANCED_PROP(_Color_arr, _Color);
	#else
	fixed4 color = i.color * UNITY_ACCESS_INSTANCED_PROP(_Color_arr, _Color);
	#endif
	color.rgb *= UNITY_ACCESS_INSTANCED_PROP(_VoxelLight_arr, _VoxelLight);

	VOXELPLAY_APPLY_LIGHTING(color, i);
	VOXELPLAY_APPLY_FOG(color, i);

	return color;
}

