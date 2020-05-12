/*
* File:        Jump Flood Shader
* Author:      Robert Neff
* Date:        11/28/17
* Description: Implements jump flooding method on GPU.
*/

Shader "Custom/JumpFloodShader" {
	// Define shader properties
	Properties {
		// _name("unity editor name", type) = value
		_MainTex ("Texture", 2D) = "white" {}
	}
	
	// Shader code
	SubShader {
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			// Input variables
			uniform float4 _k;
			uniform float _blue;
			sampler2D _MainTex;

			// Vertex incoming data
			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			// Vertex to fragment data
			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			// Vertex shader
			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			// Fragment shader
			fixed4 frag (v2f input) : COLOR {
				// Find closest site in surrounding pixels
				float2 closestSite = tex2D(_MainTex, input.uv).xy;
				float closestDist = distance(input.uv, closestSite);

				// Color appropriately
				for (int i = -1; i <= 1; i++) {
					for (int j = -1; j <= 1; j++) {
						if (i == 0 && j == 0) continue;

						float2 other = tex2D(_MainTex, input.uv + float2(_k.x * i, _k.y * j)).xy;

						if (closestDist > distance(input.uv, other)) {
							closestSite = other;
							closestDist = distance(input.uv, closestSite);
						}
					}
				}
				return fixed4(closestSite, _blue, 1.0f);
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
}
