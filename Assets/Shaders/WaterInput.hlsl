#ifndef WATER_INPUT_INCLUDED
#define WATER_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
half _BumpScale;
half4 _BumpMap_TexelSize;
CBUFFER_END


half _Visibility;
half _MaxWaveHeight;
int _DebugPass;
half4 _VeraslWater_DepthCamParams;
float4x4 _InvViewProjection;

// Screen Effects textures
SAMPLER(sampler_ScreenTextures_linear_clamp);

TEXTURE2D(_PlanarReflectionTexture);
TEXTURE2D(_WaterFXMap);
TEXTURE2D(_CameraDepthTexture);
TEXTURE2D(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture_linear_clamp);

TEXTURE2D(_WaterDepthMap); SAMPLER(sampler_WaterDepthMap_linear_clamp);

// Surface textures
TEXTURE2D(_ScatteringRamp); SAMPLER(sampler_ScatteringRamp);
TEXTURE2D(_SurfaceMap); SAMPLER(sampler_SurfaceMap);
TEXTURE2D(_FoamMap); SAMPLER(sampler_FoamMap);
TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

// Must match Lightweigth ShaderGraph master node
struct SurfaceData
{
    half3 absorption;
	half3 scattering;
    half3 normal;
    half  foam;
};

#endif // WATER_INPUT_INCLUDED