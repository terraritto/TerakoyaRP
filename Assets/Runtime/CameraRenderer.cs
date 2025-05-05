using System.ComponentModel.Design.Serialization;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    ScriptableRenderContext context;

    Camera camera;

    // コマンドバッファ
    const string bufferName = "Render Camera";
    CommandBuffer buffer = new CommandBuffer { name = bufferName };

    // Cullingの結果
    CullingResults cullingResults;

    // ShaderTagId
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");

    // Light
    Lighting lighting = new Lighting();

    // Camera毎のRenderingを定義する
    public void Render(ScriptableRenderContext context, Camera camera,
        bool useDynamicBatching, bool useGPUInstancing)
    {
        // contextとcameraを保持
        this.context = context;
        this.camera = camera;

        // Buffer側の準備
        PrepareBuffer();

        // SceneでのUI描画(カリング前に実行を行う)
        PrepareForSceneWindow();

        // カリングの処理
        if (!Cull())
        {
            return;
        }

        // 描画前の設定
        Setup();

        // ライトの更新
        lighting.Setup(context, cullingResults);

        // 描画
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);

        // 無効なShaderの描画
        DrawUnsupportedShaders();

        // ギズモの描画
        DrawGizmos();

        // コマンドの発行
        Submit();
    }

    bool Cull()
    {
        // カメラからカリングパラメータが取れた時だけ通す
        if (this.camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters))
        {
            // 結果を保持しておく
            this.cullingResults = this.context.Cull(ref cullingParameters);
            return true;
        }
        return false;
    }

    void Setup()
    {
        // カメラの設定を反映させる
        // ビュー投影行列とかその辺
        this.context.SetupCameraProperties(this.camera);

        // ClearFlagに合わせて初期化
        CameraClearFlags flags = camera.clearFlags;
        this.buffer.ClearRenderTarget
            (flags <= CameraClearFlags.Depth, 
             flags <= CameraClearFlags.Color, 
             flags == CameraClearFlags.Color ?
             camera.backgroundColor.linear : Color.clear);

        // コマンドバッファのプロファイル開始
        this.buffer.BeginSample(this.SampleName);

        // ↑のコマンドを投げる
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

        // 不透明を描画
        this.context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
            );

        // skybox描画
        this.context.DrawSkybox(this.camera);

        // 透過に設定を変更
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        // 透過を描画
        this.context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
            );
    }
    void Submit()
    {
        // コマンドバッファのプロファイル終了
        this.buffer.EndSample(this.SampleName);

        // ↑のコマンドを投げる
        ExecuteBuffer();

        // コマンド発行
        this.context.Submit();
    }
    
    void ExecuteBuffer()
    {
        // bufferの実行
        this.context.ExecuteCommandBuffer(buffer);
        // bufferをクリアしておく
        buffer.Clear();
    }
}
