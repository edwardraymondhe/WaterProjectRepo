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
    float3 preWaveSP : TEXCOORD3; // screen position of the verticies before wave distortion
    half4 shadowCoord : TEXCOORD4; // for ssshadows
    float4 additionalData : TEXCOORD5;
    // x = distance to surface, y = distance to surface, z = normalized wave height, w = horizontal movement
    float4 clipPos : SV_POSITION;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

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

    // Detail UVs
    //input.uv.zw = input.posWS.xz * 0.1h + time * 0.05h + (input.fogFactorNoise.y * 0.1);
    //input.uv.xy = input.posWS.xz * 0.4h - time.xx * 0.1h + (input.fogFactorNoise.y * 0.2);

    half4 screenUV = ComputeScreenPos(TransformWorldToHClip(input.posWS));
    screenUV.xyz /= screenUV.w;

    // shallows mask
    //half waterDepth = WaterTextureDepth(input.posWS);
    half waterDepth = 0.5;
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

    half fresnelTerm = FresnelTerm(IN.normal, IN.viewDir.xyz);
    half3 reflection = SampleReflections(IN.normal, screenUV.xy, fresnelTerm, 0.0);
    reflection = SAMPLE_TEXTURE2D(_PlanarReflectionTexture, sampler_ScreenTextures_linear_clamp, screenUV).rgb;
    // reflection = reflection;
    //reflection = clamp(reflection + spec, 0, 1024) * depthEdge;

    return half4(reflection, 1);
}

#endif // WATER_COMMON_INCLUDED
