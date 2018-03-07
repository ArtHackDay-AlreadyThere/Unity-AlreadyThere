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
        public float number;         // 番号
        public int seq;             // シーケンス
        public int blurCount;       // 残像数
        public float size;          // サイズ
        public float hashFloat;     // ハッシュ
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

    #region public member
    public int maxBlockNum = 100;   // ブロック最大数

    public Material material;


    #endregion

    #region private member
    List<BlockShapeData> blockShapeList = null;

    ShapeDrawData[] shapeDataArray = null;

    ComputeBuffer shapeDataBuffer = null;
    int shapeDataIndex = 0;

    ulong oldNumber = 0;
    #endregion


    float rand(float n)
    {
        return Mathf.Abs(Mathf.Sin(n) * 43758.5453123f) % 1f;
    }

    public void RecieveBlock(BlockData block)
    {

        Debug.Log(" hash " + block.Hash + " number " + block.Number + " oldNumber " + oldNumber);
        
        if(oldNumber == block.Number)
        {
            Debug.Log("Same Block Number!");
            return;
        }

        oldNumber = block.Number;

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
        data.shape.blurCount = 8 + Mathf.FloorToInt(rand(bottomNum) * 8);   // 8～16

        //Vector2 pos = Random.insideUnitCircle * Screen.height;
        //Vector2 pos = Random.insideUnitCircle * Camera.main.orthographicSize;
        float radius = Camera.main.orthographicSize;
        float trackNum = 5; // 周回数
        float pi2 = 2f * Mathf.PI;
        Vector2 pos;
        pos.x = Mathf.Cos(trackNum * pi2) * radius;
        pos.y = Mathf.Sin(trackNum * pi2) * radius;
        data.shape.position.x = pos.x;
        data.shape.position.y = 0;
        data.shape.position.z = pos.y;

        data.shape.seq = 0;
        data.shape.number = (block.Number * 0.0000001f);
        //data.shape.number = bottomNum;  // 仮
        //data.shape.rotation = Quaternion.identity; // 角度
        //data.shape.hashFloat = rand(bottomNum);
        Random.InitState((int)bottomNum);
        data.shape.hashFloat = Random.value;

        double size = 0;
        for(int i = 0; i < block.Transactions.Count; i++)
        {
            size += block.Transactions[i].Value;
        }
        data.shape.size = Mathf.Clamp((float)(size / block.Transactions.Count), 0.25f, 0.5f);
        //data.shape.size = 0.1f; //test

        Debug.Log("hashTop " + hashTop + " Number " + data.shape.number + " pos " + data.shape.position + " vertexCount " + data.shape.vertexCount + " blurCount " + data.shape.blurCount + " size " + data.shape.size + " hashFloat "+ data.shape.hashFloat + " TopNum " + topNum + " BottomNum " + bottomNum);

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

    void UpdateShapeData()
    {
        float trackNum = 5; // 周回数
        float pi2 = 2f * Mathf.PI;

        int count = Mathf.Min(blockShapeList.Count, maxBlockNum);
        for (int i = 0; i < count; i++)
        {
            float per = (float)(maxBlockNum - (count - i)) / maxBlockNum;
            float radius = Camera.main.orthographicSize * per;
            float rad = per * trackNum * pi2;
            Vector2 pos;
            pos.x = Mathf.Cos(rad) * radius;
            pos.y = Mathf.Sin(rad) * radius;
            blockShapeList[i].shape.position.x = pos.x;
            blockShapeList[i].shape.position.z = pos.y;
        }
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

        shapeDataBuffer.SetData(shapeDataArray);
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
        UpdateShapeData();
        UpdateDrawData();

    }

    /// <summary>
    /// レンダリング
    /// </summary>
    void OnRenderObject()
    {
        material.SetBuffer("_ShapeBuffer", shapeDataBuffer);
        material.SetInt("_ShapeBufferCount", shapeDataIndex);

        material.SetPass(0);

        Graphics.DrawProcedural(MeshTopology.Points, shapeDataIndex);
    }

    void OnDestroy()
    {
        ReleaseBuffer();
    }
}
