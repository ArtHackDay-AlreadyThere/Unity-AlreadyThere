using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour {

    #region define
    // 図形描画用データ
    public struct ShapeDrawData
    {
        public Vector3 position;    // 座標
        public int vertexCount;     // 頂点数
        public ulong number;        // 番号
        public int seq;             // シーケンス
        public int blurCount;       // 残像数
    }
    
    /// <summary>
    /// ブロックと図形データをまとめたクラス
    /// </summary>
    public class BlockShapeData
    {
        public BlockData block;
        public ShapeDrawData shape;
    }
    #endregion

    public int maxBlockNum = 100;   // ブロック最大数

    List<BlockShapeData> blockShapeList = null;

    ShapeDrawData[] shapeDataArray = null;

    ComputeBuffer shapeDataBuffer = null;
    int shapeDataIndex = 0;

    float rand(float n)
    {
        return Mathf.Abs(Mathf.Sin(n) * 43758.5453123f) % 1f;
    }

    public void RecieveBlock(BlockData block)
    {

        Debug.Log("hash " + block.Hash + " number " + block.Number);

        BlockShapeData data = new BlockShapeData();
        data.block = block;

        // 変換
        // ハッシュの先頭16文字取り出す
        string hashTopStr = block.Hash.Substring(2, 16);    // 0xを無視する為に3文字目から開始
        //ulong a = System.Convert.ToUInt64(hashTopStr, 16);
        ulong hashTop = ulong.Parse(hashTopStr, System.Globalization.NumberStyles.AllowHexSpecifier);

        uint topNum = (uint)(hashTop >> 32);
        uint bottomNum = (uint)(hashTop & 0xffffffff);
        
        data.shape.vertexCount = 3 + Mathf.FloorToInt(rand(topNum) * 6);    // 3～8角形
        data.shape.blurCount = 3 + Mathf.FloorToInt(rand(bottomNum) * 6);

        Vector2 pos = Random.insideUnitCircle * Screen.height;
        data.shape.position.x = pos.x;
        data.shape.position.y = 0;
        data.shape.position.z = pos.y;

        data.shape.seq = 0;
        data.shape.number = block.Number;

        Debug.Log("hashTop " + hashTop + " Number " + data.shape.number + " pos " + data.shape.position + " vertexCount " + data.shape.vertexCount + " blurCount " + data.shape.blurCount);

        blockShapeList.Add(data);
    }

    void Initialize()
    {
        shapeDataArray = new ShapeDrawData[maxBlockNum];
        shapeDataBuffer = new ComputeBuffer(maxBlockNum, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ShapeDrawData)));
        blockShapeList = new List<BlockShapeData>(maxBlockNum);

        //for (int i = 0; i < maxBlockNum; i++)
        //{
        //    BlockShapeData data = new BlockShapeData();
        //    blockShapeList.Add(data);
        //}
    }

    void UpdateDrawData()
    {
        shapeDataIndex = 0;

        int count = Mathf.Min(blockShapeList.Count, maxBlockNum);
        for (int i = 0; i < count; i++)
        {
            shapeDataArray[i] = blockShapeList[i].shape;
        }
        shapeDataIndex = count;


    }

    void ReleaseBuffer()
    {
        shapeDataBuffer.Release();
        shapeDataBuffer = null;
    }

	// Use this for initialization
	void Start () {
        Initialize();

    }
	
	// Update is called once per frame
	void Update () {
        UpdateDrawData();

    }

    void OnDestroy()
    {
        ReleaseBuffer();
    }
}
