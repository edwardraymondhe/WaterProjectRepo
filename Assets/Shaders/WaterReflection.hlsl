#ifndef WATER_REFLECTION_INCLUDED
#define WATER_REFLECTION_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "WaterInput.hlsl"

half FresnelTerm(half3 normalWS, half3 viewDirectionWS)
{
    return pow(1.0 - saturate(dot(normalWS, viewDirectionWS)), 10);//fresnel TODO - find a better place
}

///////////////////////////////////////////////////////////////////////////////
//                           Reflection Modes                                //
///////////////////////////////////////////////////////////////////////////////

half3 SampleReflections(half3 normalWS, half2 screenUV, half fresnelTerm, half roughness)
{
    half3 reflection = 0;
    // get the perspective projection
    float2 p11_22 = float2(unity_CameraInvProjection._11, unity_CameraInvProjection._22) * 10;
    // conver the uvs into view space by "undoing" projection
    float3 viewDir = -(float3((screenUV * 2 - 1) / p11_22, -1));

    half3 viewNormal = mul(normalWS, (float3x3)GetWorldToViewMatrix()).xyz;
    half3 reflectVector = reflect(-viewDir, viewNormal);

    half2 reflectionUV = screenUV + normalWS.zx * half2(0.02, 0.15);
    reflection += SAMPLE_TEXTURE2D_LOD(_PlanarReflectionTexture, sampler_ScreenTextures_linear_clamp, reflectionUV, 6 * roughness).rgb;//planar reflection
    //do backup
    //return reflectVector.yyy;
    return reflection * fresnelTerm;
}

#endif // WATER_LIGHTING_INCLUDED