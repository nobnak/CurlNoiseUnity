Shader "Custom/DebugNormal" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "black" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200 Cull Off
		
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;

			struct Input {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};
			
			Input vert(Input IN) {
				Input OUT;
				OUT.vertex = mul(UNITY_MATRIX_MVP, IN.vertex);
				OUT.uv = IN.uv;
				return OUT;
			}
			float4 frag(Input IN) : COLOR {
				float3 n = UnpackNormal(tex2D(_MainTex, IN.uv));
				return float4(0.5 * (n + 1.0), 1.0);
			}
			ENDCG
		}
	} 
}
