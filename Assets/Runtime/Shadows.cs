using Palmmedia.ReportGenerator.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    // 最大のDirectional Light数
    const int maxShadowedDirectionalLightCount = 1;

    // 影のために使うdirectional Lightの構造体
    struct ShadowedDirectionalLight
    {
        // 対象の可視光のindex
        public int visibleLightIndex;
    }

    ShadowedDirectionalLight[] shadowedDirectionalLights =
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

    // バッファ名
    const string bufferName = "shadows";

    // コマンドバッファ
    CommandBuffer buffer = new CommandBuffer { name = bufferName };

    // コンテキスト
    ScriptableRenderContext context;

    // カリング設定
    CullingResults cullingResults;

    // 影の設定
    ShadowSettings ShadowSettings;

    // 利用する影の現在の個数
    int shadowedDirectionalLightCount;

    // DirectionalLightのShadowのAtlasのID
    static int directionalShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");

    public void Setup(
        ScriptableRenderContext context,
        CullingResults cullingResults,
        ShadowSettings shadowSettings)
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.ShadowSettings = shadowSettings;
        this.shadowedDirectionalLightCount = 0;
    }

    public void ReserveDirectionalShadows(
        Light light,
        int visibleLightIndex
        )
    {
        // Shadow対象でない
        if (light.shadows == LightShadows.None)
        {
            return;
        }

        // Shadowの強度が0以下の場合は出ない
        if (light.shadowStrength <= 0f)
        {
            return;
        }

        // Right範囲にShadowCasterがいないならスキップする
        if (cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b) == false)
        {
            return;
        }

        if (shadowedDirectionalLightCount < maxShadowedDirectionalLightCount)
        {
            shadowedDirectionalLights[shadowedDirectionalLightCount++] =
                new ShadowedDirectionalLight
                {
                    visibleLightIndex = visibleLightIndex
                };
        }
    }

    public void Render()
    {
        if (this.shadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            // 確保自体は必要ない場合でもやっておく
            buffer.GetTemporaryRT(directionalShadowAtlasId, 1, 1, 32,
                FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }
    }

    private void RenderDirectionalShadows()
    {
        // Atlasの用意
        int atlasSize = (int)(this.ShadowSettings.directional.atlasSize);
        buffer.GetTemporaryRT(directionalShadowAtlasId, atlasSize, atlasSize, 32,
            FilterMode.Bilinear, RenderTextureFormat.Shadowmap);

        // 描画するRenderTargetの設定
        // ClearはしなくてOK, ShadowMap書き込みなので保存はする
        buffer.SetRenderTarget(
            directionalShadowAtlasId,
            RenderBufferLoadAction.DontCare, 
            RenderBufferStoreAction.Store
            );

        // Depthのみクリアしておく
        buffer.ClearRenderTarget(true, false, Color.clear);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        for (int i = 0; i < shadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, atlasSize);
        }

        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    private void RenderDirectionalShadows(int index, int tileSize)
    {
        ShadowedDirectionalLight light = shadowedDirectionalLights[index];
        
        // Shadowに必要な設定を用意
        var shadowSettings = new ShadowDrawingSettings(
            cullingResults, 
            light.visibleLightIndex,
            BatchCullingProjectionType.Orthographic);

        // LPV行列を取得
        cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
            activeLightIndex: light.visibleLightIndex,
            splitIndex: 0,
            splitCount: 1,
            splitRatio: Vector3.zero,
            shadowResolution: tileSize,
            shadowNearPlaneOffset: 0f,
            viewMatrix: out Matrix4x4 viewMatrix,
            projMatrix: out Matrix4x4 projectionMatrix,
            shadowSplitData: out ShadowSplitData splitData);

        // 取得した情報を設定
        shadowSettings.splitData = splitData;
        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

        // 実行
        ExecuteBuffer();
        context.DrawShadows(ref shadowSettings);
    }

    public void Cleanup()
    {
        buffer.ReleaseTemporaryRT(directionalShadowAtlasId);
        ExecuteBuffer();
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
}
