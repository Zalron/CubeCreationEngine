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
//	UNITY_VERTEX_INPUT_INSTANCE_ID
	float4 pos    : SV_POSITION;
	VOXELPLAY_LIGHT_DATA(0,1)
	SHADOW_COORDS(2)
	#if defined(USE_TEXTURE)
	float2 uv     : TEXCOORD3;
	#elif defined(USE_TRIPLANAR) 
	float3 worldPos    : TEXCOORD3;
	half3  worldNormal : TEXCOORD5;
	#endif
	fixed4 color  : COLOR;
	VOXELPLAY_FOG_DATA(4)
};


fixed4 _Color;
fixed _VoxelLight;
sampler _BumpMap;
fixed _DiffuseWrap;

UNITY_INSTANCING_BUFFER_START(Props)
	UNITY_DEFINE_INSTANCED_PROP(fixed4, _TintColor)
#define _TintColor_arr Props
UNITY_INSTANCING_BUFFER_END(Props)


v2f vert (appdata v) {
	v2f o;
	UNITY_SETUP_INSTANCE_ID(v);
//	UNITY_TRANSFER_INSTANCE_ID(v, o);
	float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
	VOXELPLAY_MODIFY_VERTEX(v.vertex, worldPos)
	o.pos    = UnityObjectToClipPos(v.vertex);
	fixed4 color = v.color * _Color;
	color.rgb *= _VoxelLight;

    fixed4 tintColor = UNITY_ACCESS_INSTANCED_PROP(_TintColor_arr, _TintColor);
    color.rgb *= tintColor.rgb * tintColor.a; // UNITY_ACCESS_INSTANCED_PROP(_VoxelLight); // voxel light now is encoded into tintColor.a

	o.color = color;

	float3 worldNormal = UnityObjectToWorldNormal(v.normal);

	#if defined(USE_TEXTURE)
	o.uv     = v.uv;
	#elif defined(USE_TRIPLANAR)
	o.worldPos    = worldPos;
	o.worldNormal = worldNormal;
	#endif

	VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_NORMAL(worldPos, worldNormal);
	VOXELPLAY_SET_LIGHT(o, worldPos, worldNormal);

	TRANSFER_SHADOW(o);
	return o;
}

fixed4 frag (v2f i) : SV_Target {
//	UNITY_SETUP_INSTANCE_ID(i);

	// Diffuse
	#if defined(USE_TEXTURE)
		fixed4 color = VOXELPLAY_GET_TEXEL_2D(i.uv) * i.color;
	#elif defined(USE_TRIPLANAR) // from bgolus' https://github.com/bgolus/Normal-Mapping-for-a-Triplanar-Shader/blob/master/TriplanarSwizzle.shader
		// triplanar blend
        half3 triblend = saturate(pow(i.worldNormal, 4));
        triblend /= max(dot(triblend, half3(1,1,1)), 0.0001);

        // triplanar uvs
        float2 uvX = i.worldPos.zy * _MainTex_ST.xy + _MainTex_ST.zw;
        float2 uvY = i.worldPos.xz * _MainTex_ST.xy + _MainTex_ST.zw;
        float2 uvZ = i.worldPos.xy * _MainTex_ST.xy + _MainTex_ST.zw;

        // albedo textures
        fixed4 colX = VOXELPLAY_GET_TEXEL_2D(uvX);
        fixed4 colY = VOXELPLAY_GET_TEXEL_2D(uvY);
        fixed4 colZ = VOXELPLAY_GET_TEXEL_2D(uvZ);
        fixed4 color = colX * triblend.x + colY * triblend.y + colZ * triblend.z;

        half3 axisSign = i.worldNormal < 0 ? -1 : 1;

        // tangent space normal maps
        half3 tnormalX = UnpackNormal(tex2D(_BumpMap, uvX));
        half3 tnormalY = UnpackNormal(tex2D(_BumpMap, uvY));
        half3 tnormalZ = UnpackNormal(tex2D(_BumpMap, uvZ));

        // flip normal maps' z axis to account for world surface normal facing
        tnormalX.z *= axisSign.x;
        tnormalY.z *= axisSign.y;
        tnormalZ.z *= axisSign.z;

        // swizzle tangent normals to match world orientation and blend together
        half3 worldNormal = normalize(tnormalX.zyx * triblend.x + tnormalY.xzy * triblend.y + tnormalZ.xyz * triblend.z);
        half ndotl = saturate(dot(worldNormal, _WorldSpaceLightPos0.xyz));
        ndotl = lerp(ndotl, ndotl * 0.5 + 0.5, _DiffuseWrap);
        color *= ndotl + _VPAmbientLight;
        color *= i.color;
	#else
		fixed4 color = i.color;
	#endif

    #if defined(VP_CUTOUT)
    clip(color.a - 0.5);
    #endif
	VOXELPLAY_APPLY_LIGHTING(color, i);
	VOXELPLAY_APPLY_FOG(color, i);
	return color;
}

