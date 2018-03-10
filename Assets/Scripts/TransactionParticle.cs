using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using System.IO;
#endif

public struct TransactionParticleData
{
    public bool isActive;       // 有効無効
    public int seq;             // シーケンス
    public Vector3 position;    // 座標
    public Vector3 velocity;    // 加速度
    public float scale;         // 大きさ
    public uint parentID;       // 親図形のID
    public Color color;			// 色
}

public class TransactionParticle : GPUParticleBase<TransactionParticleData> {

    // パーティクルのサイズの範囲
    public Vector2 particleSizeRange = new Vector2(0.01f, 1f);

    // パーティクルの初期座標のランダムの範囲
    public float particlePositionRandomSize = 0.1f;

    // パーティクルの質量の範囲
    public Vector2 massRange = new Vector2(0.5f, 1);

    protected ComputeBuffer shapeBuffer;    // 親図形のComputeBuffer
    protected int shapeCount = 0;

    protected TransactionParticleData[] emitList;
    protected int emitIndex = 0;
    protected ComputeBuffer emitBuffer;
    public void SetShapeBuffer(ComputeBuffer sb, int count)
    {
        shapeBuffer = sb;
        shapeCount = count;
    }

    public void EmitParticle(uint parentID, Vector3 pos, float value)
    {

        if(emitIndex < emitNum)
        {
            int i = emitIndex;
            emitList[i].isActive = true;

            Vector2 ra = Random.insideUnitCircle * particlePositionRandomSize;
            emitList[i].position = pos;
            emitList[i].position.x += ra.x;
            emitList[i].position.y += ra.y;

            emitList[i].seq = 0;
            emitList[i].velocity = Vector3.zero;
            emitList[i].scale = Mathf.Clamp(value, particleSizeRange.x, particleSizeRange.y);
            emitList[i].parentID = parentID;

            emitIndex++;
        }
    }

    protected override void Initialize()
    {
        base.Initialize();

        emitList = new TransactionParticleData[emitNum];
        emitBuffer = new ComputeBuffer(emitNum, System.Runtime.InteropServices.Marshal.SizeOf(typeof(TransactionParticleData)));
        emitIndex = 0;

        emitKernel = cs.FindKernel("Emit");
    }

    protected override void EmitParticle()
    {
        particlePoolCountBuffer.SetData(particleCounts);
        ComputeBuffer.CopyCount(particlePoolBuffer, particlePoolCountBuffer, 0);
        particlePoolCountBuffer.GetData(particleCounts);
        Debug.Log("EmitParticle Pool Num " + particleCounts[0]);

        emitBuffer.SetData(emitList);
        cs.SetInt("_EmitIndex", emitIndex);

        cs.SetBuffer(emitKernel, "_EmitBuffer", emitBuffer);

        cs.SetBuffer(emitKernel, cspropid_ParticlePool, particlePoolBuffer);
        cs.SetBuffer(emitKernel, cspropid_Particles, particleBuffer);

        cs.SetBuffer(emitKernel, "_PoolCount", particlePoolCountBuffer);

        //int threadGroupNumX = emitNum / THREAD_NUM_X;
        int threadGroupNumX = Mathf.CeilToInt(emitNum / (float)THREAD_NUM_X);
        Debug.Log("threadGroupNumX " + threadGroupNumX + " particleCounts " + particleCounts[0] + " emitIndex " + emitIndex);

        cs.Dispatch(emitKernel, threadGroupNumX, 1, 1);   // emitNumの数だけ発生

        emitIndex = 0;
    }

    protected override void UpdateParticle()
    {
        particleActiveBuffer.SetCounterValue(0);

        cs.SetFloat("_Time", Time.time);

        cs.SetFloat("_DT", Time.deltaTime);

        cs.SetVector("_MassRange", massRange);

        cs.SetBuffer(updateKernel, "_Particles", particleBuffer);
        cs.SetBuffer(updateKernel, "_DeadList", particlePoolBuffer);
        cs.SetBuffer(updateKernel, "_ActiveList", particleActiveBuffer);

        cs.SetBuffer(updateKernel, "_ShapeBuffer", shapeBuffer);
        cs.SetInt("_ShapeCount", shapeCount);

        cs.Dispatch(updateKernel, particleNum / THREAD_NUM_X, 1, 1);

        particleActiveCountBuffer.SetData(particleCounts);
        ComputeBuffer.CopyCount(particleActiveBuffer, particleActiveCountBuffer, 0);

    }

    protected override void Update()
    {
        if(emitIndex > 0)
        {
            EmitParticle();
        }

        UpdateParticle();

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.M))
        {
            DumpParticleData(emitBuffer, "emitBuffer");
            DumpParticleData(particleBuffer, "particleBuffer");
            DumpPoolData(particleActiveBuffer, "activeBuffer");
            DumpPoolData(particlePoolBuffer, "poolBuffer");

            //particleActiveCountBuffer.GetData(debugounts);

            //Debug.Log("GetActiveParticleNum " + debugounts[0]);

        }
#endif
    }

    protected override void ReleaseBuffer()
    {
        base.ReleaseBuffer();

        if (emitBuffer != null)
        {
            emitBuffer.Release();
        }
        
    }

#if UNITY_EDITOR
    protected override void WriteData(StreamWriter sw, int index, ref TransactionParticleData data)
    {
        sw.WriteLine("" + index + "," + debugData[index].isActive + "," + debugData[index].position + "," + debugData[index].velocity + "," + debugData[index].scale + "," + debugData[index].parentID);
    }
#endif
}
