using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    // Directional Lightの最大数
    const int MaxDirectionalLight = 4;

    // バッファ名
    const string bufferName = "Lighting";

    // コマンドバッファ
    CommandBuffer buffer = new CommandBuffer { name = bufferName };

    // カリング設定
    CullingResults cullingResults;

    // Id
    static int directionalLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    static int directionalLightColorId = Shader.PropertyToID("_DirectionalLightColors");
    static int directionalLightDirectionId = Shader.PropertyToID("_DirectionalLightDirections");

    static Vector4[] directionalLightColors = new Vector4[MaxDirectionalLight];
    static Vector4[] directionalLightDirections = new Vector4[MaxDirectionalLight];

    Shadows shadows = new Shadows();

    public void Setup(
        ScriptableRenderContext context,
        CullingResults cullingResults,
        ShadowSettings shadowSettings)
    {
        this.cullingResults = cullingResults;

        buffer.BeginSample(bufferName);
        shadows.Setup(context, cullingResults, shadowSettings);
        SetupLights();
        shadows.Render();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    public void Cleanup()
    {
        shadows.Cleanup();
    }

    void SetupLights()
    {
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        int directionalLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            if (visibleLight.lightType != LightType.Directional)
            {
                // Directional Light出ないなら計上しない
                continue;
            }
            
            SetupDirectionalLight(directionalLightCount++, ref visibleLight);

            // 最大数を超えてたら反映しない
            if (directionalLightCount >= MaxDirectionalLight)
            {
                break;
            }
        }

        buffer.SetGlobalInt(directionalLightCountId, directionalLightCount);
        buffer.SetGlobalVectorArray(directionalLightColorId, directionalLightColors);
        buffer.SetGlobalVectorArray(directionalLightDirectionId, directionalLightDirections);
    }

    void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
    {
        directionalLightColors[index] = visibleLight.finalColor;
        directionalLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        shadows.ReserveDirectionalShadows(visibleLight.light, index);
    }
}
