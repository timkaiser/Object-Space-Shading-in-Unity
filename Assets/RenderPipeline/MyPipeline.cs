//based on tutorial: https://catlikecoding.com/unity/tutorials/scriptable-render-pipeline/custom-pipeline/

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

public class MyPipeline : RenderPipeline {
    //### variables ################################################################################################################
    CullResults cull;
    CommandBuffer cameraBuffer = new CommandBuffer {
        name = "Render Camera"
    };

    /* Public static for debugging reasons*/
    #if DEBUG
    public static RenderTexture[] CSoutputCopy; 
    public static RenderTexture[] rts;
    //Texture for final image
    public static RenderTexture mainImage;
#else
    RenderTexture[] rts;
    //Texture for final image
    RenderTexture mainImage;
#endif


    RenderBuffer[] colorBuffers;
    RenderBuffer depthBuffer;
    
    public const int MIP_MAP_COUNT = 9;
    public const int MAX_TEXTURE_SIZE = 1 << MIP_MAP_COUNT;

    //compute shader for mapping the normal/world position to a texture atlas
    private ComputeShader debugCS;
    private int CSkernel;

    //### Constructor ##############################################################################################################
    public MyPipeline() {
        //Find Camera
        Camera cam = GameObject.FindObjectOfType<Camera>();
        int width = cam.pixelWidth;
        int height = cam.pixelHeight;

        //Initilize RenderTexture for final image
        mainImage = new RenderTexture(width, height, 24, RenderTextureFormat.Default);


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
        #region First Pass

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
        

        context.Submit();
        #endregion

        var result = runComputeShader(camera.pixelWidth,camera.pixelHeight);

        #region Second Pass
        //Second Shader Pass
        //set render target
        //Graphics.SetRenderTarget(mainImage);
        //camera.SetReplacementShader(Shader.Find("Custom/SecondPass"), "");

        cameraBuffer.Clear();

        Shader secondPass = Shader.Find("Custom/SecondPass");
        Material secondPassMaterial = new Material(secondPass);
        secondPassMaterial.SetTexture("_TextureArray", result);


        #region clearing
        cameraBuffer.ClearRenderTarget(
            (clearFlags & CameraClearFlags.Depth) != 0,
            (clearFlags & CameraClearFlags.Color) != 0,
            camera.backgroundColor
        );


        context.ExecuteCommandBuffer(cameraBuffer);
        cameraBuffer.Clear();
        #endregion

        #region drawing

        //setup settings for rendering unlit opaque materials
        drawSettings = new DrawRendererSettings(camera, new ShaderPassName("SRPDefaultUnlit"));
        drawSettings.sorting.flags = SortFlags.CommonOpaque;

        filterSettings = new FilterRenderersSettings(true);


        drawSettings.SetOverrideMaterial(secondPassMaterial, 0);

        //draw unlit opaque materials
        context.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);

        #endregion

        
        context.Submit();
        result.Release();
        #endregion

    }

    //### other methodes #############################################################################################################

    RenderTexture runComputeShader(int width, int height) {
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
        return result;
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