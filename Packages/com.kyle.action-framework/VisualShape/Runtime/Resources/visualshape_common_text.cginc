#include "visualshape_common.cginc"

float4 _Color;
float4 _FadeColor;
UNITY_DECLARE_TEX2D(_MainTex);
UNITY_DECLARE_TEX2D(_FallbackTex);
float _FallbackAmount;
float _TransitionPoint;
float _MipBias;
float _GammaCorrection;

struct vertex {
    float4 pos : POSITION;
    float4 color : COLOR;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f {
    float4 col : COLOR;
    float2 uv: TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f vert_base (vertex v, float4 tint, out float4 outpos : SV_POSITION) {
    UNITY_SETUP_INSTANCE_ID(v);
    v2f o;
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.uv = v.uv;
    o.col = v.color * tint;
    o.col.rgb = ConvertSRGBToDestinationColorSpace(o.col.rgb);
    outpos = TransformObjectToHClip(v.pos.xyz);
    return o;
}

float getAlpha(float2 uv) {
    float rawSignedDistance = UNITY_SAMPLE_TEX2D(_MainTex, uv).a;
    float scale = 1.0 / fwidth(rawSignedDistance);
    float thresholdedDistance = (rawSignedDistance - 0.5) * scale;
    float color = clamp(thresholdedDistance + 0.5, 0.0, 1.0);
    return color;
}

float4 frag (v2f i, float4 screenPos : VPOS) : SV_Target {
    float fallbackAlpha = UNITY_SAMPLE_TEX2D_BIAS(_FallbackTex, i.uv, _MipBias).a;
    fallbackAlpha *= 1.2;

    float pixelSize = length(float2(ddx(i.uv.x), ddy(i.uv.x)));

    float sdfAlpha = getAlpha(i.uv);

    float sdfTextureWidth = 1024;
    float transitionSharpness = 10;
    float blend = clamp(transitionSharpness*(_TransitionPoint*pixelSize*sdfTextureWidth - 1.0), 0, 1);

    float alpha = lerp(sdfAlpha, fallbackAlpha, blend * _FallbackAmount);

    float4 blendcolor = float4(1,1,1,1);

    return blendcolor * i.col * float4(1, 1, 1, alpha);
}
