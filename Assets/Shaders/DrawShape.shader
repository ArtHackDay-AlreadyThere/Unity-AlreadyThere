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
		Tags { "RenderType"="Opaque" }
		LOD 100

		ZWrite Off
		Blend One One

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
			
			// 図形描画用データ
			struct ShapeDrawData
			{
				float3 position;	// 座標
				int vertexCount;    // 頂点数
				float number;		// 番号
				int seq;            // シーケンス
				int blurCount;      // 残像数
				float size;         // サイズ
				float hashFloat;	// ハッシュ
				uint id;			// 自分のID(起動時からの連番)
				float4 color;		// 色
			};

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

				//float3 angle = snoise_grad(float3(input[0].number, _Time.y, 0));
				
				float hash = hash11(input[0].hashFloat) * 344.0;
				float hash1 = hash11(input[0].hashFloat + 1392.0) * 192.0;
				float hash2 = hash11(input[0].hashFloat + 294.0) * 3543.0;
				//float hash = input[0].hashFloat * 0.05;
				//float hash1 = hash11(input[0].hashFloat + 100392.0) * 34294.0;
				//float hash2 = hash11(input[0].hashFloat + 34294.0) * 235483543.0;

				for (int j = 0; j < input[0].blurCount; j++) {
					
					float3 angle = snoise3D(float3(hash1, j * 0.01 + _Time.y * 0.1, hash2)) * PI2 * 0.5;
					//float size = snoise(float2((float)input[0].number, j * 0.01 + _Time.y * 0.1)) * input[0].size;
					float size = (abs(snoise(float2(hash, j * 0.02 + _Time.y * 0.2))) * 0.9 + 0.1)* input[0].size;

					for (int i = 0; i < count; i++) {
						float3 pos2 = float3(cos(rad * i) * size, 0, sin(rad * i) * size);

						//pos2 = rotateWithQuaternion(pos2, input[0].rotation);
						//pos2 = rotateAngleAxis(pos2, axis, _Time.y);
						//pos2 = rotateX(pos2, angle.x);
						pos2 = rotateY(pos2, angle.y);
						//pos2 = rotateZ(pos2, angle.z);

						//output.pos = pos + mul(float4(pos2, 1), billboardMatrix);
						output.pos = pos + float4(pos2, 0);

						output.pos = mul(UNITY_MATRIX_VP, output.pos);
						output.col = input[0].col;

						outStream.Append(output);
					}

					outStream.RestartStrip();
				}
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = i.col;
				return col;
			}
			ENDCG
		}
	}
}
