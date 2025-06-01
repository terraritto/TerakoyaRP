using Palmmedia.ReportGenerator.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    // �ő��Directional Light��
    const int maxShadowedDirectionalLightCount = 1;

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
        // Shadow�ΏۂłȂ�
        if (light.shadows == LightShadows.None)
        {
            return;
        }

        // Shadow�̋��x��0�ȉ��̏ꍇ�͏o�Ȃ�
        if (light.shadowStrength <= 0f)
        {
            return;
        }

        // Right�͈͂�ShadowCaster�����Ȃ��Ȃ�X�L�b�v����
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
        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

        // ���s
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
