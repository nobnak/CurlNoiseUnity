Shader "Custom/Collider" {
	Properties {
		_Color ("Color", Color) = (1, 1, 1, 0.5)
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 200 ZWrite Off ZTest Always Fog { Mode Off }
		Blend SrcAlpha OneMinusSrcAlpha
		
		CGPROGRAM
		#pragma surface surf Lambert alpha

		float4 _Color;

		struct Input {
			float4 _Color;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			o.Albedo = _Color.rgb;
			o.Alpha = _Color.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
