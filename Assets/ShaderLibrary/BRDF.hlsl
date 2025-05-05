#ifndef TERAKOYA_CUSTOM_BRDF_INCLUDED
#define TERAKOYA_CUSTOM_BRDF_INCLUDED

#define MIN_REFLECTIVITY 0.04 // 金属の平均を最低値とする
#define PI 3.1415926535

struct BRDF
{
    float3 diffuse;
    float3 specular;
    float roughness;
};

struct WardBRDF
{
    float3 diffuse;
    float3 specular;
    float2 alpha;
};

float OneMinusReflectivity(float metallic)
{
    float range = 1.0f - MIN_REFLECTIVITY;
    return range - metallic * range;
}

// Optimizing PBR for Mobile [SIGGRAPH 2015]
BRDF GetBRDF(Surface surface, bool applyAlphaToDiffuse = false)
{
    float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);

    BRDF brdf;
    brdf.diffuse = surface.color * oneMinusReflectivity;
    if (applyAlphaToDiffuse)
    {
        brdf.diffuse *= surface.alpha;
    }
    brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
    
    float perceptualRoughness = 
        PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);

    return brdf;
}

float SpecularStrength(Surface surface, BRDF brdf, Light light)
{
    float3 h = SafeNormalize(light.direction + surface.viewDirection);
    float nh2 = Square(saturate(dot(surface.normal, h)));
    float lh2 = Square(saturate(dot(light.direction, h)));
    float2 r2 = Square(brdf.roughness);
    float2 d2 = Square(nh2 * (r2 - 1.0f) + 1.00001);
    float normalization = (brdf.roughness * 4.0f + 2.0f) * PI;
    return r2 / (d2 * max(0.1f, lh2) * normalization);
}

float3 DirectBRDF(Surface surface, BRDF brdf, Light light)
{
    return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

// Notes On Ward BRDF [Walter 2005]
WardBRDF GetWardBRDF(WardSurface surface, bool applyAlphaToDiffuse = false)
{
    float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);

    WardBRDF brdf;
    brdf.diffuse = surface.color * oneMinusReflectivity;
    if (applyAlphaToDiffuse)
    {
        brdf.diffuse *= surface.alpha;
    }
    brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
    
    brdf.alpha = surface.alpha_xy;

    return brdf;
}

float WardStrength(WardSurface surface, WardBRDF brdf, Light light)
{
    float3 h = SafeNormalize(light.direction + surface.viewDirection);
    
    float ax = brdf.alpha.x;
    float ay = brdf.alpha.y;
    
    // calculate exp
    float hX2 = Square(dot(h, surface.tangent) / ax);
    float hY2 = Square(dot(h, surface.binormal) / ay);
    float hn2 = Square(dot(h, surface.normal));
    float exponent = exp(-(hX2 + hY2) / hn2); 

    float ln = saturate(dot(light.direction, surface.normal));
    float vn = saturate(dot(surface.viewDirection, surface.normal));
    float denom = 4.0 * PI * ax * ay * sqrt(max(0.00001f, ln * vn));
    return min(1.0f, (1.0 / denom) * exponent);
}

float3 DirectWardBRDF(WardSurface surface, WardBRDF brdf, Light light)
{
    return WardStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

#endif