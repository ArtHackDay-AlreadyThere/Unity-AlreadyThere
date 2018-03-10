using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TranscationParticleRender : GPUParticleRendererBase<TransactionParticleData>
{
    protected override void SetMaterialParam()
    {
        material.SetBuffer("_Particles", particleBuffer);
        material.SetBuffer("_ParticleActiveList", activeIndexBuffer);
        
        material.SetPass(0);
    }

    protected override void OnRenderObjectInternal()
    {
        SetMaterialParam();

        material.DisableKeyword("GPUPARTICLE_CULLING_ON");

        //Graphics.DrawProcedural(MeshTopology.Points, 1);
        Graphics.DrawProceduralIndirect(MeshTopology.Points, activeCountBuffer, 0);
    }
}
