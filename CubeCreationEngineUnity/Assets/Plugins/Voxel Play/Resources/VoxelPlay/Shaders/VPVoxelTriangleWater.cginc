#include "VPCommon.cginc"

struct appdata {
	float4 vertex   : POSITION;
	float4 uv       : TEXCOORD0;
	float3 normal   : NORMAL;
};


struct v2f {
	float4 pos    : SV_POSITION;
	float4 uv     : TEXCOORD0;
	VOXELPLAY_LIGHT_DATA(1,2)
	VOXELPLAY_FOG_DATA(3)
	float4 foam   : TEXCOORD4;
	float2 flow   : TEXCOORD5;
	#if defined(USE_SHADOWS)
	float4 grabPos: TEXCOORD6;
	SHADOW_COORDS(7)
	#endif
	VOXELPLAY_BUMPMAP_DATA(8)
	VOXELPLAY_PARALLAX_DATA(9)
	VOXELPLAY_NORMAL_DATA
};

struct vertexInfo {
	float4 vertex;
};

sampler2D _WaterBackgroundTexture;

v2f vert (appdata v) {
	v2f o;
	float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

	// wave effect
	v.vertex.y += sin(worldPos.x * 3.1415927 * 1.5 + _Time.w) * 0.025 + 0.028;

	o.pos    = UnityObjectToClipPos(v.vertex);

	int w = (int)v.uv.w;
	o.foam.x = w & 1;	// back
	o.foam.y = (w >>1) & 1; // front
	o.foam.z = (w >>2) & 1; // left
	o.foam.w = (w >>3) & 1; // right
	o.foam *= 4.0; // intensity
	o.flow   = float2(((w>>8) & 3) - 1.0, ((w>>10) & 3) - 1.0);

	o.uv = v.uv;
	o.uv.w = (w & 240) / 240.0; // light intensity encoded in bits 5-8 (128+64+32+16)

	VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_NORMAL(worldPos, v.normal);
	VOXELPLAY_SET_LIGHT(o, worldPos, v.normal);

	#if defined(USE_SHADOWS)
	TRANSFER_SHADOW(o);
	o.grabPos = ComputeGrabScreenPos(o.pos);
	#endif

	float3 tang = float3( dot(float3(0,1,-1), v.normal), 0, dot(float3(1,0,0), v.normal) );
	VOXELPLAY_SET_TANGENT_SPACE(tang, v.normal)
	VOXELPLAY_OUTPUT_PARALLAX_DATA(v, uv, o)
	VOXELPLAY_OUTPUT_NORMAL_DATA(uv, o)

	return o;
}


fixed4 frag (v2f i) : SV_Target {

	// Foam
	const float waveStart = 0.92;
	fixed foam = saturate( (1.0 - i.uv.y) - waveStart) * i.foam.x;
	foam = max(foam, saturate( i.uv.y - waveStart) * i.foam.y);
	foam = max(foam, saturate( (1.0 - i.uv.x) - waveStart) * i.foam.z);
	foam = max(foam, saturate( i.uv.x - waveStart) * i.foam.w);

	i.uv.xy    = i.uv.xy - _Time.yy * i.flow + _Time.xx;

	VOXELPLAY_APPLY_PARALLAX(i);

	// Diffuse
	fixed4 color   = VOXELPLAY_GET_TEXEL_DD(i.uv.xyz);
	color.rgb += foam;

	VOXELPLAY_APPLY_NORMAL(i);

	VOXELPLAY_APPLY_LIGHTING_AND_GI(color, i);

	VOXELPLAY_APPLY_FOG(color, i);

	// Blend transparency
	#if defined(USE_SHADOWS)
	fixed4 bgColor = tex2Dproj(_WaterBackgroundTexture, i.grabPos);
	color = lerp(bgColor, color, color.a);
	#endif

	return color;
}

