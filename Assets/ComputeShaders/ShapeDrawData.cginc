#ifndef __SHAPE_DRAW_DATA__
#define __SHAPE_DRAW_DATA__

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
	float fadeDuration;  // 消える時の時間
	bool isSpecial;      // 特別な送金先か？
};
#endif
