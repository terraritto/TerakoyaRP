using System.ComponentModel.Design.Serialization;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    ScriptableRenderContext context;

    Camera camera;

    // �R�}���h�o�b�t�@
    const string bufferName = "Render Camera";
    CommandBuffer buffer = new CommandBuffer { name = bufferName };

    // Culling�̌���
    CullingResults cullingResults;

    // Unlit��ShaderTagId
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

    // Camera����Rendering���`����
    public void Render(ScriptableRenderContext context, Camera camera)
    {
        // context��camera��ێ�
        this.context = context;
        this.camera = camera;

        // Buffer���̏���
        PrepareBuffer();

        // Scene�ł�UI�`��(�J�����O�O�Ɏ��s���s��)
        PrepareForSceneWindow();

        // �J�����O�̏���
        if (!Cull())
        {
            return;
        }

        // �`��O�̐ݒ�
        Setup();

        // �`��
        DrawVisibleGeometry();

        // ������Shader�̕`��
        DrawUnsupportedShaders();

        // �M�Y���̕`��
        DrawGizmos();

        // �R�}���h�̔��s
        Submit();
    }

    bool Cull()
    {
        // �J��������J�����O�p�����[�^����ꂽ�������ʂ�
        if (this.camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters))
        {
            // ���ʂ�ێ����Ă���
            this.cullingResults = this.context.Cull(ref cullingParameters);
            return true;
        }
        return false;
    }

    void Setup()
    {
        // �J�����̐ݒ�𔽉f������
        // �r���[���e�s��Ƃ����̕�
        this.context.SetupCameraProperties(this.camera);

        // ClearFlag�ɍ��킹�ď�����
        CameraClearFlags flags = camera.clearFlags;
        this.buffer.ClearRenderTarget
            (flags <= CameraClearFlags.Depth, 
             flags <= CameraClearFlags.Color, 
             flags == CameraClearFlags.Color ?
             camera.backgroundColor.linear : Color.clear);

        // �R�}���h�o�b�t�@�̃v���t�@�C���J�n
        this.buffer.BeginSample(this.SampleName);

        // ���̃R�}���h�𓊂���
        ExecuteBuffer();
    }

    void DrawVisibleGeometry()
    {
        SortingSettings sortingSettings = new SortingSettings(this.camera)
        { criteria = SortingCriteria.RenderQueue };
        DrawingSettings drawingSettings = new DrawingSettings(
            unlitShaderTagId, sortingSettings
            );
        FilteringSettings filteringSettings = new FilteringSettings(
            RenderQueueRange.opaque
            );

        // �s������`��
        this.context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
            );

        // skybox�`��
        this.context.DrawSkybox(this.camera);

        // ���߂ɐݒ��ύX
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        // ���߂�`��
        this.context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
            );
    }
    void Submit()
    {
        // �R�}���h�o�b�t�@�̃v���t�@�C���I��
        this.buffer.EndSample(this.SampleName);

        // ���̃R�}���h�𓊂���
        ExecuteBuffer();

        // �R�}���h���s
        this.context.Submit();
    }
    
    void ExecuteBuffer()
    {
        // buffer�̎��s
        this.context.ExecuteCommandBuffer(buffer);
        // buffer���N���A���Ă���
        buffer.Clear();
    }
}