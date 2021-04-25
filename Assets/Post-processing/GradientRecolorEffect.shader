// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/Gradient Recolor Effect" {

	Properties {

 		_MainTex ("Base (RGB)", 2D) = "white" {}

        _RampTex ("Color Ramp", 2D) = "white" {}

		_BlobMin ("Blob Min", float) = 0

		_BlobRange ("Blob Range", float) = 1
 	}

 	SubShader {

 		Pass {

           ZTest Always Cull Off ZWrite Off Fog { Mode off }

 			CGPROGRAM

 			#pragma vertex vert
 			#pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest
 
 			uniform sampler2D _MainTex;        
            uniform sampler2D _RampTex;

			uniform float _BlobMin;
			uniform float _BlobRange;

			uniform float4 _MainTex_TexelSize;

            static const int _BlobCount = 3;
            float4 _Blobs[_BlobCount];
            float _WidthScale;

            float allBlobs (float2 uv) {

                uv.x *= _WidthScale;
                float result = 0;

                for (int i = 0; i < _BlobCount; i++) {
        
                    float4 blob = _Blobs[i];
                    if (blob.w <= 0) continue;
                    float r = length(blob.xy - uv);
                    result += r * 0.3;

                }

                return _BlobMin + result * _BlobRange;
            
            }

            #include "UnityCG.cginc"
            #pragma target 3.0

            struct v2f {

                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
                float4 screenpos : TEXCOORD1;

            };

            v2f vert (appdata_img v) {

                v2f o;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = MultiplyUV(UNITY_MATRIX_TEXTURE0, v.texcoord);
                o.screenpos = ComputeScreenPos(o.pos);

                return o;
            }
			 
			half4 frag (v2f i) : COLOR {

				#if UNITY_UV_STARTS_AT_TOP
					if (_MainTex_TexelSize.y < 0) i.uv.y = 1 - i.uv.y;
				#endif

                half4 c = tex2D(_MainTex, i.uv);
                c *= tex2D(_RampTex, allBlobs(i.uv.xy));

                return c;
                
 			}

 			ENDCG

 		}
 	}	
}
