Shader "KT/Custom/Glitch" {
	Properties {
		_Color ("Tint", Color) = (1,1,1,1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"

			#pragma vertex vert
			#pragma fragment frag

			uniform float4 _Color;

			struct vert2frag {
				float4 pos : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				float2 screenPos : TEXCOORD1;
			};
		
			struct frag2screen {
				float4 color : COLOR;
			};

			struct input {
				 float4 pos : POSITION;
				 half2 uv : TEXCOORD0;
			};

			float random (float2 st) {
				//return st.x;
				float OUT = sin(dot(floor(abs(st.xy)),float2(12.9898,78.233)))*43758.5453123;
				return abs(OUT) - floor(abs(OUT));
			}

			vert2frag vert(input IN) {
				vert2frag o;
				o.pos = UnityObjectToClipPos(IN.pos);
				//o.texcoord = MultiplyUV(UNITY_MATRIX_TEXTURE0, IN.uv);
				o.screenPos = ComputeScreenPos(o.pos);

				return o;
			}

			frag2screen frag(vert2frag IN)
			{
				frag2screen outVar;
				//float2 uv = IN.texcoord.xy;
				
				//float4 tex = tex2D(_MainTex,uv); 
				float2 worldpos = IN.screenPos * _ScreenParams.xy;
				//float2 fixedpos = float2(worldpos.x / _Data.x, worldpos.y / _Data.y);
				//float2 fixedpos = float2(IN.screenPos.x / _Data.x, IN.screenPos.y / _Data.y);

				outVar.color = _Color * random(worldpos / 2.0); 
				return outVar;
			} 
			ENDCG
		}
	}
}
