#ifndef WATER_UTILITIES_INCLUDED
#define WATER_UTILITIES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "WaterInput.hlsl"

//计算Fresnel参数
half FresnelTerm(half3 normalWS, half3 viewDirectionWS)
{
    return pow(1.0 - saturate(dot(normalWS, viewDirectionWS)), 10);
}

// Simple noise from thebookofshaders.com
// 2D Random
float2 random2D(float2 st)
{
    st = float2(dot(st, float2(127.1, 311.7)), dot(st, float2(269.5, 183.3)));
    return -1.0 + 2.0 * frac(sin(st) * 43758.5453123);
}

// 2D Noise based on Morgan McGuire @morgan3d
// https://www.shadertoy.com/view/4dS3Wd
float noise2D(float2 st)
{
    float2 i = floor(st);
    float2 f = frac(st);

    float2 u = f * f * (3.0 - 2.0 * f);

    return lerp(lerp(dot(random2D(i), f),
                     dot(random2D(i + float2(1.0, 0.0)), f - float2(1.0, 0.0)), u.x),
                lerp(dot(random2D(i + float2(0.0, 1.0)), f - float2(0.0, 1.0)),
                     dot(random2D(i + float2(1.0, 1.0)), f - float2(1.0, 1.0)), u.x), u.y);
}

// 根据深度信息获取散射颜色
half3 ScatterColor(half depth)
{
    return SAMPLE_TEXTURE2D(_ScatteringRamp, sampler_ScatteringRamp, half2(depth, 0.375h)).rgb;
}

// 根据深度信息获取吸收颜色
half3 AbsorptionColor(half depth)
{
    return SAMPLE_TEXTURE2D(_ScatteringRamp, sampler_ScatteringRamp, half2(depth, 0.0h)).rgb;
}

//根据水面法向量和深度计算扭曲后的UV
half2 DistortionUVs(half depth, float3 normalWS)
{
    half3 viewNormal = mul((float3x3)GetWorldToHClipMatrix(), -normalWS).xyz;
    return viewNormal.xz * saturate((depth) * 0.005);
}

//根据UV和AdditionalData计算出真实的深度
float2 CalculateDepthFromUV(half2 uvs, half4 additionalData)
{
    float rawD = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_ScreenTextures_linear_clamp, uvs);
    float d = LinearEyeDepth(rawD, _ZBufferParams);
    return float2(d * additionalData.x - additionalData.y, (rawD * -_ProjectionParams.x) + (1 - UNITY_REVERSED_Z));
}



#endif // WATER_LIGHTING_INCLUDED