#ifndef WATER_REFLECTION_INCLUDED
#define WATER_REFLECTION_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "WaterInput.hlsl"

half3 CalculateReflection(half3 normalWS, half2 screenUV, half fresnelTerm, half roughness)
{
    half3 reflection = 0;
    half2 reflectionUV = screenUV + normalWS.zx * half2(0.02, 0.15);
    reflection += SAMPLE_TEXTURE2D_LOD(_PlanarReflectionTexture, sampler_ScreenTextures_linear_clamp, reflectionUV,
                                       6 * roughness).rgb; //planar reflection
    return reflection * fresnelTerm;
}

half3 CalculateSpecular(Light mainLight, half3 viewDir, half3 normal)
{
    BRDFData brdfData;
    InitializeBRDFData(half3(0, 0, 0), 0, half3(1, 1, 1), 0.9, 1, brdfData);
    half3 spec = DirectBDRF(brdfData, normal, mainLight.direction, viewDir) * mainLight.color;
    return spec;
}

#endif // WATER_LIGHTING_INCLUDED
