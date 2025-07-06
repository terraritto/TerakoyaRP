#ifndef TERAKOYA_WARD_PASS_INCLUDED
#define TERAKOYA_WARD_PASS_INCLUDED

#include "../../ShaderLibrary/Common.hlsl"
#include "../../ShaderLibrary/Surface.hlsl"
#include "../../ShaderLibrary/Shadows.hlsl"
#include "../../ShaderLibrary/Light.hlsl"
#include "../../ShaderLibrary/BRDF.hlsl"
#include "../../ShaderLibrary/Lighting.hlsl"

/*
cbuffer UnityPerMaterial
{
float4 _BaseColor;
};
*/

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
	UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
	UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
	UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
    UNITY_DEFINE_INSTANCED_PROP(float, _AlphaX)
    UNITY_DEFINE_INSTANCED_PROP(float, _AlphaY)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct Attributes
{
	float3 positionOS : POSITION;
	float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
	float2 baseUV : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float3 positionWS : VAR_POSITION;
	float3 normalWS : VAR_NORMAL;
    float3 tangentWS : VAR_TANGENT;
    float3 binormalWS : VAR_BINORMAL;
	float2 baseUV : VAR_BASE_UV;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings WardPassVertex(Attributes input)
{
	Varyings output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	output.positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(output.positionWS);

    // BTN
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.tangentWS = TransformObjectToWorld(input.tangentOS.xyz);
    output.binormalWS = normalize(cross(input.tangentOS.xyz, input.normalOS.xyz) * 
		input.tangentOS.w * unity_WorldTransformParams.w);
    output.binormalWS = TransformObjectToWorld(output.binormalWS.xyz);
	
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
	output.baseUV = input.baseUV * baseST.xy + baseST.zw;
	return output;
}

float4 WardPassFragment(Varyings input) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);
	float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
	float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
	float4 base = baseMap * baseColor;
#ifdef _CLIPPING
	clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
#endif

	WardSurface surface;
	surface.normal = normalize(input.normalWS);
    surface.tangent = normalize(input.tangentWS);
    surface.binormal = normalize(input.binormalWS);
	surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
	surface.color = base.rgb;
	surface.alpha = base.a;
	surface.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
	surface.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
    surface.alpha_xy.x = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _AlphaX);
    surface.alpha_xy.y = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _AlphaY);

#if defined(_PREMULTIPLY_ALPHA)
	WardBRDF brdf = GetWardBRDF(surface, true);
#else
	WardBRDF brdf = GetWardBRDF(surface);
#endif

	float3 color = GetLightingForWard(surface, brdf);

	return float4(color, surface.alpha);
}

#endif