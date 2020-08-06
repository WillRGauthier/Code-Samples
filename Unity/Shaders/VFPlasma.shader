Shader "Holistic/VFPlasma"
{
	Properties{
		_Tint("Color Tint", Color) = (1,1,1,1)
		_Speed("Speed", Range(1,100)) = 10
		_Scale1("Scale1", Range(0.1,10)) = 2
		_Scale2("Scale2", Range(0.1,10)) = 2
		_Scale3("Scale3", Range(0.1,10)) = 2
		_Scale4("Scale4", Range(0.1,10)) = 2
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			float4 _Tint;
			float _Speed;
			float _Scale1;
			float _Scale2;
			float _Scale3;
			float _Scale4;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				const float PI = 3.14159265;
				float t = _Time.x * _Speed;

				float posX = i.vertex.x / 1000;
				float posY = i.vertex.y / 1000;

				// Vertical
				float c = sin(posX * _Scale1 + t);

				// Horizontal
				c += sin(posY * _Scale2 + t);

				// Diagonal
				c += sin(_Scale3 * (posX * sin(t / 2.0) + posY * cos(t / 3)) + t);

				// Circular
				float c1 = pow(posX + 0.5 * sin(t / 5), 2);
				float c2 = pow(posY + 0.5 * cos(t / 3), 2);
				c += sin(sqrt(_Scale4 * (c1 + c2) + 1 + t));

				float4 col;
				col.r = sin(c / 4.0*PI);
				col.g = sin(c / 4.0*PI + 2 * PI / 4);
				col.b = sin(c / 4.0*PI + 4 * PI / 4);
				col.a = 1;
				col *= _Tint;
				return col;
			}
			ENDCG
		}
	}
}
