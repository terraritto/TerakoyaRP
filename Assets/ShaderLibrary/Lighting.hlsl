#ifndef TERAKOYA_CUSTOM_LIGHTING_INCLUDED
#define TERAKOYA_CUSTOM_LIGHTING_INCLUDED

float3 InComingLight(Surface surface, Light light)
{
    return saturate(dot(surface.normal, light.direction) * light.attenuation) * light.color;
}

float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
    return InComingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

float3 GetLighting(Surface surfaceWS, BRDF brdf)
{
    float3 color = 0.0;
    for (int i = 0; i < GetDirectionalLightCount(); i++)
    {
        color += GetLighting(surfaceWS, brdf, GetDirectionalLight(i, surfaceWS));
    }
    return color;
}

// for ward
float3 InComingLightForWard(WardSurface surface, Light light)
{
    return saturate(dot(surface.normal, light.direction)) * light.color;
}

float3 GetLightingForWard(WardSurface surface, WardBRDF brdf, Light light)
{
    return InComingLightForWard(surface, light) * DirectWardBRDF(surface, brdf, light);
}

float3 GetLightingForWard(WardSurface surfaceWS, WardBRDF brdf)
{
    float3 color = 0.0;
    for (int i = 0; i < GetDirectionalLightCount(); i++)
    {
        color += GetLightingForWard(surfaceWS, brdf, GetDirectionalLightForWard(i, surfaceWS));
    }
    return color;
}

#endif