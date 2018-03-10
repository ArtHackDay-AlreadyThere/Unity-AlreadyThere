using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using Utility;
#if UNITY_EDITOR
using System.IO;
#endif

/// <summary>
/// GPUParticleの更新処理
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class GPUParticleBase<T> : MonoBehaviour where T : struct {

    #region define
    // ComputeShaderのスレッド数
    protected const int THREAD_NUM_X = 32;
    #endregion

    #region public
    public int particleMax = 1024;
    public int emitMax = 8;

    public ComputeShader cs;
    //public Material material;
    #endregion

    #region private
    protected ComputeBuffer particleBuffer;         // パーティクル構造体のバッファ
    protected ComputeBuffer particleActiveBuffer;   // 使用中のパーティクルのインデックスのリスト
    protected ComputeBuffer particlePoolBuffer;     // 未使用中のパーティクルのインデックスのリスト
    protected ComputeBuffer particleActiveCountBuffer;  // particleActiveBuffer内の個数バッファ
    protected ComputeBuffer particlePoolCountBuffer;    // particlePoolBuffer内の個数バッファ
    protected int particleNum = 0;
    protected int emitNum = 0;
    protected int[] particleCounts = { 1, 1, 0, 0 };    // [0]インスタンスあたりの頂点数 [1]インスタンス数 [2]開始する頂点位置 [3]開始するインスタンス

    protected int initKernel = -1;
    protected int emitKernel = -1;
    protected int updateKernel = -1;

    //protected int particleActiveNum = 0;
    protected int particlePoolNum = 0;

    protected int cspropid_Particles;
    protected int cspropid_DeadList;
    protected int cspropid_ActiveList;
    protected int cspropid_EmitNum;
    protected int cspropid_ParticlePool;

	protected bool isInitialized = false;

    #region debug
    protected T[] debugData;
    protected int[] debugPoolData;
    protected ComputeBuffer debugCountBuffer;
    #endregion

    #endregion

    #region virtual
    public virtual int GetParticleNum() { return particleNum; }

    protected int[] debugounts = { 0, 0, 0, 0 };

    /// <summary>
    /// アクティブなパーティクルの数を取得（デバッグ機能）
    /// </summary>
    /// <returns></returns>
    public virtual int GetActiveParticleNum() {
        particleActiveCountBuffer.GetData(debugounts);
        return debugounts[1];
    }

    public virtual ComputeBuffer GetParticleBuffer() { return particleBuffer; }
    public virtual ComputeBuffer GetActiveParticleBuffer() { return particleActiveBuffer; }

    public virtual ComputeBuffer GetParticleCountBuffer() { return particleActiveCountBuffer; }
    //public virtual ComputeBuffer GetActiveCountBuffer() { return particleActiveCountBuffer; }

    public virtual void SetVertexCount(int vertexNum)
    {
        particleCounts[0] = vertexNum;
    }

    /// <summary>
    /// 初期化
    /// </summary>
    protected virtual void Initialize()
    {
        particleNum = (particleMax / THREAD_NUM_X) * THREAD_NUM_X;
        emitNum = (emitMax / THREAD_NUM_X) * THREAD_NUM_X;
        //Debug.Log("particleNum " + particleNum + " emitNum " + emitNum + " THREAD_NUM_X " + THREAD_NUM_X);

        particleBuffer = new ComputeBuffer(particleNum, Marshal.SizeOf(typeof(T)), ComputeBufferType.Default);
        particleActiveBuffer = new ComputeBuffer(particleNum, Marshal.SizeOf(typeof(int)), ComputeBufferType.Append);
        particleActiveBuffer.SetCounterValue(0);
        particlePoolBuffer = new ComputeBuffer(particleNum, Marshal.SizeOf(typeof(int)), ComputeBufferType.Append);
        particlePoolBuffer.SetCounterValue(0);
        particleActiveCountBuffer = new ComputeBuffer(4, Marshal.SizeOf(typeof(int)), ComputeBufferType.IndirectArguments);
        particlePoolCountBuffer = new ComputeBuffer(4, Marshal.SizeOf(typeof(int)), ComputeBufferType.IndirectArguments);
        particlePoolCountBuffer.SetData(particleCounts);

        int[] counts = { 0, 1, 0, 0 };
        particleActiveCountBuffer.SetData(counts);

        initKernel = cs.FindKernel("Init");
        emitKernel = cs.FindKernel("Emit");
        updateKernel = cs.FindKernel("Update");

        cspropid_Particles = ShaderDefines.GetBufferPropertyID(ShaderDefines.BufferID._Particles);
        cspropid_DeadList = ShaderDefines.GetBufferPropertyID(ShaderDefines.BufferID._DeadList);
        cspropid_ActiveList = ShaderDefines.GetBufferPropertyID(ShaderDefines.BufferID._ActiveList);
        cspropid_ParticlePool = ShaderDefines.GetBufferPropertyID(ShaderDefines.BufferID._ParticlePool);
        cspropid_EmitNum = ShaderDefines.GetIntPropertyID(ShaderDefines.IntID._EmitNum);

        //Debug.Log("initKernel " + initKernel + " emitKernel " + emitKernel + " updateKernel " + updateKernel);

        cs.SetBuffer(initKernel, cspropid_Particles, particleBuffer);
        cs.SetBuffer(initKernel, cspropid_DeadList, particlePoolBuffer);
        cs.Dispatch(initKernel, particleNum / THREAD_NUM_X, 1, 1);

		isInitialized = true;

#if UNITY_EDITOR
        debugData = new T[particleNum];
        debugPoolData = new int[particleNum];
        debugCountBuffer = new ComputeBuffer(4, Marshal.SizeOf(typeof(int)), ComputeBufferType.IndirectArguments);
#endif
    }

    /// <summary>
    /// パーティクルの更新
    /// </summary>
    protected abstract void UpdateParticle();

    /// <summary>
    /// パーティクルの発生
    /// THREAD_NUM_X分発生
    /// </summary>
    protected virtual void EmitParticle() { }

    /// <summary>
    /// ComputeBufferの解放
    /// </summary>
    protected virtual void ReleaseBuffer() {
        if(particleActiveBuffer != null)
        {
            particleActiveBuffer.Release();
        }

        if (particlePoolBuffer != null)
        {
            particlePoolBuffer.Release();
        }

        if (particleBuffer != null)
        {
            particleBuffer.Release();
        }

        if(particlePoolCountBuffer != null)
        {
            particlePoolCountBuffer.Release();
        }

        if(particleActiveCountBuffer != null)
        {
            particleActiveCountBuffer.Release();
        }
#if UNITY_EDITOR
        if(debugCountBuffer != null)
        {
            debugCountBuffer.Release();
        }
#endif
    }

    // Use this for initialization
    protected virtual void Awake()
    {
        ReleaseBuffer();
        Initialize();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        UpdateParticle();
    }

    #endregion

    void OnDestroy()
    {
        ReleaseBuffer();
    }

    //protected virtual void DumpActive

#if UNITY_EDITOR
    protected virtual void WriteData(StreamWriter sw, int index, ref T data) { }

    protected virtual void DumpParticleData(ComputeBuffer cb, string name)
    {
        cb.GetData(debugData);
        StreamWriter sw;
        FileInfo fi;
        string date = System.DateTime.Now.ToString("yyyyMMddHHmmss");
        fi = new FileInfo(Application.dataPath + "/../" + name + date + ".csv");
        sw = fi.AppendText();
        for (int i = 0; i < debugData.Length; i++)
        {
            //Debug.Log("[" + i + "] GridHash " + gridHashDataArray[i] + " index " + sortedIndexDataArray[i]);
            //sw.WriteLine("" + i + "," + debugData[i].position + "," + debugData[i].velocity + "," + debugData[i].rotation + "," + debugData[i].animeTime + "," + debugData[i].speed + "," + debugData[i].offsetLimit);
            WriteData(sw, i, ref debugData[i]);
        }
        sw.Flush();
        sw.Close();
        Debug.Log("Dump Data " + fi.FullName);
    }

    protected virtual void DumpPoolData(ComputeBuffer cb, string name)
    {
        cb.GetData(debugPoolData);

        ComputeBuffer.CopyCount(cb, debugCountBuffer, 0);
        debugCountBuffer.GetData(debugounts);
        Debug.Log(name + " Pool Num " + debugounts[0]);

        StreamWriter sw;
        FileInfo fi;
        string date = System.DateTime.Now.ToString("yyyyMMddHHmmss");
        fi = new FileInfo(Application.dataPath + "/../" + name + date + ".csv");
        sw = fi.AppendText();
        for (int i = 0; i < debugPoolData.Length; i++)
        {
            sw.WriteLine("" + i + "," + debugPoolData[i]);
        }
        sw.Flush();
        sw.Close();
        Debug.Log("Dump Data " + fi.FullName);
    }
#endif
}
