﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Runtime.InteropServices;
using System.IO;

public abstract class GPUParticleCullingRendererBase <T> : GPUParticleRendererBase<T> where T : struct {

    #region define
    public class CullingData
    {
        public ComputeBuffer inViewsAppendBuffer;
        public ComputeBuffer inViewsCountBuffer;
        //public int inViewsNum;

        public int[] inViewsCounts = { 0, 1, 0, 0 };    // [0]インスタンスあたりの頂点数 [1]インスタンス数 [2]開始する頂点位置 [3]開始するインスタンス

        public CullingData(int particleNum)
        {
            inViewsAppendBuffer = new ComputeBuffer(particleNum, Marshal.SizeOf(typeof(int)), ComputeBufferType.Append);
            inViewsAppendBuffer.SetCounterValue(0);
            inViewsCountBuffer = new ComputeBuffer(4, Marshal.SizeOf(typeof(int)), ComputeBufferType.IndirectArguments);
            //inViewsCounts = new int[] { 0, 1, 0, 0 };
            inViewsCountBuffer.SetData(inViewsCounts);
        }

        /// <summary>
        /// 頂点数セット
        /// </summary>
        /// <param name="num"></param>
        public void SetVertexCount(int num)
        {
            inViewsCounts[0] = num;
        }

        public void Release()
        {
            if(inViewsAppendBuffer != null)
            {
                inViewsAppendBuffer.Release();
                inViewsAppendBuffer = null;
            }
            if(inViewsCountBuffer != null)
            {
                inViewsCountBuffer.Release();
                inViewsCountBuffer = null;
            }
        }

        private int[] debugCount = { 0, 0, 0, 0 };
        /// <summary>
        /// 視界内のパーティクルの数を取得（デバッグ機能）
        /// </summary>
        /// <returns></returns>
        public int GetInViewNum()
        {
            inViewsCountBuffer.GetData(debugCount);
            return debugCount[1];
        }

        const int NUM_THREAD_X = 32;

        private Plane[] _planes = new Plane[4];
        private float[] _normalsFloat = new float[12];  // 4x3
        Vector3 temp;

        private void CalculateFrustumPlanes(Matrix4x4 mat, ref Plane[] planes)
        {
            // left
            temp.x = mat.m30 + mat.m00;
            temp.y = mat.m31 + mat.m01;
            temp.z = mat.m32 + mat.m02;
            planes[0].normal = temp;
            //planes[0].distance = mat.m33 + mat.m03;

            // right
            temp.x = mat.m30 - mat.m00;
            temp.y = mat.m31 - mat.m01;
            temp.z = mat.m32 - mat.m02;
            planes[1].normal = temp;
            //planes[1].distance = mat.m33 - mat.m03;

            // bottom
            temp.x = mat.m30 + mat.m10;
            temp.y = mat.m31 + mat.m11;
            temp.z = mat.m32 + mat.m12;
            planes[2].normal = temp;
            //planes[2].distance = mat.m33 + mat.m13;

            // top
            temp.x = mat.m30 - mat.m10;
            temp.y = mat.m31 - mat.m11;
            temp.z = mat.m32 - mat.m12;
            planes[3].normal = temp;
            //planes[3].normal = new Vector3(mat.m30 - mat.m10, mat.m31 - mat.m11, mat.m32 - mat.m12);
            //planes[3].distance = mat.m33 - mat.m13;

            //// near
            //planes[4].normal = new Vector3(mat.m30 + mat.m20, mat.m31 + mat.m21, mat.m32 + mat.m22);
            //planes[4].distance = mat.m33 + mat.m23;

            //// far
            //planes[5].normal = new Vector3(mat.m30 - mat.m20, mat.m31 - mat.m21, mat.m32 - mat.m22);
            //planes[5].distance = mat.m33 - mat.m23;

            // normalize
            for (uint i = 0; i < planes.Length; i++)
            {
                float length = planes[i].normal.magnitude;
                temp = planes[i].normal;
                temp.x /= length;
                temp.y /= length;
                temp.z /= length;
                planes[i].normal = temp;
                //planes[i].normal /= length;
                //planes[i].distance /= length;
            }
        }

        public void Update(ComputeShader cs, Camera camera, int particleNum, ComputeBuffer particleBuffer, ComputeBuffer activeList)
        {
            int kernel = cs.FindKernel("CheckCameraCulling");

            CalculateFrustumPlanes(camera.projectionMatrix * camera.worldToCameraMatrix, ref _planes);
            for (int i = 0; i < 4; i++)
            {
                //Debug.DrawRay(camera.transform.position, _planes[i].normal * 10f, Color.yellow);
                _normalsFloat[i + 0] = _planes[i].normal.x;
                _normalsFloat[i + 4] = _planes[i].normal.y;
                _normalsFloat[i + 8] = _planes[i].normal.z;
            }
            inViewsAppendBuffer.SetCounterValue(0);

            var cPos = camera.transform.position;
            cs.SetFloats("_CameraPos", cPos.x, cPos.y, cPos.z);

            cs.SetInt("_ParticleNum", particleNum);
            cs.SetFloats("_CameraFrustumNormals", _normalsFloat);
            cs.SetBuffer(kernel, "_InViewAppend", inViewsAppendBuffer);
            cs.SetBuffer(kernel, "_ParticleBuffer", particleBuffer);
            cs.SetBuffer(kernel, "_ParticleActiveList", activeList);
            cs.Dispatch(kernel, Mathf.CeilToInt((float)activeList.count / NUM_THREAD_X), 1, 1);

            inViewsCountBuffer.SetData(inViewsCounts);
            ComputeBuffer.CopyCount(inViewsAppendBuffer, inViewsCountBuffer, 4);    // インスタンス数
            //inViewsCountBuffer.GetData(inViewsCounts);
            //inViewsNum = inViewsCounts[0];
            //Debug.Log("inViewsCounts " + inViewsCounts[0]);

            // debug
            //if (Input.GetKeyDown(KeyCode.M))
            //{
            //    //inViewsCountBuffer.GetData(inViewsCounts);
            //    //Debug.Log("inViewsCounts " + inViewsCounts[0]);

            //    DumpAppendData(inViewsAppendBuffer, particleNum, "inviews");

            //    DumpAppendData(activeList, particleNum, "activeList");
            //}

        }

        void DumpAppendData(ComputeBuffer cb, int size, string name)
        {
            var data = new uint[size];
            cb.GetData(data);
            StreamWriter sw;
            FileInfo fi;
            string date = System.DateTime.Now.ToString("yyyyMMddHHmmss");
            fi = new FileInfo(Application.dataPath + "/../" + name + date + ".csv");
            sw = fi.AppendText();
            for (int i = 0; i < data.Length; i++)
            {
                //Debug.Log("[" + i + "] GridHash " + gridHashDataArray[i] + " index " + sortedIndexDataArray[i]);
                //sw.WriteLine("" + i + "," + debugData[i].isActive + "," + debugData[i].position + "," + debugData[i].velocity + "," + debugData[i].rotation + "," + debugData[i].animeTime + "," + debugData[i].speed + "," + debugData[i].offsetLimit);
                sw.WriteLine("" + i + "," + data[i]);
            }
            sw.Flush();
            sw.Close();
            Debug.Log("Dump AppendBuffer Data " + fi.FullName);
        }
    }
#endregion

    

#region public
    public ComputeShader cullingCS;
    public bool isCulling = true;
    public float scale = 1;
#endregion

#region protected
    protected Dictionary<Camera, CullingData> cameraDatas = new Dictionary<Camera, CullingData>();

    protected virtual void UpdateVertexBuffer(Camera camera)
    {
        
        CullingData data = cameraDatas[camera];
        if (data == null)
        {
            data = cameraDatas[camera] = new CullingData(particleNum);
        }

        //_SetCommonParameterForCS(cullingCS);
        data.Update(cullingCS, camera, particleNum, particleBuffer, activeIndexBuffer);
    }
    #endregion

    protected override void OnRenderObjectInternal()
    {
        var cam = Camera.current;
        if (isCulling)
        {
            if (!cameraDatas.ContainsKey(cam))
            {
                cameraDatas[cam] = null; // このフレームは登録だけ
            }
            else
            {
                var data = cameraDatas[cam];
                if (data != null)
                {
                    SetMaterialParam();
                    
                    material.EnableKeyword("GPUPARTICLE_CULLING_ON");

                    material.SetBuffer("_InViewsList", data.inViewsAppendBuffer);

                    Graphics.DrawProceduralIndirect(MeshTopology.Points, data.inViewsCountBuffer, 0);
                }
            }
        }
        else
        {
            base.OnRenderObjectInternal();
        }
    }

    protected virtual void LateUpdate()
    {
        if (isCulling)
        {
			//Dictionary<Camera, CullingData>.KeyCollection keys = cameraDatas.Keys;
			//int count = keys.Count;
			Camera[] cameras = cameraDatas.Keys.ToArray();
			for (int i = cameras.Length - 1; i >= 0; i--) {
				if (cameras[i] == null) {
					cameraDatas.Remove (cameras[i]);
				}else if(cameras [i].isActiveAndEnabled) {
					UpdateVertexBuffer (cameras [i]);
				}
			}
			//keys = cameraDatas.Keys;
//			foreach (Camera cam in keys) {
//				if(cam.isActiveAndEnabled){
//					UpdateVertexBuffer(cam);
//				}
//			}

			////
//            cameraDatas.Keys.Where(cam => cam == null).ToList().ForEach(cam => cameraDatas.Remove(cam));
//            cameraDatas.Keys
//                .Where(cam => cam.isActiveAndEnabled)
//                .ToList().ForEach(cam =>
//                {
//                    UpdateVertexBuffer(cam);
//                });
        }
    }

    protected virtual void ReleaseBuffer()
    {
        cameraDatas.Values.Where(d => d != null).ToList().ForEach(d => d.Release());
        cameraDatas.Clear();
    }

    private void OnDestroy()
    {
        ReleaseBuffer();
    }
}
