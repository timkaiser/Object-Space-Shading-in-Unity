//based on tutorial: https://catlikecoding.com/unity/tutorials/scriptable-render-pipeline/custom-pipeline/

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

public class MyPipline : RenderPipeline {
    //### variables ################################################################################################################
    CullResults cull;
    CommandBuffer cameraBuffer = new CommandBuffer {
        name = "Render Camera"
    };

    /* Public static for debugging reasons*/
    #if DEBUG
    public static RenderTexture[] CSoutputCopy; 
    public static RenderTexture[] rts;
    #else
    RenderTexture[] rts;
    #endif

    RenderBuffer[] colorBuffers;
    RenderBuffer depthBuffer;
    
    public const int MIP_MAP_COUNT = 9;
    public const int MAX_TEXTURE_SIZE = 1 << MIP_MAP_COUNT;

    //compute shader for mapping the normal/world position to a texture atlas
    private ComputeShader debugCS;
    private int CSkernel;

    //### Constructor ##############################################################################################################
    public MyPipline() {
        //Find Camera
        Camera cam = GameObject.FindObjectOfType<Camera>();
        int width = cam.pixelWidth;
        int height = cam.pixelHeight;

        //Initialize Render Textures
        rts = new RenderTexture[4] {
            new RenderTexture(width, height, 0, RenderTextureFormat.RInt), //ID
            new RenderTexture(width, height, 0, RenderTextureFormat.RG16), //UV
            new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat), //World Position
            new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32), //Normal
        };

        //get color buffers from rendertextures
        colorBuffers = new RenderBuffer[4] { rts[0].colorBuffer, rts[1].colorBuffer, rts[2].colorBuffer, rts[3].colorBuffer };

        //create depth buffer
        RenderTexture depthBufferTexture = new RenderTexture(width, height, 32, RenderTextureFormat.Depth);
        depthBufferTexture.Create();
        depthBuffer = depthBufferTexture.depthBuffer;

        //compute shader
        debugCS = (ComputeShader)(Resources.Load("Shader/DebugCS"));
        CSkernel = debugCS.FindKernel("DebugCS");


        #if DEBUG
        //debug for cs output
        CSoutputCopy = new RenderTexture[MIP_MAP_COUNT];
        for (int i = 0; i < MIP_MAP_COUNT; ++i) {
            CSoutputCopy[i] = new RenderTexture(MAX_TEXTURE_SIZE, MAX_TEXTURE_SIZE, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            CSoutputCopy[i].filterMode = FilterMode.Point;
            CSoutputCopy[i].Create();
        }
        #endif
    }


    //### Rendering #######################################################################################################
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
        //sample call for frame debugger
        cameraBuffer.BeginSample("Camera Render");
        //basic rendering stuff =================================================

        //cull objects not on screen
        #region culling
        ScriptableCullingParameters cullingParameters;
        if (!CullResults.GetCullingParameters(camera, out cullingParameters)) {
            return;
        }

        CullResults.Cull(ref cullingParameters, context, ref cull);
        #endregion

        //setup cmaera
        context.SetupCameraProperties(camera);

        //clear render target
        #region clearing
        CameraClearFlags clearFlags = camera.clearFlags;
        cameraBuffer.ClearRenderTarget(
            (clearFlags & CameraClearFlags.Depth) != 0,
            (clearFlags & CameraClearFlags.Color) != 0,
            camera.backgroundColor
        );


        context.ExecuteCommandBuffer(cameraBuffer);
        cameraBuffer.Clear();
        #endregion


        #region drawing
        //set render target
        camera.SetTargetBuffers(colorBuffers, depthBuffer);

        //setup settings for rendering unlit opaque materials
        var drawSettings = new DrawRendererSettings(camera, new ShaderPassName("SRPDefaultUnlit"));
        drawSettings.sorting.flags = SortFlags.CommonOpaque;
   
        var filterSettings = new FilterRenderersSettings(true);

        //draw unlit opaque materials
        context.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);
        
        //reset Render Target
        Graphics.SetRenderTarget(null);
        #endregion

        cameraBuffer.EndSample("Camera Render");

        Graphics.Blit(rts[1], camera.activeTexture);

        context.Submit();

        runComputeShader(camera.pixelWidth,camera.pixelHeight);
    }

    //### other methodes #############################################################################################################

    private void runComputeShader(int width, int height) {
        Graphics.SetRenderTarget(null);
        var result = CreateIntermediateCSTarget();

        debugCS.SetTexture(CSkernel, "Output", result);
        debugCS.SetTexture(CSkernel, "ID", rts[0]);
        debugCS.SetTexture(CSkernel, "UV", rts[1]);
        debugCS.SetTexture(CSkernel, "WorldPos", rts[2]);
        debugCS.SetTexture(CSkernel, "Normal", rts[3]);

        //Call compute shader
        debugCS.Dispatch(CSkernel, width / 8, height / 8, 1);

        #if DEBUG
        //debug for cs output
        for (int i = 0; i < MIP_MAP_COUNT; ++i) {
            Graphics.CopyTexture(result, i, CSoutputCopy[i], 0);
        }
        #endif

        result.Release();
    }

    RenderTexture CreateIntermediateCSTarget() {
        var result = new RenderTexture(MAX_TEXTURE_SIZE, MAX_TEXTURE_SIZE, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        result.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        result.enableRandomWrite = true;
        result.filterMode = FilterMode.Point;
        result.anisoLevel = 0;
        result.volumeDepth = MIP_MAP_COUNT;
        result.Create();

        return result;
    }
 

}