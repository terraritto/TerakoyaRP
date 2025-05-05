#ifndef TERAKOYA_CUSTOM_SURFACE_INCLUDED
#define TERAKOYA_CUSTOM_SURFACE_INCLUDED

struct Surface
{
    float3 normal;
    float3 viewDirection;
    float3 color;
    float alpha;
    float metallic;
    float smoothness;
};

struct WardSurface
{
    float3 normal;
    float3 tangent;
    float3 binormal;
    float3 viewDirection;
    float3 color;
    float alpha;
    float metallic;
    float smoothness;
    float2 alpha_xy;
};

#endif