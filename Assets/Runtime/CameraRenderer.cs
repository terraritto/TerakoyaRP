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

    // ShaderTagId
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");

    // Light
    Lighting lighting = new Lighting();

    // Camera����Rendering���`����
    public void Render(
        ScriptableRenderContext context,
        Camera camera,
        bool useDynamicBatching,
        bool useGPUInstancing,
        ShadowSettings shadowSettings)
    {
        // context��camera��ێ�
        this.context = context;
        this.camera = camera;

        // Buffer���̏���
        PrepareBuffer();

        // Scene�ł�UI�`��(�J�����O�O�Ɏ��s���s��)
        PrepareForSceneWindow();

        // �J�����O�̏���
        if (!Cull(shadowSettings.maxDistance))
        {
            return;
        }

        // ���C�g�̍X�V
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
        lighting.Setup(context, cullingResults, shadowSettings);
        buffer.EndSample(SampleName);

        // �`��O�̐ݒ�
        Setup();

        // �`��
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);

        // ������Shader�̕`��
        DrawUnsupportedShaders();

        // �M�Y���̕`��
        DrawGizmos();

        // �f�[�^�̌�n��
        lighting.Cleanup();

        // �R�}���h�̔��s
        Submit();
    }

    bool Cull(float maxShadowDistance)
    {
        // �J��������J�����O�p�����[�^����ꂽ�������ʂ�
        if (this.camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters))
        {
            // ���ʂ�ێ����Ă���
            cullingParameters.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
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

    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        SortingSettings sortingSettings = new SortingSettings(this.camera)
        { criteria = SortingCriteria.RenderQueue };
        DrawingSettings drawingSettings = new DrawingSettings(
            unlitShaderTagId, sortingSettings
            )
        {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing
        };
        drawingSettings.SetShaderPassName(1, litShaderTagId);
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
