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

    [SerializeField]
    ShadowSettings shadows = default;

    protected override RenderPipeline CreatePipeline()
    {
        return new TerakoyaRenderPipeline(
            useDynamicBatching: useDynamicBatching, 
            useGPUInstancing: useGPUInstancing, 
            useSRPBatcher: useSRPBatcher, 
            shadowSettings: shadows
            );
    }
}
