Shader "MyWaterSystem/Water"
{
	Properties
	{
		_BumpScale("Bump Scale", Range(0, 2)) = 0.2
		_BumpMap ("Bump map", 2D) = "bump" {}
		_CausticsScale("Caustics Scale", Float) = 0.5
        [NoScaleOffset]_CausticMap("Caustics", 2D) = "white" {}
        _BlendDistance("BlendDistance", Float) = 3
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent-100" "RenderPipeline" = "UniversalPipeline" }
		ZWrite On

		Pass
		{
			Name "Water"
			Tags{"LightMode" = "UniversalForward"}

			HLSLPROGRAM
			#include "WaterCommon.hlsl"
			
			#pragma vertex WaterVertex
			#pragma fragment WaterFragment

			ENDHLSL
		}
	}
	FallBack "Hidden/InternalErrorShader"
}
