﻿#include "VPCommon.cginc"

struct appdata {
	float4 vertex   : POSITION;
	float3 uv       : TEXCOORD0;
};


struct v2f {
	V2F_SHADOW_CASTER;
	float3 uv     : TEXCOORD1;
};

struct vertexInfo {
	float4 vertex;
};

v2f vert (appdata v) {
	v2f o;
	float3 wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
	VOXELPLAY_MODIFY_VERTEX(v.vertex, wpos)
 
    int iuvz = (int)v.uv.z;
    float disp = (iuvz>>16) * sin(wpos.x + _Time.w) * 0.01;
    v.vertex.x += disp * v.uv.y;

    o.pos    = UnityObjectToClipPos(v.vertex);
    float3 uv = v.uv;
    uv.z = iuvz & 65535; // remove wind animation flag
    o.uv     = uv;

	TRANSFER_SHADOW_CASTER(o);
	return o;
}

fixed4 frag (v2f i) : SV_Target {
	fixed4 color   = UNITY_SAMPLE_TEX2DARRAY(_MainTex, i.uv.xyz);
	clip(color.a - 0.25);
	SHADOW_CASTER_FRAGMENT(i)
}

