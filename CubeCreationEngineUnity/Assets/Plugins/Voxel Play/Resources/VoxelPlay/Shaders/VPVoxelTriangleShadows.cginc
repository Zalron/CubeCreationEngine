#include "UnityCG.cginc"

struct appdata {
	float4 vertex   : POSITION;
};

struct v2f {
	V2F_SHADOW_CASTER;
};

struct vertexInfo {
	float4 vertex;
};


v2f vert (appdata v) {
	v2f o;
	TRANSFER_SHADOW_CASTER(o);
	return o;
}

fixed4 frag (v2f i) : SV_Target {
	SHADOW_CASTER_FRAGMENT(i)
}

