Shader "Holistic/CutoffWithDiffuse" {
    Properties {
      _MainTex ("Base (RGB)", 2D) = "white" { }
      _StripeWidth ("StripeWidth", Range(1,20)) = 10
	  _StripeColor1 ("Stripe Color1", Color) = (1, 0, 0)
	  _StripeColor2 ("Stripe Color2", Color) = (0, 1, 0)
    }
    SubShader {
      CGPROGRAM
      #pragma surface surf Lambert
      struct Input {
          float2 uv_MainTex;
          float3 viewDir;
          float3 worldPos;
      };

      sampler2D _MainTex;
      float _StripeWidth;
	  float4 _StripeColor1;
	  float4 _StripeColor2;

      void surf (Input IN, inout SurfaceOutput o) {
          o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
          half rim = 1 - saturate(dot(normalize(IN.viewDir), o.Normal));
          o.Emission = frac(IN.worldPos.y*(20-_StripeWidth) * 0.5) > 0.4 ? 
                          _StripeColor1*rim: _StripeColor2*rim;
      }
      ENDCG
    } 
    Fallback "Diffuse"
  }