using Palmmedia.ReportGenerator.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class Shadows
{
    // 最大のDirectional Light数
    const int maxShadowedDirectionalLightCount = 4;

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
    static int directionalShadowAtlasId = 
        Shader.PropertyToID("_DirectionalShadowAtlas");

    // DirectionalLightのShadowのMatrixのID
    static int directionalShadowMatrixId = 
        Shader.PropertyToID("_DirectionalShadowMatrices");
    // Shadow用のViewProj Matrix
    static Matrix4x4[] directionalShadowMatrices = 
        new Matrix4x4[maxShadowedDirectionalLightCount];

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

    public Vector2 ReserveDirectionalShadows(
        Light light,
        int visibleLightIndex
        )
    {
        // Shadow対象でない
        if (light.shadows == LightShadows.None)
        {
            return Vector2.zero;
        }

        // Shadowの強度が0以下の場合は出ない
        if (light.shadowStrength <= 0f)
        {
            return Vector2.zero;
        }

        // Right範囲にShadowCasterがいないならスキップする
        if (cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b) == false)
        {
            return Vector2.zero;
        }

        if (shadowedDirectionalLightCount < maxShadowedDirectionalLightCount)
        {
            shadowedDirectionalLights[shadowedDirectionalLightCount] =
                new ShadowedDirectionalLight
                {
                    visibleLightIndex = visibleLightIndex
                };

            return new Vector2(
                light.shadowStrength, shadowedDirectionalLightCount++);
        }

        return Vector2.zero;
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

        int split = shadowedDirectionalLightCount <= 1 ? 1 : 2;
        int tileSize = atlasSize / split;

        for (int i = 0; i < shadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }

        // MatrixをGPUに送信
        buffer.SetGlobalMatrixArray(
            nameID: directionalShadowMatrixId, 
            values: directionalShadowMatrices
            );

        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    private void RenderDirectionalShadows(int index, int split, int tileSize)
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
        directionalShadowMatrices[index] =
            ConvertToAtlasMatrix(
            m: projectionMatrix * viewMatrix,
            offset: SetTileViewport(index, split, tileSize),
            split: split
            ); // Matrixを用意
        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

        // 実行
        ExecuteBuffer();
        context.DrawShadows(ref shadowSettings);
    }

    private Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        // x->0,1,0,1 y->0,0,1,1のように描画位置を変える
        Vector2 offset = new Vector2(index % split, index / split);
        buffer.SetViewport(new Rect(
            offset.x * tileSize, offset.y * tileSize, tileSize, tileSize
            ));

        return offset;
    }

    private Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        return m;
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
