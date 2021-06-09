#ifndef WATER_COMMON_INCLUDED
#define WATER_COMMON_INCLUDED

#define SHADOWS_SCREEN 0

#include "Gerstner.hlsl"
#include "WaterInput.hlsl"
#include "WaterReflection.hlsl"
#include "WaterUtilities.hlsl"
///////////////////////////////////////////////////////////////////////////////
//                  				Structs		                             //
///////////////////////////////////////////////////////////////////////////////

struct WaterVertexInput // vert struct
{
    float4 vertex : POSITION;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct WaterVertexOutput // fragment struct
{
    float4 uv : TEXCOORD0;
    // xy存储位置UV信息,zw存储世界坐标系下uv信息
    float3 posWS : TEXCOORD1;
    // 顶点的世界坐标
    half3 normal : NORMAL;
    // 法向量
    float3 viewDir : TEXCOORD2;
    // 视角向量
    float3 originalPos : TEXCOORD3;
    // 原位置
    half4 shadowCoord : TEXCOORD4;
    // SSS 计算系数
    float4 additionalData : TEXCOORD5;
    // waterattack: x = distance to surface, y = distance to surface, z = normalized wave height, w = horizontal movement
    half2 fogFactorNoise : TEXCOORD6;
    // waterattack: x = fogFactor y = Noise
    float4 clipPos : SV_POSITION;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};


///////////////////////////////////////////////////////////////////////////////
//                  		     	Utilities                                //
///////////////////////////////////////////////////////////////////////////////


float WaterTextureDepth(float3 posWS)
{
    return (1 - SAMPLE_TEXTURE2D_LOD(_WaterDepthMap, sampler_WaterDepthMap_linear_clamp, posWS.xz * 0.002 + 0.5, 1).r) *
        (_Visibility + _VeraslWater_DepthCamParams.x) - _VeraslWater_DepthCamParams.x;
}

float3 WaterDepth(float3 posWS, half4 additionalData, half2 screenUVs) // x = seafloor depth, y = water depth
{
    float3 outDepth = 0;
    outDepth.xz = CalculateDepthFromUV(screenUVs, additionalData);
    float wd = WaterTextureDepth(posWS);
    outDepth.y = wd + posWS.y;
    return outDepth;
}

half3 AbsorptionTerm(half2 distortion, half depth, real depthMulti)
{
    half3 output = SAMPLE_TEXTURE2D_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture_linear_clamp, distortion,
                                        depth * 0.25).rgb;
    output *= AbsorptionColor((depth) * depthMulti);
    return output;
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

half3 SurfaceNormal(half3 normal, float2 localUV, float2 worldUV, float depth)
{
    half2 detailBump_local = SAMPLE_TEXTURE2D(_SurfaceMap, sampler_SurfaceMap, worldUV).xy * 2 - 1;
    half2 detailBump_world = SAMPLE_TEXTURE2D(_SurfaceMap, sampler_SurfaceMap, localUV).xy * 2 - 1;
    half2 detailBump = (detailBump_local + detailBump_world * 0.5) * saturate(depth * 0.25 + 0.25);
    normal += half3(detailBump.x, 0, detailBump.y) * _BumpScale;
    normal = normalize(normal);
    return normal;
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

    //计算Gerstner波
    WaveStruct wave;
    SampleWaves(input.posWS, saturate((waterDepth * 0.1 + 0.05)), wave);
    input.normal = wave.normal.xzy;
    input.posWS += wave.position;

    // 运动船的水波
    // half4 waterFX = SAMPLE_TEXTURE2D_LOD(_WaterFXMap, sampler_ScreenTextures_linear_clamp, screenUV.xy, 0);
    // input.posWS.y += waterFX.w * 2 - 1;

    //计算SV_POSITION,UV和视角向量
    input.clipPos = TransformWorldToHClip(input.posWS);
    input.shadowCoord = ComputeScreenPos(input.clipPos);
    input.viewDir = SafeNormalize(_WorldSpaceCameraPos - input.posWS);

    // Additional data
    input.additionalData = AdditionalData(input.posWS, wave);

    // 根据距离对远处顶点的法线进行混合，降低其法向量
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
    
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.posWS));
    
    half3 screenUV = IN.shadowCoord.xyz / IN.shadowCoord.w;

    float3 depth = WaterDepth(IN.posWS, IN.additionalData, screenUV.xy);
    half depthEdge = saturate(depth.y * 20 + 1);
    half depthMulti = 1 / _Visibility;;

    half2 distortion = screenUV.xy + DistortionUVs(depth.x, IN.normal);
    float d = depth.x;

    depth.xz = CalculateDepthFromUV(distortion, IN.additionalData);
    distortion = depth.x < 0 ? screenUV.xy : distortion;
    depth.x = depth.x < 0 ? d : depth.x;

    half fresnelTerm = FresnelTerm(IN.normal, IN.viewDir.xyz);

    // 加入了BumpMap噪声后进行计算
    half2 r_UV = screenUV.xy * _ScreenParams.xy * _BumpMap_TexelSize.xy;
    float3 r_Texture = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, r_UV).xyz * 2 - 1;
    float3 r_light = IN.posWS + r_Texture.xzy * 2.5;
    Light r_mainLight = GetMainLight(TransformWorldToShadowCoord(r_light));
    
    //阴影信息
    half shadow = r_mainLight.shadowAttenuation;

    //使用surfaceMap对法向量进行扰动，从而产生更精细的波纹
    IN.normal = SurfaceNormal(IN.normal,IN.uv.xy,IN.uv.zw,depth.x);

    //计算高光
    half3 spec = CalculateSpecular(mainLight,IN.viewDir,IN.normal) * shadow;

    //计算散射颜色
    half3 sss = ScatterColor(depth.x * depthMulti);

    //计算反射
    half3 reflection = CalculateReflection(IN.normal, screenUV.xy, fresnelTerm, 0.0);

    //计算折射吸收
    half3 absorption = AbsorptionTerm(distortion, depth.x, depthMulti);

    //计算光照
    half3 lighting = clamp(reflection + spec, 0, 1024) * depthEdge;
    
    half3 diffuse = absorption + sss;
    half3 comp = lighting + diffuse;

    //return half4(absorption, 1);
    //return half4(lighting, 1);
    //return half4(sss, 1);
    return half4(comp, 1);
}

#endif // WATER_COMMON_INCLUDED
