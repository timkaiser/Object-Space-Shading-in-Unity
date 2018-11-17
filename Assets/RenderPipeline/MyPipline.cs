//based on tutorial: https://catlikecoding.com/unity/tutorials/scriptable-render-pipeline/custom-pipeline/

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public class MyPipline : RenderPipeline {
    //variables
    CullResults cull;
    Material errorMaterial;
    CommandBuffer cameraBuffer = new CommandBuffer {
        name = "Render Camera"
    };


    //methodes

    //called by unity at the begining
    public override void Render(ScriptableRenderContext renderContext, Camera[] cameras) {
        base.Render(renderContext, cameras);

        //call new render methode for every camera
        foreach (var camera in cameras) {
            Render(renderContext, camera);
        }
    }

    //called once for every camera
    public void Render(ScriptableRenderContext context, Camera camera) {
        //cull objects not on screen
        ScriptableCullingParameters cullingParameters;
        if (!CullResults.GetCullingParameters(camera, out cullingParameters)) {
            return;
        }

        CullResults.Cull(ref cullingParameters, context, ref cull);

        //setup cmaera
        context.SetupCameraProperties(camera);

        //clear render target
        CameraClearFlags clearFlags = camera.clearFlags;
        cameraBuffer.ClearRenderTarget(
            (clearFlags & CameraClearFlags.Depth) != 0,
            (clearFlags & CameraClearFlags.Color) != 0,
            camera.backgroundColor
        );

        //sample call for frame debugger
        cameraBuffer.BeginSample("Render Camera");

        context.ExecuteCommandBuffer(cameraBuffer);
        cameraBuffer.Clear();

        //setup settings for rendering unlit opaque materials
        var drawSettings = new DrawRendererSettings(camera, new ShaderPassName("SRPDefaultUnlit"));
        drawSettings.sorting.flags = SortFlags.CommonOpaque;
   
        var filterSettings = new FilterRenderersSettings(true);

        //draw unlit opaque materials
        context.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);

        //draw skybox
        context.DrawSkybox(camera);

        //setup settings for rendering unlit transparent materials
        drawSettings.sorting.flags = SortFlags.CommonTransparent;
        filterSettings.renderQueueRange = RenderQueueRange.transparent;

        //unlit transparent materials
        context.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);

        //call methode to draw not supported materials with error material
        DrawDefaultPipeline(context, camera);

        cameraBuffer.EndSample("Render Camera");
        context.ExecuteCommandBuffer(cameraBuffer);
        cameraBuffer.Clear();

        //Sumbit all commands for execution
        context.Submit();

    }

    //methode that draws all unsupported materials with error texture
    void DrawDefaultPipeline(ScriptableRenderContext context, Camera camera) {
        //find unitys error material
        if (errorMaterial == null) {
            Shader errorShader = Shader.Find("Hidden/InternalErrorShader");
            errorMaterial = new Material(errorShader) {
                hideFlags = HideFlags.HideAndDontSave
            };
        }
        //find shader passes for all unsupported materials
        var drawSettings = new DrawRendererSettings(camera, new ShaderPassName("ForwardBase"));
        drawSettings.SetShaderPassName(1, new ShaderPassName("PrepassBase"));
        drawSettings.SetShaderPassName(2, new ShaderPassName("Always"));
        drawSettings.SetShaderPassName(3, new ShaderPassName("Vertex"));
        drawSettings.SetShaderPassName(4, new ShaderPassName("VertexLMRGBM"));
        drawSettings.SetShaderPassName(5, new ShaderPassName("VertexLM"));
        drawSettings.SetOverrideMaterial(errorMaterial, 0);

        var filterSettings = new FilterRenderersSettings(true);

        //draw unsupported materials
        context.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);
    }
}