using Palmmedia.ReportGenerator.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class Shadows
{
    // �ő��Directional Light��
    const int maxShadowedDirectionalLightCount = 4;

    // �e�̂��߂Ɏg��directional Light�̍\����
    struct ShadowedDirectionalLight
    {
        // �Ώۂ̉�����index
        public int visibleLightIndex;
    }

    ShadowedDirectionalLight[] shadowedDirectionalLights =
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

    // �o�b�t�@��
    const string bufferName = "shadows";

    // �R�}���h�o�b�t�@
    CommandBuffer buffer = new CommandBuffer { name = bufferName };

    // �R���e�L�X�g
    ScriptableRenderContext context;

    // �J�����O�ݒ�
    CullingResults cullingResults;

    // �e�̐ݒ�
    ShadowSettings ShadowSettings;

    // ���p����e�̌��݂̌�
    int shadowedDirectionalLightCount;

    // DirectionalLight��Shadow��Atlas��ID
    static int directionalShadowAtlasId = 
        Shader.PropertyToID("_DirectionalShadowAtlas");

    // DirectionalLight��Shadow��Matrix��ID
    static int directionalShadowMatrixId = 
        Shader.PropertyToID("_DirectionalShadowMatrices");
    // Shadow�p��ViewProj Matrix
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
        // Shadow�ΏۂłȂ�
        if (light.shadows == LightShadows.None)
        {
            return Vector2.zero;
        }

        // Shadow�̋��x��0�ȉ��̏ꍇ�͏o�Ȃ�
        if (light.shadowStrength <= 0f)
        {
            return Vector2.zero;
        }

        // Right�͈͂�ShadowCaster�����Ȃ��Ȃ�X�L�b�v����
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
            // �m�ێ��͕̂K�v�Ȃ��ꍇ�ł�����Ă���
            buffer.GetTemporaryRT(directionalShadowAtlasId, 1, 1, 32,
                FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }
    }

    private void RenderDirectionalShadows()
    {
        // Atlas�̗p��
        int atlasSize = (int)(this.ShadowSettings.directional.atlasSize);
        buffer.GetTemporaryRT(directionalShadowAtlasId, atlasSize, atlasSize, 32,
            FilterMode.Bilinear, RenderTextureFormat.Shadowmap);

        // �`�悷��RenderTarget�̐ݒ�
        // Clear�͂��Ȃ���OK, ShadowMap�������݂Ȃ̂ŕۑ��͂���
        buffer.SetRenderTarget(
            directionalShadowAtlasId,
            RenderBufferLoadAction.DontCare, 
            RenderBufferStoreAction.Store
            );

        // Depth�̂݃N���A���Ă���
        buffer.ClearRenderTarget(true, false, Color.clear);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        int split = shadowedDirectionalLightCount <= 1 ? 1 : 2;
        int tileSize = atlasSize / split;

        for (int i = 0; i < shadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }

        // Matrix��GPU�ɑ��M
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
        
        // Shadow�ɕK�v�Ȑݒ��p��
        var shadowSettings = new ShadowDrawingSettings(
            cullingResults, 
            light.visibleLightIndex,
            BatchCullingProjectionType.Orthographic);

        // LPV�s����擾
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

        // �擾��������ݒ�
        shadowSettings.splitData = splitData;
        directionalShadowMatrices[index] =
            ConvertToAtlasMatrix(
            m: projectionMatrix * viewMatrix,
            offset: SetTileViewport(index, split, tileSize),
            split: split
            ); // Matrix��p��
        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

        // ���s
        ExecuteBuffer();
        context.DrawShadows(ref shadowSettings);
    }

    private Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        // x->0,1,0,1 y->0,0,1,1�̂悤�ɕ`��ʒu��ς���
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
