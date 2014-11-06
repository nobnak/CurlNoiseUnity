﻿Shader "Custom/Pos" {
	Properties {
		_Color ("Color", Color) = (1, 1, 1, 1)
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 200 ZWrite Off ZTest Always Fog { Mode Off }
		Blend SrcAlpha One
		
		Pass {
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4 _Color;
			int Id;
			StructuredBuffer<float3> PosIn;

			struct Input {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};
			struct vs2ps {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};
			
			vs2ps vert(Input IN) {
				float3 center = PosIn[Id];
				float4 posWorld = mul(_Object2World, IN.vertex);
				posWorld.xyz += center;
			
				vs2ps OUT;
				OUT.vertex = mul(UNITY_MATRIX_P, mul(UNITY_MATRIX_V, posWorld));
				OUT.uv = IN.uv;
				return OUT;
			}
			
			float4 frag(vs2ps IN) : COLOR {
				return _Color;
			}
			ENDCG
		}
	}
}
