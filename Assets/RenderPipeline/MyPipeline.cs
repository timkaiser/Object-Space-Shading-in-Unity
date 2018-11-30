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

    Texture texture;
    /* Public static for debugging reasons*/
    #if DEBUG
    public static RenderTexture tileMaskCopy;
    public static RenderTexture worldPosMapCopy;
    public static RenderTexture[] rts;
    public static RenderTexture[] loadedTexture;
    public static RenderTexture baycentricCoords;
    public static RenderTexture vertexIds;
    public static RenderTexture finalImage;

#else
    RenderTexture[] rts;    
    RenderTexture baycentricCoords;
    RenderTexture vertexIds;
    RenderTexture finalImage;
    RenderTexture[] loadedTexture;
#endif


    RenderBuffer[] colorBuffers;
    RenderBuffer depthBuffer;
    
    public const int MIP_MAP_COUNT = 9;
    public const int MAX_TEXTURE_SIZE = 1 << MIP_MAP_COUNT;

    //compute shader for mapping the normal/world position to a texture atlas
    private ComputeShader tileMaskShader;
    private int tileMaskKernel;
    private ComputeShader worldPosShader;
    private int worldPosKernel;

    //### Constructor ##############################################################################################################
    public MyPipeline() {
        //Find Camera
        Camera cam = GameObject.FindObjectOfType<Camera>();
        int width = cam.pixelWidth;
        int height = cam.pixelHeight;

        //load texture
        texture = (Texture)Resources.Load("Textures/earth_daymap");


        //Initialize Render Textures
        rts = new RenderTexture[4] {
            new RenderTexture(width, height, 0, RenderTextureFormat.RInt), //ID
            new RenderTexture(width, height, 0, RenderTextureFormat.RGFloat), //UV
            new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat), //World Position
            new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32), //Normal
        };

        for (int i = 0; i < rts.Length; i++) {
            rts[i].filterMode = FilterMode.Point;
            rts[i].anisoLevel = 0;
            rts[i].Create();
        }

        //get color buffers from rendertextures
        colorBuffers = new RenderBuffer[4] { rts[0].colorBuffer, rts[1].colorBuffer, rts[2].colorBuffer, rts[3].colorBuffer };

        //create depth buffer
        RenderTexture depthBufferTexture = new RenderTexture(width, height, 32, RenderTextureFormat.Depth);
        depthBufferTexture.Create();
        depthBuffer = depthBufferTexture.depthBuffer;

        //compute shader
        tileMaskShader = (ComputeShader)Resources.Load("Shader/UVCoordReader");
        tileMaskKernel = tileMaskShader.FindKernel("CSMain");

        worldPosShader = (ComputeShader)Resources.Load("Shader/WorldPosOSS");
        worldPosKernel = worldPosShader.FindKernel("CSMain");

        #if DEBUG
        tileMaskCopy = new RenderTexture(MAX_TEXTURE_SIZE / 4, MAX_TEXTURE_SIZE / 8, 0, RenderTextureFormat.R8);
        worldPosMapCopy = new RenderTexture(MAX_TEXTURE_SIZE*2, MAX_TEXTURE_SIZE, 0, RenderTextureFormat.ARGBFloat);
        #endif

        //fianl Image
        finalImage = new RenderTexture(width, height, 32, RenderTextureFormat.ARGBFloat);
        finalImage.filterMode = FilterMode.Point;
        finalImage.anisoLevel = 0;
        finalImage.enableRandomWrite = true;
        finalImage.Create();
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
       
        #region Setup
        DrawRendererSettings drawSettings;
        FilterRenderersSettings filterSettings;

        //setup camera
        context.SetupCameraProperties(camera);

        //cull objects not on screen
        #region culling
        ScriptableCullingParameters cullingParameters;
        if (!CullResults.GetCullingParameters(camera, out cullingParameters)) {
            return;
        }

        CullResults.Cull(ref cullingParameters, context, ref cull);

        
        #endregion

        #endregion
        
        #region UV Renderer
        cameraBuffer.BeginSample("UV Renderer");

        if (baycentricCoords == null || vertexIds == null) {
            //Set camera to pass through
            bool isCamOrth = camera.orthographic;
            float camNear = camera.nearClipPlane;
            float camSize = camera.orthographicSize;
            Vector3 camPos = camera.transform.position;
            Quaternion camRot = camera.transform.rotation;
            camera.orthographic = true;
            camera.nearClipPlane = 0;
            camera.orthographicSize = 0.5f;
            camera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.EulerRotation(0, 0, 0));

            context.SetupCameraProperties(camera);

            //Initilize RenderTexture for baycentric coordinate/vertex id
            baycentricCoords = new RenderTexture(MAX_TEXTURE_SIZE, MAX_TEXTURE_SIZE, 0, RenderTextureFormat.ARGBFloat);
            baycentricCoords.filterMode = FilterMode.Point;
            baycentricCoords.anisoLevel = 0;
            baycentricCoords.Create();

            vertexIds = new RenderTexture(MAX_TEXTURE_SIZE, MAX_TEXTURE_SIZE, 24, RenderTextureFormat.ARGBInt);
            vertexIds.filterMode = FilterMode.Point;
            vertexIds.anisoLevel = 0;
            vertexIds.Create();


            cameraBuffer.Clear();

            Shader uvRenderer = Shader.Find("Custom/UVRenderer");
            Material uvRendererMaterial = new Material(uvRenderer);


            //set render target
            RenderBuffer[] cBuffer = new RenderBuffer[2] { baycentricCoords.colorBuffer, vertexIds.colorBuffer };

            camera.SetTargetBuffers(cBuffer, baycentricCoords.depthBuffer);
            #region clearing
            cameraBuffer.ClearRenderTarget(true, true, new Color(0,0,0,0));

            context.ExecuteCommandBuffer(cameraBuffer);
            cameraBuffer.Clear();
            #endregion

            #region drawing
            //setup settings for rendering unlit opaque materials
            drawSettings = new DrawRendererSettings(camera, new ShaderPassName("SRPDefaultUnlit"));
            drawSettings.sorting.flags = SortFlags.CommonOpaque;

            filterSettings = new FilterRenderersSettings(true);

            drawSettings.SetOverrideMaterial(uvRendererMaterial, 0);

            //draw unlit opaque materials
            context.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);

            context.Submit();

            //reset camera
            camera.orthographic = isCamOrth;
            camera.nearClipPlane = camNear;
            camera.orthographicSize = camSize;
            camera.transform.SetPositionAndRotation(camPos, camRot);
            #endregion

            //reset render target
            Graphics.SetRenderTarget(null);
        }

        cameraBuffer.EndSample("UVRenderer");
        #endregion
        
        #region First Pass

        //sample call for frame debugger
        cameraBuffer.BeginSample("First Pass");

        //set render target
        camera.SetTargetBuffers(colorBuffers, depthBuffer);

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

        //setup settings for rendering unlit opaque materials
        drawSettings = new DrawRendererSettings(camera, new ShaderPassName("SRPDefaultUnlit"));
        drawSettings.sorting.flags = SortFlags.CommonOpaque;
   
        filterSettings = new FilterRenderersSettings(true);

        //draw unlit opaque materials
        context.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);
        
        //reset Render Target
        //Graphics.SetRenderTarget(null);
        #endregion

        cameraBuffer.EndSample("First Pass");

        context.Submit();
        #endregion

        #region Compute Shader
        var tileMask = runTileMaskShader(camera.pixelWidth,camera.pixelHeight);

        var worldPosMap = runWorldPosMapShader(tileMask);
        #endregion

        //Vector2Int v = uv_to_px_coords(0.1f, 0.0f, 8, 1024, 512);
        //Debug.Log(v.ToString() + " -> " + px_to_uv_coords(v.x,v.y,1024,512).ToString());
        //Debug.Log(uv_to_px_coords(1.0f, 0.0f, 5, 1024, 512));


        #region Read-Back

        cameraBuffer.BeginSample("Read Back");

        cameraBuffer.Clear();

        Shader readBack = Shader.Find("Custom/ReadBack");
        Material secondPassMaterial = new Material(readBack);
        secondPassMaterial.SetTexture("_TextureAtlas", worldPosMap);

        Graphics.SetRenderTarget(finalImage);

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


        cameraBuffer.EndSample("Read Back");
        #endregion


        context.Submit();
        tileMask.Release();
        worldPosMap.Release();
        

        Graphics.Blit(finalImage, camera.activeTexture);
        //Graphics.Blit(baycentricCoords, camera.activeTexture);
        
        #endregion
    }

    //### other methodes #############################################################################################################

    RenderTexture runTileMaskShader(int width, int height) {
        Graphics.SetRenderTarget(null);
        var result = CreateIntermediateCSTarget(MAX_TEXTURE_SIZE/8, RenderTextureFormat.R8);

        tileMaskShader.SetTexture(tileMaskKernel, "Output", result);
        tileMaskShader.SetTexture(tileMaskKernel, "ID", rts[0]);
        tileMaskShader.SetTexture(tileMaskKernel, "UV", rts[1]);
        tileMaskShader.SetTexture(tileMaskKernel, "WorldPos", rts[2]);
        tileMaskShader.SetTexture(tileMaskKernel, "Normal", rts[3]);

        //Call compute shader
        tileMaskShader.Dispatch(tileMaskKernel, width / 8, height / 8, 1);

        #if DEBUG
        //debug for cs output
        Graphics.CopyTexture(result, tileMaskCopy);
        #endif
        return result;
    }

    RenderTexture runWorldPosMapShader(RenderTexture tileMask) {
        //get gameObject
        GameObject obj = GameObject.FindGameObjectWithTag("RenderObject");
        Vector3[] vertices = GetVertices(obj);

        ComputeBuffer vertexBuffer = new ComputeBuffer(vertices.Length, sizeof(float)*3);
        vertexBuffer.SetData(vertices);

        Graphics.SetRenderTarget(null);
        var result = CreateIntermediateCSTarget(MAX_TEXTURE_SIZE, RenderTextureFormat.ARGBFloat);

        worldPosShader.SetMatrix("localToWorldMatrix", obj.GetComponent<Renderer>().localToWorldMatrix);
        worldPosShader.SetTexture(worldPosKernel, "tileMask", tileMask);
        worldPosShader.SetTexture(worldPosKernel, "vertexIds", vertexIds);
        worldPosShader.SetTexture(worldPosKernel, "baycentCoords", baycentricCoords);
        worldPosShader.SetBuffer(worldPosKernel, "vertexPositions", vertexBuffer);
        worldPosShader.SetTexture(worldPosKernel, "Output", result);

        //Call compute shader
        worldPosShader.Dispatch(worldPosKernel, MAX_TEXTURE_SIZE * 2 / 8, MAX_TEXTURE_SIZE / 8, 1);

        #if DEBUG
        //debug for cs output
        Graphics.CopyTexture(result, worldPosMapCopy);
        #endif

        vertexBuffer.Release();
        return result;

    }

    RenderTexture CreateIntermediateCSTarget(int size, RenderTextureFormat rtFormat) {
        var result = new RenderTexture(size*2, size, 0, rtFormat, RenderTextureReadWrite.Default);
        result.enableRandomWrite = true;
        result.filterMode = FilterMode.Point;
        result.anisoLevel = 0;
        result.Create();

        return result;
    }

    Vector3[] GetVertices(GameObject go) { //Source: https://answers.unity.com/questions/697616/get-all-vertices-in-gameobject-to-array.html
        MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();
        List<Vector3> vList = new List<Vector3>();
        foreach (MeshFilter mf in mfs) {
            vList.AddRange(mf.sharedMesh.vertices);
        }
        return vList.ToArray();
    }

    Vector2Int uv_to_px_coords(float u, float v, int mip, int atlasWidth, int atlasHeight) {
        int widthAtLevel = atlasWidth/2 >> mip;
        int heightAtLevel = atlasHeight >> mip;

        int x = (int)(widthAtLevel * u);
        int y = (int)(heightAtLevel * v);

        int offset = atlasWidth - 2 * widthAtLevel;

        return new Vector2Int(x + offset, y);

    }

    Vector3 px_to_uv_coords(int x, int y, int atlasWidth, int atlasHeight) {
        int mip = 0;

        for (int w = atlasWidth / 2; x > w; w /= 2) {
            mip += (x > w) ? 1 : 0;
            x -= w;
        }

        int widthAtLevel = atlasWidth / 2 >> mip;
        int heightAtLevel = atlasHeight >> mip;

        int offset = atlasWidth - 2 * widthAtLevel;

        Debug.Log(x + " | " + offset);

        //x -= offset;

        float u = (float)x / widthAtLevel;
        float v = (float)y / heightAtLevel;

        return new Vector3(u, v, mip);

    }
}