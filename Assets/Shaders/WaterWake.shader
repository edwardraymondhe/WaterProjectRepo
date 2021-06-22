Shader "MyWaterSystem/Wake"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		[Toggle] _Invert ("Invert?", Float) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend mode", Float) = 0.0
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend mode", Float) = 0.0
		
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" }
		ZWrite Off
		Blend[_SrcBlend][_DstBlend]
		LOD 100

		Pass
		{
			Name "Wake"
			Tags{"LightMode" = "Wake"}
			
			HLSLPROGRAM
			#pragma vertex WaterFXVertex
			#pragma fragment WaterFXFragment
			#pragma shader_feature _INVERT_ON
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct VertexData
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
    			float4 tangentOS : TANGENT;
				half4 color : COLOR;
				float2 uv : TEXCOORD0;
			};

			struct FragmentData
			{
				float2 uv : TEXCOORD0;
				half3 normal : TEXCOORD1; 
    			half3 tangent : TEXCOORD2;
    			half3 binormal : TEXCOORD3; 
				half4 color : TEXCOORD4;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			
			FragmentData WaterFXVertex (VertexData input)
			{
				FragmentData output = (FragmentData)0;
				
				VertexPositionInputs vertexPosition = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs vertexTBN = GetVertexNormalInputs(input.normalOS, input.tangentOS);
				
				output.vertex = vertexPosition.positionCS;				
				output.uv = input.uv;
				output.color = input.color;				
                output.normal = vertexTBN.normalWS;
                output.tangent = vertexTBN.tangentWS;
                output.binormal = vertexTBN.bitangentWS;

				return output;
			}
			
			half4 WaterFXFragment (FragmentData input) : SV_Target
			{
				half4 col = tex2D(_MainTex, input.uv);

				half foamMask = col.r * input.color.r;
				half disp = col.a * 2 - 1;

				disp *= input.color.a;

				half3 tNorm = half3(col.b, col.g, 1) * 2 - 1;

    			half3 normalWS = TransformTangentToWorld(tNorm, half3x3(input.tangent.xyz, input.binormal.xyz, input.normal.xyz));

				normalWS = lerp(half3(0, 1, 0), normalWS, input.color.g);
				half4 comp = half4(foamMask, normalWS.xz, disp);

				#ifdef _INVERT_ON
				comp *= -1;
				#endif
				
				return comp;
			}
			ENDHLSL
		}
	}
}
