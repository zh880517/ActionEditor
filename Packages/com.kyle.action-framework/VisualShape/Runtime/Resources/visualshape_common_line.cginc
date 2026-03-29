#include "visualshape_common.cginc"

struct LineData {
    float3 start;
	uint joinHintInstanceIndex;
	float3 end;
	float width;
	float4 color;
};

static float2 vertexToSide[6] = {
	float2(-1, -1),
	float2(1, -1),
	float2(1, 1),

	float2(-1, -1),
	float2(1, 1),
	float2(-1, 1),
};

struct line_v2f {
	half4 col : COLOR;
	noperspective float lineWidth: TEXCOORD3;
	noperspective float uv : TEXCOORD4;
	UNITY_VERTEX_OUTPUT_STEREO
};

// d = normalized distance to line
float lineAA(float d) {
	d = max(min(d, 1.0), 0) * 1.116;
	float v = 0.93124*d*d*d - 1.42215*d*d - 0.42715*d + 0.95316;
	v /= 0.95316;
	return max(v, 0);
}


float calculateLineAlpha(line_v2f i, float pixelWidth, float falloffTextureScreenPixels) {
	float dist = abs((i.uv - 0.5)*2);
	float falloffFractionOfWidth = falloffTextureScreenPixels/(pixelWidth*0.5);
	float a = lineAA((abs(dist) - (1 - falloffFractionOfWidth))/falloffFractionOfWidth);
	return a;
}

line_v2f line_vert (appdata_color v, float pixelWidth, float lengthPadding, out float4 outpos : SV_POSITION) {
	UNITY_SETUP_INSTANCE_ID(v);
	line_v2f o;
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
	float4 Mv = TransformObjectToHClip(v.vertex.xyz);
	float3 n = normalize(v.normal);
	float4 Mn = UnityObjectToClipDirection(n);

	float4 delta = (Mn - Mv*Mn.w/Mv.w) / Mv.w;

	delta.xy *= _ScreenParams.xy;
	delta.xy = normalize(delta.xy);
	// Handle DirectX properly
	float2 normalizedScreenSpaceNormal = float2(-delta.y, delta.x) * _ProjectionParams.x;
	float2 screenSpaceNormal = normalizedScreenSpaceNormal / _ScreenParams.xy;
	float4 sn = float4(screenSpaceNormal.x, screenSpaceNormal.y, 0, 0);

	// Left (-1) or Right (1) of the line
	float side = (v.uv.x - 0.5)*2;
	// Make the line wide
	outpos = (Mv / Mv.w) + side*sn*pixelWidth*0.5;

	// -1 or +1 if this vertex is at the start or end of the line respectively.
	float forwards = (v.uv.y - 0.5)*2;
	// Add some additional length to the line
	outpos.xy += forwards*(delta.xy / _ScreenParams.xy)*0.5*lengthPadding;

	// Multiply by w because homogeneous coordinates
	outpos *= Mv.w;
	o.lineWidth = pixelWidth;
	o.uv = v.uv.x;
	return o;
}

line_v2f line_vert_raw (appdata_color v, float4 tint, float pixelWidth, float lengthPadding, out float4 outpos) {
	pixelWidth *= length(v.normal);
	line_v2f o = line_vert(v, pixelWidth, lengthPadding, outpos);
	o.col = v.color * tint;
	o.col.rgb = ConvertSRGBToDestinationColorSpace(o.col.rgb);
	return o;
}
