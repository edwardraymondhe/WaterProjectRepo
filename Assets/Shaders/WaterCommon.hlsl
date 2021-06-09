#ifndef WATER_COMMON_INCLUDED
#define WATER_COMMON_INCLUDED

#define SHADOWS_SCREEN 0

#include "Gerstner.hlsl"
#include "WaterInput.hlsl"
#include "WaterReflection.hlsl"
///////////////////////////////////////////////////////////////////////////////
//                  				Structs		                             //
///////////////////////////////////////////////////////////////////////////////

struct WaterVertexInput // vert struct
{
    float4 vertex : POSITION; // vertex positions
    float2 texcoord : TEXCOORD0; // local UVs
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct WaterVertexOutput // fragment struct
{
    float4 uv : TEXCOORD0; // Geometric UVs stored in xy, and world(pre-waves) in zw
    float3 posWS : TEXCOORD1; // world position of the vertices
    half3 normal : NORMAL; // vert normals
    float3 viewDir : TEXCOORD2; // view direction
    float3 originalPos : TEXCOORD3; // screen position of the verticies before wave distortion
    half4 shadowCoord : TEXCOORD4; // for ssshadows
    float4 additionalData : TEXCOORD5;
    half2 fogFactorNoise : TEXCOORD6;
    // x = distance to surface, y = distance to surface, z = normalized wave height, w = horizontal movement
    float4 clipPos : SV_POSITION;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};


///////////////////////////////////////////////////////////////////////////////
//                  		     	Utilities                                //
///////////////////////////////////////////////////////////////////////////////
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

half3 Scattering(half depth)
{
    return SAMPLE_TEXTURE2D(_ScatteringRamp, sampler_ScatteringRamp, half2(depth, 0.375h)).rgb;
}

half3 Absorption(half depth)
{
    return SAMPLE_TEXTURE2D(_ScatteringRamp, sampler_ScatteringRamp, half2(depth, 0.0h)).rgb;
}

float2 AdjustedDepth(half2 uvs, half4 additionalData)
{
    float rawD = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_ScreenTextures_linear_clamp, uvs);
    float d = LinearEyeDepth(rawD, _ZBufferParams);
    return float2(d * additionalData.x - additionalData.y, (rawD * -_ProjectionParams.x) + (1 - UNITY_REVERSED_Z));
}

float WaterTextureDepth(float3 posWS)
{
    return (1 - SAMPLE_TEXTURE2D_LOD(_WaterDepthMap, sampler_WaterDepthMap_linear_clamp, posWS.xz * 0.002 + 0.5, 1).r) *
        (_Visibility + _VeraslWater_DepthCamParams.x) - _VeraslWater_DepthCamParams.x;
}

float3 WaterDepth(float3 posWS, half4 additionalData, half2 screenUVs) // x = seafloor depth, y = water depth
{
    float3 outDepth = 0;
    outDepth.xz = AdjustedDepth(screenUVs, additionalData);
    float wd = WaterTextureDepth(posWS);
    outDepth.y = wd + posWS.y;
    return outDepth;
}

half3 Refraction(half2 distortion, half depth, real depthMulti)
{
    half3 output = SAMPLE_TEXTURE2D_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture_linear_clamp, distortion,
                                        depth * 0.25).rgb;
    output *= Absorption((depth) * depthMulti);
    return output;
}

half2 DistortionUVs(half depth, float3 normalWS)
{
    half3 viewNormal = mul((float3x3)GetWorldToHClipMatrix(), -normalWS).xyz;

    return viewNormal.xz * saturate((depth) * 0.005);
}

half4 AdditionalData(float3 postionWS, WaveStruct wave)
{
    half4 data = half4(0.0, 0.0, 0.0, 0.0);
    float3 viewPos = TransformWorldToView(postionWS);
    data.x = length(viewPos / viewPos.z); // distance to surface
    data.y = length(GetCameraPositionWS().xyz - postionWS); // local position in camera space
    data.z = wave.position.y / _MaxWaveHeight; // encode the normalized wave height into additional data
    data.w = wave.position.x + wave.position.z;
    return data;
}

WaterVertexOutput WaveVertexOperations(WaterVertexOutput input)
{
    float time = _Time.y;
    input.normal = float3(0, 1, 0);
    input.fogFactorNoise.y = ((noise2D((input.posWS.xz * 0.5) + time) +
        noise2D((input.posWS.xz * 1) + time)) * 0.25 - 0.5) + 1;

    // Detail UVs
    input.uv.zw = input.posWS.xz * 0.1h + time * 0.05h + (input.fogFactorNoise.y * 0.1);
    input.uv.xy = input.posWS.xz * 0.4h - time.xx * 0.1h + (input.fogFactorNoise.y * 0.2);

    half4 screenUV = ComputeScreenPos(TransformWorldToHClip(input.posWS));
    screenUV.xyz /= screenUV.w;

    
    // shallows mask
    half waterDepth = WaterTextureDepth(input.posWS);
    input.posWS.y += pow(saturate((-waterDepth + 1.5) * 0.4), 2);

    //Gerstner here
    WaveStruct wave;
    SampleWaves(input.posWS, saturate((waterDepth * 0.1 + 0.05)), wave);
    input.normal = wave.normal.xzy;
    input.posWS += wave.position;

    // Dynamic displacement
    // half4 waterFX = SAMPLE_TEXTURE2D_LOD(_WaterFXMap, sampler_ScreenTextures_linear_clamp, screenUV.xy, 0);
    // input.posWS.y += waterFX.w * 2 - 1;

    // After waves
    input.clipPos = TransformWorldToHClip(input.posWS);
    input.shadowCoord = ComputeScreenPos(input.clipPos);
    input.viewDir = SafeNormalize(_WorldSpaceCameraPos - input.posWS);

    // Additional data
    input.additionalData = AdditionalData(input.posWS, wave);

    // distance blend
    half distanceBlend = saturate(abs(length((_WorldSpaceCameraPos.xz - input.posWS.xz) * 0.005)) - 0.25);
    input.normal = lerp(input.normal, half3(0, 1, 0), distanceBlend);

    return input;
}

///////////////////////////////////////////////////////////////////////////////
//               	   Vertex and Fragment functions                         //
///////////////////////////////////////////////////////////////////////////////

// Vertex: Used for Standard non-tessellated water
WaterVertexOutput WaterVertex(WaterVertexInput v)
{
    WaterVertexOutput o = (WaterVertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.uv.xy = v.texcoord; // geo uvs
    o.posWS = TransformObjectToWorld(v.vertex.xyz);

    o = WaveVertexOperations(o);

    return o;
}

// Fragment for water
half4 WaterFragment(WaterVertexOutput IN) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN);
    //half3 pos = IN.normal;
    half3 screenUV = IN.shadowCoord.xyz / IN.shadowCoord.w;

    float3 depth = WaterDepth(IN.posWS, IN.additionalData, screenUV.xy);
    half depthEdge = saturate(depth.y * 20 + 1);
    half depthMulti = 1 / _Visibility;;

    half2 distortion = DistortionUVs(depth.x, IN.normal);
    distortion = screenUV.xy + distortion; // * clamp(depth.x, 0, 5);
    float d = depth.x;
    depth.xz = AdjustedDepth(distortion, IN.additionalData);
    distortion = depth.x < 0 ? screenUV.xy : distortion;
    depth.x = depth.x < 0 ? d : depth.x;

    half fresnelTerm = FresnelTerm(IN.normal, IN.viewDir.xyz);
    half3 refraction = Refraction(distortion, depth.x, depthMulti);

    half2 jitterUV = screenUV.xy * _ScreenParams.xy * _BumpMap_TexelSize.xy;
    float3 jitterTexture = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, jitterUV).xyz * 2 - 1;
    float3 lightJitter = IN.posWS + jitterTexture.xzy * 2.5;
    Light mainLightJittered = GetMainLight(TransformWorldToShadowCoord(lightJitter));
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.posWS));
    half shadow = mainLightJittered.shadowAttenuation;
    half3 GI = SampleSH(IN.normal);
    
    half3 sss = 1 * (shadow * mainLight.color + GI);
    sss = Scattering(depth.x * depthMulti);

    half2 detailBump1 = SAMPLE_TEXTURE2D(_SurfaceMap, sampler_SurfaceMap, IN.uv.zw).xy * 2 - 1;
    half2 detailBump2 = SAMPLE_TEXTURE2D(_SurfaceMap, sampler_SurfaceMap, IN.uv.xy).xy * 2 - 1;
    half2 detailBump = (detailBump1 + detailBump2 * 0.5) * saturate(depth.x * 0.25 + 0.25);
    
    IN.normal += half3(detailBump.x, 0, detailBump.y) * _BumpScale;
    IN.normal = normalize(IN.normal);

    BRDFData brdfData;
    InitializeBRDFData(half3(0, 0, 0), 0, half3(1, 1, 1), 0.9, 1, brdfData);
    half3 spec = DirectBDRF(brdfData, IN.normal, mainLight.direction, IN.viewDir) * shadow * mainLight.color;

    half3 reflection = SampleReflections(IN.normal, screenUV.xy, fresnelTerm, 0.0);
   
    
    //reflection = SAMPLE_TEXTURE2D(_PlanarReflectionTexture, sampler_ScreenTextures_linear_clamp, screenUV).rgb;
    reflection = clamp(reflection + spec, 0, 1024) * depthEdge;

    half3 diffuse = refraction + sss;
    half3 comp = reflection + diffuse;
    
    //return half4(reflection, 1);
    return half4(comp,1);
}

#endif // WATER_COMMON_INCLUDED
