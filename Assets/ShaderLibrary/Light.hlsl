#ifndef TERAKOYA_CUSTOM_LIGHT_INCLUDED
#define TERAKOYA_CUSTOM_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT 4

CBUFFER_START(_CustomLight)
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT];
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT];
CBUFFER_END

struct Light
{
    float3 color;
    float3 direction;
    float attenuation;
};

int GetDirectionalLightCount()
{
    return _DirectionalLightCount;
}

DirectionalShadowData GetDirectionalShadowData(int lightIndex)
{
    DirectionalShadowData data;
    data.strength = _DirectionalLightShadowData[lightIndex].x;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y;
    return data;
}

Light GetDirectionalLight(int index, Surface surfaceWS)
{
    Light light;
    light.color = _DirectionalLightColors[index].rgb;
    light.direction = _DirectionalLightDirections[index].xyz;
    DirectionalShadowData shadowData = GetDirectionalShadowData(index);
    light.attenuation = GetDirectionalShadowAttenuation(shadowData, surfaceWS);
    return light;
}

Light GetDirectionalLightForWard(int index, WardSurface surfaceWS)
{
    Light light;
    light.color = _DirectionalLightColors[index].rgb;
    light.direction = _DirectionalLightDirections[index].xyz;
    DirectionalShadowData shadowData = GetDirectionalShadowData(index);
    light.attenuation = GetDirectionalShadowAttenuationWard(shadowData, surfaceWS);
    return light;
}

#endif