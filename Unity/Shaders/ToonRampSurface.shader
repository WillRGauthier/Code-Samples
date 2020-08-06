Shader "Holistic/ToonRampSurface" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_RampTex ("Ramp Texture", 2D) = "white" {}
	}
	SubShader {
		CGPROGRAM
		#pragma surface surf Lambert

		struct Input {
			float2 uv_RampTex;
			float3 viewDir;
		};

		fixed4 _Color;
		sampler2D _RampTex;

		void surf (Input IN, inout SurfaceOutput o) {
			float diffuse = dot(o.Normal, IN.viewDir);
			float u = diffuse * 0.5 + 0.5;
			float2 uv = u;
			o.Albedo = (_Color * tex2D(_RampTex, uv)).rgb;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
