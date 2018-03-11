Shader "Custom/Draw Shape"
{
	Properties
	{
		_HSVSat("HSV Sat", Range(0.0,1)) = 0.25				// 彩度
		_HSVVal("HSV Value", Range(0.0,1)) = 1				// 明度
		_ColSpeed("HSV Change Speed", Range(0.0,2)) = 1		// 色が変わる速度
		_ColNumberPow("HSV Number Power", Float) = 100000	// ブロックごとの色のズレ
	}

	SubShader
	{
		//Tags { "RenderType"="Opaque" }
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

		//LOD 100

		ZWrite Off
		//Blend One One
		//Blend OneMinusDstColor One
		Blend SrcAlpha One

		Pass
		{
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			#include "Libs/Utils2D.cginc"
			#include "Libs/Quaternion.cginc"
			#include "Libs/Noise.cginc"
			#include "Libs/Transform.cginc"
			#include "Libs/Color.cginc"
			#include "Assets/ComputeShaders/ShapeDrawData.cginc"

			// 頂点シェーダからの出力
			struct VSOut {
				float4 pos : SV_POSITION;
				float4 col : COLOR;
				int vertexCount : TEXCOORD0;    // 頂点数
				uint number		: TEXCOORD1;	// 番号
				int blurCount	: TEXCOORD2;    // 残像数
				float size		: TEXCOORD3;    // サイズ
				//float4 rotation : TEXCOORD4;    // 角度
				float hashFloat : TEXCOORD5;	// ハッシュ
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 col : COLOR;
			};
			
			StructuredBuffer<ShapeDrawData> _ShapeBuffer;
			int _ShapeBufferCount;

			float _HSVSat;
			float _HSVVal;
			float _ColSpeed;
			float _ColNumberPow;
			float _FadeTime;

			VSOut vert (uint id : SV_VertexID)
			{
				VSOut output;
				output.pos = float4(_ShapeBuffer[id].position, 1);
				output.col = _ShapeBuffer[id].color;
				//output.col = float4(HSVtoRGB(float3(_ShapeBuffer[id].number * _ColNumberPow + _Time.y * _ColSpeed, _HSVSat, _HSVVal)), 1);
				output.vertexCount = _ShapeBuffer[id].vertexCount;  // 頂点数
				output.number = _ShapeBuffer[id].number;			// 番号
				output.blurCount = _ShapeBuffer[id].blurCount;      // 残像数
				output.size = _ShapeBuffer[id].size;				// サイズ
				if (_ShapeBuffer[id].seq == 1) {
					float d = 1.0 - saturate(_ShapeBuffer[id].fadeDuration / _FadeTime);
					output.size += d * 2;  // 消える時の時間
				}
				//output.rotation = _ShapeBuffer[id].rotation;		// 角度
				output.hashFloat = _ShapeBuffer[id].hashFloat;
				return output;
			}
			
			// ジオメトリシェーダ
			[maxvertexcount(128)]
			void geom(point VSOut input[1], inout LineStream<v2f> outStream)
			{
				v2f output;

				float4 pos = input[0].pos;
				//float4 pos = float4(0,0,0,1);	// test

				// ビルボード用の行列
				float4x4 billboardMatrix = UNITY_MATRIX_V;
				billboardMatrix._m03 =
					billboardMatrix._m13 =
					billboardMatrix._m23 =
					billboardMatrix._m33 = 0;

				float rad = PI2 / input[0].vertexCount;
				int count = input[0].vertexCount + 1;
				bool isSpecial = input[0].vertexCount == 16 ? true : false;

				//float3 angle = snoise_grad(float3(input[0].number, _Time.y, 0));
				
				float hash = hash11(input[0].hashFloat) * 344.0;
				float hash1 = hash11(input[0].hashFloat + 1392.0) * 192.0;
				float hash2 = hash11(input[0].hashFloat + 294.0) * 3543.0;

				for (int j = 0; j < input[0].blurCount; j++) {
					
					float3 angle = snoise3D(float3(hash1, j * 0.01 + _Time.y * 0.1, hash2)) * PI2 * 0.5;
					//float size = snoise(float2((float)input[0].number, j * 0.01 + _Time.y * 0.1)) * input[0].size;
					float size = (abs(snoise(float2(hash, j * 0.02 + _Time.y * 0.2))) * 0.9 + 0.1)* input[0].size;
					for (int i = 0; i < count; i++) {
						float size2 = (isSpecial && (i % 2 == 0)) ? size * 0.75 : size;

						float3 pos2 = float3(cos(rad * i) * size2, 0, sin(rad * i) * size2);

						pos2 = rotateY(pos2, angle.y);

						//output.pos = pos + mul(float4(pos2, 1), billboardMatrix);
						output.pos = pos + float4(pos2, 0);

						output.pos = mul(UNITY_MATRIX_VP, output.pos);
						output.col = input[0].col;

						outStream.Append(output);
					}

					outStream.RestartStrip();
				}
			}

			float4 frag (v2f i) : SV_Target
			{
				// sample the texture
				float4 col = i.col;
				//fixed4 col = fixed4(1,1,1,0);

				return col;
			}
			ENDCG
		}
	}
}
