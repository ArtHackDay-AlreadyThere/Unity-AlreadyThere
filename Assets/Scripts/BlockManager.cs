﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PrefsGUI;

public class BlockManager : MonoBehaviour {

    #region define
    // 図形描画用データ
    public struct ShapeDrawData
    {
        public Vector3 position;    // 座標
        public int vertexCount;     // 頂点数
        public float number;        // 番号
        public int seq;             // シーケンス
        public int blurCount;       // 残像数
        public float size;          // サイズ
        public float hashFloat;     // ハッシュ
        public uint id;             // ID(最初からの連番)
        public Color color;         // 色
        public float fadeDuration;  // 消える時の時間
        public bool isSpecial;      // 特別な送金先か？

    }

    /// <summary>
    /// ブロックと図形データをまとめたクラス
    /// </summary>
    public class BlockShapeData
    {
        public BlockData block;
        public ShapeDrawData shape;

        // 移動関係のパラメータ
        public Vector3 velocity;
        public float mass;
    }
    #endregion

    #region public member
    public PrefsFloat fadeTime = new PrefsFloat("fadeTime", 5);
    public PrefsInt maxBlockNum = new PrefsInt("maxBlockNum", 200);
    public PrefsInt minBlockNum = new PrefsInt("minBlockNum", 10);

    public PrefsFloat printInterval = new PrefsFloat("printInterval", 300);
    public PrefsInt printNum = new PrefsInt("printNum", 100);

    public PrefsBool printEnable = new PrefsBool("printEnable", true);
    public PrefsInt printHashSize = new PrefsInt("printHashSize", 512);

    public Material shapeMaterial;  // 図形描画用マテリアル
    public Material lineMaterial;   // ライン描画用マテリアル

    /// <summary>
    /// 特別な送金先
    /// </summary>
    public string specialTo = "";

    // バネの強度
    public float stiffness = 0.1f;

    // 摩擦
    public float damping = 0.9f;

    // 質量の範囲
    public Vector2 massRange = new Vector2(1f, 1.5f);

    // バネの長さ
    public float springLength = 1;

    public float noiseSpeed = 0.1f;
    public float noisePower = 1;
    public float noiseScale = 0.1f;

    [Range(0,1)]
    public float _HSVSat;
    [Range(0, 1)]
    public float _HSVVal;
    public float _ColSpeed;
    public float _ColNumberPow;

    public TransactionParticle particle;
    #endregion

    #region private member
    List<BlockShapeData> blockShapeList = null;

    ShapeDrawData[] shapeDataArray = null;

    ComputeBuffer shapeDataBuffer = null;
    int shapeDataIndex = 0;

    ulong oldNumber = 0;

    float printDuration = 0;

    static uint idNumber = 0;   // 起動時からの連番

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
        Vector2 pos;
        if (blockShapeList.Count < 2)
        {
            // 最初
            //float radius = Camera.main.orthographicSize;
            //float trackNum = 5; // 周回数
            //float pi2 = 2f * Mathf.PI;
            //pos.x = Mathf.Cos(trackNum * pi2) * radius;
            //pos.y = Mathf.Sin(trackNum * pi2) * radius;
            pos = Random.insideUnitCircle * 0.1f;
        }
        else
        {
            // 3個め以降は一つ前から発生
            Vector3 pos3d = blockShapeList[blockShapeList.Count - 1].shape.position;
            Vector3 diff = pos3d - blockShapeList[blockShapeList.Count - 2].shape.position;
            float len = diff.magnitude;
            Vector3 norm = Vector3.zero;
            if (len > 0)
            {
                norm = diff.normalized;
            }
                
            //pos.x = pos3d.x + norm.x * 0.1f;
            //pos.y = pos3d.z + norm.z * 0.1f;
            Vector2 randPos = Random.insideUnitCircle * 0.01f;
            pos.x = pos3d.x + norm.x * springLength * 0.125f + randPos.x;
            pos.y = pos3d.z + norm.y * springLength * 0.125f + randPos.y;
            //float rad = Mathf.Atan2(norm.y, norm.x) * Mathf.PI * 0.5f;
            //pos.x = pos3d.x + Mathf.Cos(rad) * 1f;
            //pos.y = pos3d.z + Mathf.Sin(rad) * 1f;
        }

        data.shape.id = idNumber++;

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

        data.velocity = Vector3.zero;
        data.mass = Random.Range(massRange.x, massRange.y);

        bool isSpecial = false;
        double size = 0;
        for(int i = 0; i < block.Transactions.Count; i++)
        {
            size += block.Transactions[i].Value;
            particle.EmitParticle(data.shape.id, data.shape.position, (float)block.Transactions[i].Value);
            if(block.Transactions[i].To == specialTo)
            {
                isSpecial = true;
            }
        }
        //isSpecial = true;   // test
        data.shape.size = Mathf.Clamp((float)(size / block.Transactions.Count), 0.25f, 0.5f);
        data.shape.isSpecial = isSpecial;
        if (isSpecial)
        {
            data.shape.size += 0.5f;   // 大きくする
            data.shape.vertexCount = 16;    // 16芒星
            data.shape.blurCount = 6;
        }

        Debug.Log("hashTop " + hashTop + " Number " + data.shape.number + " pos " + data.shape.position + " vertexCount " + data.shape.vertexCount + " blurCount " + data.shape.blurCount + " size " + data.shape.size + " hashFloat "+ data.shape.hashFloat + " TopNum " + topNum + " BottomNum " + bottomNum);

        blockShapeList.Add(data);
        Debug.Log("blockShapeList.Count " + blockShapeList.Count);

        if(blockShapeList.Count >= maxBlockNum)
        {
            // 最大数越えたら印刷して削除
            RemoveBlock();
        }
    }

    void Initialize()
    {
        shapeDataArray = new ShapeDrawData[maxBlockNum];
        shapeDataBuffer = new ComputeBuffer(maxBlockNum, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ShapeDrawData)));
        blockShapeList = new List<BlockShapeData>(maxBlockNum);

        printDuration = printInterval;

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

        float dt = Time.deltaTime;

        int count = Mathf.Min(blockShapeList.Count, maxBlockNum);
        for (int i = 0; i < count; i++)
        {
            // 目標位置
            Vector3 targetPos;

            if (i == 0)
            {
                // 先頭
                //float per = (float)(maxBlockNum - (count - i)) / maxBlockNum;
                //float radius = Camera.main.orthographicSize * per;
                //float rad = per * trackNum * pi2;
                //targetPos.x = Mathf.Cos(rad) * radius;
                //targetPos.y = 0;
                //targetPos.z = Mathf.Sin(rad) * radius;

                //Vector3 pos = blockShapeList[i].shape.position;
                //float tx = Mathf.PerlinNoise(pos.x * noiseScale + Time.time * noiseSpeed, pos.y * noiseScale - Time.time * noiseSpeed) * 2f - 1f;
                //float ty = Mathf.PerlinNoise(pos.y * noiseScale + Time.time * noiseSpeed + 10000, pos.x * noiseScale - Time.time * noiseSpeed + 30000) * 2f - 1f;
                //targetPos.x = pos.x + tx * noisePower;
                //targetPos.z = pos.z + ty * noisePower;

                targetPos.x = 0;
                targetPos.y = 0;
                targetPos.z = 0;
            }
            else
            {
                Vector3 pos = blockShapeList[i - 1].shape.position;
                float tx = Mathf.PerlinNoise(pos.x * noiseScale + Time.time * noiseSpeed, pos.y * noiseScale - Time.time * noiseSpeed) * 2f - 1f;
                float ty = Mathf.PerlinNoise(pos.y * noiseScale + Time.time * noiseSpeed + 10000, pos.x * noiseScale - Time.time * noiseSpeed + 30000) * 2f - 1f;

                float scale = (blockShapeList[i].shape.size + blockShapeList[i - 1].shape.size) * 0.5f;
                targetPos = pos + (blockShapeList[i].shape.position - pos).normalized * scale * springLength;   // 一定の長さを保つ位置を求める
                targetPos.x += ty * 0.5f;
                targetPos.z += ty * 0.5f;

            }

            // フックの法則
            //Vector3 force = (targetPos - blockShapeList[i].shape.position) * stiffness / blockShapeList[i].mass;           // フックの法則 f = -kx
            Vector3 force = (targetPos - blockShapeList[i].shape.position) * stiffness;
            //velocity = (blockShapeList[i].velocity + force) * damping;    // 速度計算

            // 他のブロックとぶつからないようにする
            Vector3 velocity = Vector3.zero;
            for (int j = 0; j < count; j++)
            {
                if (j == i) continue;
                Vector3 diff = blockShapeList[i].shape.position - blockShapeList[j].shape.position;
                float length = diff.magnitude;
                float scale = (blockShapeList[i].shape.size + blockShapeList[j].shape.size) * 0.5f * springLength;
                if (length < scale)
                {
                    velocity += diff.normalized * (scale - length);
                }
            }

            blockShapeList[i].velocity = (blockShapeList[i].velocity + velocity + force) * damping;
            //blockShapeList[i].shape.position += blockShapeList[i].velocity * dt;
            //blockShapeList[i].shape.position.x = pos.x;
            //blockShapeList[i].shape.position.z = pos.y;
        }

        float time = Time.time;

        // 座標行進
        float screenRange = Camera.main.orthographicSize;
        for (int i = (count - 1); i >= 0; i--)
        {
            blockShapeList[i].shape.position += blockShapeList[i].velocity * dt;

            // 色変更
            float colorSpeed = blockShapeList[i].shape.isSpecial ? 10 : 1;
            blockShapeList[i].shape.color = Color.HSVToRGB((blockShapeList[i].shape.number * _ColNumberPow + time * _ColSpeed * colorSpeed) % 1f, _HSVSat, _HSVVal);

            // 消滅チェック
            if (blockShapeList[i].shape.seq > 0)
            {
                if (blockShapeList[i].shape.fadeDuration <= 0f)
                {
                    blockShapeList.RemoveAt(i);
                    continue;
                }

                blockShapeList[i].shape.fadeDuration -= dt;
                float d = Mathf.Clamp01(blockShapeList[i].shape.fadeDuration / fadeTime);
                blockShapeList[i].shape.color.a = d;
                //Debug.Log("[" + i + "] " + d + " " + blockShapeList[i].shape.fadeDuration);

            }

            // はみ出しチェック
            float scr = screenRange - blockShapeList[i].shape.size;

            if (blockShapeList[i].shape.position.x < -scr)
            {
                blockShapeList[i].shape.position.x = -scr;
            }
            if (blockShapeList[i].shape.position.x > scr)
            {
                blockShapeList[i].shape.position.x = scr;
            }

            if (blockShapeList[i].shape.position.z < -scr)
            {
                blockShapeList[i].shape.position.z = -scr;
            }
            if (blockShapeList[i].shape.position.z > scr)
            {
                blockShapeList[i].shape.position.z = scr;
            }


        }

        // 印刷チェック
        printDuration -= dt;

        if(printDuration <= 0f)
        {
            RemoveBlock();
        }
    }

    void RemoveBlock()
    {
        int printCount = printNum;

        if((blockShapeList.Count - printCount) < minBlockNum)
        {
            printCount = Mathf.Max(blockShapeList.Count - minBlockNum, 0);
        }

        Debug.Log("printCount " + printCount);

        if (printCount > 0)
        {
            // 印刷
            PrintBlocks(0, printCount);
        }

        // 削除
        for (int i = 0; i < printCount; i++)
        {
            blockShapeList[i].shape.seq = 1;    // 消えるフェーズへ
            blockShapeList[i].shape.fadeDuration = fadeTime + i * 0.5f;
        }

        printDuration = Mathf.Max(printInterval, fadeTime + printCount * 0.5f);
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
        if (Input.GetKeyDown(KeyCode.R))
        {
            RemoveBlock();
        }

        UpdateShapeData();
        UpdateDrawData();
        if (particle != null)
        {
            particle.SetShapeBuffer(shapeDataBuffer, shapeDataIndex);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Exit");
            Application.Quit();
        }
    }

    /// <summary>
    /// レンダリング
    /// </summary>
    void OnRenderObject()
    {
        // 図形の描画
        shapeMaterial.SetBuffer("_ShapeBuffer", shapeDataBuffer);
        shapeMaterial.SetInt("_ShapeBufferCount", shapeDataIndex);
        shapeMaterial.SetFloat("_FadeTime", fadeTime);

        shapeMaterial.SetPass(0);

        Graphics.DrawProcedural(MeshTopology.Points, shapeDataIndex);

        // ラインの描画
        lineMaterial.SetBuffer("_ShapeBuffer", shapeDataBuffer);
        lineMaterial.SetInt("_ShapeBufferCount", shapeDataIndex);

        lineMaterial.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.LineStrip, shapeDataIndex);
    }

    void OnDestroy()
    {
        ReleaseBuffer();
    }

    void PrintBlocks(int startIndex, int count) {
        if (!printEnable)
            return;

        string print = "";
        for (int i = startIndex; i < startIndex + count; i++)
        {
            foreach (var transaction in blockShapeList[i].block.Transactions)
            {
                if (transaction.From.Length > 0 && transaction.To.Length > 0)
                {
                    print += transaction.From.Substring(2) + transaction.To.Substring(2);
                }
            }
        }

        if (print.Length == 0)
        {
            return;
        }

        print = print.Substring(0, printHashSize);


        StreamWriter writer = null;
        try
        {
            string filename = Path.GetTempFileName();

            writer = new System.IO.StreamWriter(filename, false);
            writer.Write(print);

            writer.Close();

            System.Diagnostics.Process.Start(@"C:\Program Files (x86)\TeraPad\TeraPad.exe", @"/p " + filename);
        }catch(System.Exception e)
        {
            Debug.LogError(e.Message);
        }
    }


    public void DebugMenu()
    {
        fadeTime.OnGUI();

        maxBlockNum.OnGUI();
        minBlockNum.OnGUI();

        if (printInterval.OnGUI())
        {
            printDuration = printInterval;
        }
        printNum.OnGUI();

        printEnable.OnGUI();
        printHashSize.OnGUI();
    }

}
