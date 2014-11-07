Shader "Custom/Particle" {
	Properties {
		_Color ("Color", Color) = (1, 1, 1, 1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		Pass {
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct Particle {
				float2 x;
				float t;
				float life;
			};

			float4 _Color;
			StructuredBuffer<Particle> ParticleIn;

			struct Input {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
			};
			struct vs2ps {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};
			
			vs2ps vert(Input IN) {
				int id = int(IN.uv2.x);
				Particle p = ParticleIn[id];
				float4 posWorld = mul(_Object2World, IN.vertex);
				posWorld.xy += p.x;
			
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
