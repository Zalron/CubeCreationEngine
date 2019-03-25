#include "UnityCG.cginc"
#include "VPCommonVertexModifier.cginc"

struct appdata {
	UNITY_VERTEX_INPUT_INSTANCE_ID
	float4 vertex   : POSITION;
};

struct v2f {
	V2F_SHADOW_CASTER;
};


struct vertexInfo {
	float4 vertex;
};


v2f vert (appdata v) {
	UNITY_SETUP_INSTANCE_ID(v);
	VOXELPLAY_MODIFY_VERTEX_NO_WPOS(v.vertex)

	v2f o;
	TRANSFER_SHADOW_CASTER(o);
	return o;
}

fixed4 frag (v2f i) : SV_Target {
	SHADOW_CASTER_FRAGMENT(i)
}

