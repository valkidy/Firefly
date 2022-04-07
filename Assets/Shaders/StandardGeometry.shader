Shader "Standard Geometry Shader Example"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
		_MainTex("Albedo", 2D) = "white" {}

		[Space]
		_LocalTime("Animation Time", Range(0, 4)) = 0.0
		_ElapsedTime("Particle Time", Range(0, 1)) = 0.0
	}
		SubShader
		{
			Tags { "RenderType" = "Opaque" }			
			LOD 100
			Cull Off

			Pass
			{				
				CGPROGRAM
				#pragma target 4.0
				#pragma vertex Vertex
				#pragma geometry Geometry
				#pragma fragment Fragment
				// #pragma multi_compile_prepassfinal noshadowmask nodynlightmap nodirlightmap nolightmap
				#include "StandardGeometry.cginc"
				ENDCG
			}			
		}
}
