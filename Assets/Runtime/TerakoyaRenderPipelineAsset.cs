using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Terakoya RP")]
public class TerakoyaRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    bool useDynamicBatching = true;

    [SerializeField]
    bool useGPUInstancing = true;

    [SerializeField]
    bool useSRPBatcher = true;

    protected override RenderPipeline CreatePipeline()
    {
        return new TerakoyaRenderPipeline(useDynamicBatching, useGPUInstancing, useSRPBatcher);
    }
}
