﻿//based on tutorial: https://catlikecoding.com/unity/tutorials/scriptable-render-pipeline/custom-pipeline/

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
    public const int MIP_MAP_COUNT = 12;
    public const int MAX_TEXTURE_SIZE = 1 << MIP_MAP_COUNT;

    //compute shader for mapping the normal/world position to a texture atlas
    private ComputeShader tileMaskShader;
    private int tileMaskKernel;
    private ComputeShader worldPosShader;
    private int worldPosKernel;
    private ComputeShader clearTextureShader;
    private int clearTextureKernel;

    public static RenderTexture[] firstPassTargets;
    public static RenderTexture finalImage;

    //scene objects
    [System.Serializable]
    public class ObjData {
        public int id;
        public GameObject obj;
        public RenderTexture baycentricCoords;
        public RenderTexture vertexIds;
        public RenderTexture tileMask;
        public RenderTexture worldPosMap;
        public RenderTexture normalMap;
        public RenderTexture textureTiles;
        public Texture texture;
    }

    [SerializeField]
    public static List<ObjData> sceneObjects;

    //### Constructor ##############################################################################################################
    public MyPipeline() {
        //compute shader
        tileMaskShader = (ComputeShader)Resources.Load("Shader/UVCoordReader");
        tileMaskKernel = tileMaskShader.FindKernel("CSMain");

        worldPosShader = (ComputeShader)Resources.Load("Shader/WorldPosOSS");
        worldPosKernel = worldPosShader.FindKernel("CSMain");

        clearTextureShader = (ComputeShader)Resources.Load("Shader/ClearTexture");
        clearTextureKernel = worldPosShader.FindKernel("CSMain");

        int width = GameObject.FindObjectOfType<Camera>().pixelWidth;
        int height = GameObject.FindObjectOfType<Camera>().pixelHeight;

        //firstPass renderTargets
        firstPassTargets = new RenderTexture[4] {
            new RenderTexture(width, height, 0, RenderTextureFormat.RGInt), //ID and Mip Map
            new RenderTexture(width, height, 0, RenderTextureFormat.RGFloat), //UV
            new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32), //WorldPos
            new RenderTexture(width, height, 0, RenderTextureFormat.Depth), //Depth
        };

        for (int i = 0; i < firstPassTargets.Length; i++) {
            firstPassTargets[i].filterMode = FilterMode.Point;
            firstPassTargets[i].anisoLevel = 0;
            firstPassTargets[i].Create();
        }


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
        if (!Application.isPlaying) { return; }
        Debug.Log(Time.realtimeSinceStartup + ": " + (1 / Time.deltaTime));

        base.Render(renderContext, cameras);

        //call new render methode for every camera
        foreach (var camera in cameras) {
            Render(renderContext, camera);
        }
    }

    //called once for every camera
    public void Render(ScriptableRenderContext context, Camera camera) {
        context.SetupCameraProperties(camera);

        //cull objects not on screen
        #region culling
        ScriptableCullingParameters cullingParameters;
        if (!CullResults.GetCullingParameters(camera, out cullingParameters)) {
            return;
        }

        CullResults.Cull(ref cullingParameters, context, ref cull);
        
        #endregion


        //UV Renderer __________________________________________________________________________________________________
        if (sceneObjects == null) {
            sceneObjects = uvRenderer(context, camera);
        }

        //First pass ___________________________________________________________________________________________________
        firstPass(context, camera);

        //Compute shader _______________________________________________________________________________________________
        foreach (ObjData obj in sceneObjects) {
            runTileMaskShader(obj, firstPassTargets[0], firstPassTargets[1]);
            runWorldPosMapShader(obj);
        }

        /*if (count == 10) {
            SaveTexture("tileMask", 2048, 1024, sceneObjects[0].tileMask);
            SaveTexture("worldPos", 2 * 8192, 8192, sceneObjects[0].worldPosMap);
        }
        count++;*/

        //Read-Back ____________________________________________________________________________________________________
        readBack(context, camera);

        //release rendertextures not in use
        Graphics.SetRenderTarget(null);
        

    }

    //### main render methodes #############################################################################################################

    List<ObjData> uvRenderer(ScriptableRenderContext context, Camera camera) {
        //setup
        DrawRendererSettings drawSettings;
        FilterRenderersSettings filterSettings;

        //initialize list
        List<ObjData> sceneObjects = new List<ObjData>();

        int id = 0;
        foreach (MeshFilter mesh in GameObject.FindObjectsOfType<MeshFilter>()) {
            int camCullMask = camera.cullingMask;
            camera.cullingMask = 1 << 9;
            context.SetupCameraProperties(camera);




            ObjData obj = new ObjData();
            obj.obj = mesh.gameObject;
            obj.id = id;
            id++;
            obj.texture = obj.obj.GetComponent<Renderer>().material.GetTexture("_Texture");

            int objLayer = obj.obj.layer;
            obj.obj.layer = 9;

            #region culling
            ScriptableCullingParameters cullingParameters;
            if (!CullResults.GetCullingParameters(camera, out cullingParameters)) {
                return null;
            }

            CullResults.Cull(ref cullingParameters, context, ref cull);


            #endregion


            obj.obj.GetComponent<Renderer>().material.SetInt("_ID", obj.id);

            //Initilize RenderTexture for baycentric coordinate/vertex id
            obj.baycentricCoords = new RenderTexture(MAX_TEXTURE_SIZE, MAX_TEXTURE_SIZE, 0, RenderTextureFormat.ARGBFloat);
            obj.baycentricCoords.filterMode = FilterMode.Point;
            obj.baycentricCoords.anisoLevel = 0;
            obj.baycentricCoords.Create();

            obj.vertexIds = new RenderTexture(MAX_TEXTURE_SIZE, MAX_TEXTURE_SIZE, 24, RenderTextureFormat.ARGBInt);
            obj.vertexIds.filterMode = FilterMode.Point;
            obj.vertexIds.anisoLevel = 0;
            obj.vertexIds.Create();

            /*obj.debug = new RenderTexture(MAX_TEXTURE_SIZE, MAX_TEXTURE_SIZE, 24, RenderTextureFormat.ARGBInt);
            obj.debug.enableRandomWrite = true;
            obj.debug.filterMode = FilterMode.Point;
            obj.debug.anisoLevel = 0;
            obj.debug.Create();*/


            //cameraBuffer.Clear();

            Shader uvRenderer = Shader.Find("Custom/UVRenderer");
            Material uvRendererMaterial = new Material(uvRenderer);


            //set render target
            RenderBuffer[] cBuffer = new RenderBuffer[2] { obj.baycentricCoords.colorBuffer, obj.vertexIds.colorBuffer };

            camera.SetTargetBuffers(cBuffer, obj.baycentricCoords.depthBuffer);
            //Graphics.SetRenderTarget(obj.baycentricCoords);

            #region clearing
            cameraBuffer.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));

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

            
            #endregion
            context.Submit();

            obj.obj.layer = objLayer;
            camera.cullingMask = camCullMask;

            sceneObjects.Add(obj);

            
            Graphics.SetRenderTarget(null);
        }

        return sceneObjects;
    }

    void firstPass(ScriptableRenderContext context, Camera camera) {
        #region First Pass

        DrawRendererSettings drawSettings;
        FilterRenderersSettings filterSettings;

        //sample call for frame debugger
        cameraBuffer.BeginSample("First Pass");

        //set render target
        camera.SetTargetBuffers(new RenderBuffer[3] { firstPassTargets[1].colorBuffer, firstPassTargets[0].colorBuffer, firstPassTargets[2].colorBuffer }, firstPassTargets[3].depthBuffer);

        //clear render target
        #region clearing
        CameraClearFlags clearFlags = camera.clearFlags;
        cameraBuffer.ClearRenderTarget(
            (clearFlags & CameraClearFlags.Depth) != 0,
            (clearFlags & CameraClearFlags.Color) != 0,
            camera.backgroundColor//new Color(0, 0, 0, 0)
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

        #endregion

        cameraBuffer.EndSample("First Pass");

        context.Submit();
        #endregion

        Graphics.SetRenderTarget(null);
    }

    void runTileMaskShader(ObjData obj, RenderTexture IDandMip, RenderTexture uv) {
        Graphics.SetRenderTarget(null);

        //compue shader parameters

        if (obj.tileMask == null) {
            obj.tileMask = CreateIntermediateCSTarget(MAX_TEXTURE_SIZE / 8, RenderTextureFormat.R8);
        } else {
            clearTextureShader.SetTexture(clearTextureKernel, "Result", obj.tileMask);
            clearTextureShader.Dispatch(clearTextureKernel, obj.tileMask.width / 8, obj.tileMask.height / 8, 1);
        };

        tileMaskShader.SetTexture(tileMaskKernel, "Output", obj.tileMask);
        tileMaskShader.SetTexture(tileMaskKernel, "IDandMip", IDandMip);
        tileMaskShader.SetTexture(tileMaskKernel, "UV", uv);
        tileMaskShader.SetInt("ID", obj.id);
        //tileMaskShader.SetTexture(tileMaskKernel, "Debug", sceneObjects[0].debug);

        //Call compute shader
        tileMaskShader.Dispatch(tileMaskKernel, IDandMip.width / 16, IDandMip.height / 16, 1);
        
    }

    void runWorldPosMapShader(ObjData obj) {
        //get gameObject location
        Vector3[] locPos = GetVertices(obj.obj);
        ComputeBuffer locationBuffer = new ComputeBuffer(locPos.Length, sizeof(float) * 3);
        locationBuffer.SetData(locPos);

        //get normals
        Vector3[] normals = GetNormals(obj.obj);
        ComputeBuffer normalBuffer = new ComputeBuffer(normals.Length, sizeof(float) * 3);
        normalBuffer.SetData(normals);

        Graphics.SetRenderTarget(null);
        if (obj.worldPosMap == null) { obj.worldPosMap = CreateIntermediateCSTarget(MAX_TEXTURE_SIZE, RenderTextureFormat.ARGBFloat); }
        if (obj.normalMap == null) { obj.normalMap = CreateIntermediateCSTarget(MAX_TEXTURE_SIZE, RenderTextureFormat.ARGBFloat); }
        if (obj.textureTiles == null) { obj.textureTiles = CreateIntermediateCSTarget(MAX_TEXTURE_SIZE, RenderTextureFormat.ARGBFloat); }

        worldPosShader.SetMatrix("localToWorldMatrix", obj.obj.GetComponent<Renderer>().localToWorldMatrix);
        worldPosShader.SetTexture(worldPosKernel, "tileMask", obj.tileMask);
        worldPosShader.SetTexture(worldPosKernel, "vertexIds", obj.vertexIds);
        worldPosShader.SetTexture(worldPosKernel, "Texture", obj.texture);
        worldPosShader.SetTexture(worldPosKernel, "baycentCoords", obj.baycentricCoords);
        worldPosShader.SetBuffer(worldPosKernel, "vertexPositions", locationBuffer);
        worldPosShader.SetBuffer(worldPosKernel, "vertexNormals", normalBuffer);
        worldPosShader.SetTexture(worldPosKernel, "WorldPos", obj.worldPosMap);
        worldPosShader.SetTexture(worldPosKernel, "Normals", obj.normalMap);
        worldPosShader.SetTexture(worldPosKernel, "TextureTiles", obj.textureTiles);

        //Call compute shader
        worldPosShader.Dispatch(worldPosKernel, MAX_TEXTURE_SIZE * 2 / 16, MAX_TEXTURE_SIZE / 16, 1);

        locationBuffer.Release();
        normalBuffer.Release();

        //SaveTexture("worldPos2", result);

    }

    void readBack(ScriptableRenderContext context, Camera camera) {

        cameraBuffer.Clear();

        foreach (ObjData obj in sceneObjects) {
            Material mat = obj.obj.GetComponent<Renderer>().material;
            mat.SetTexture("_TextureAtlas", obj.textureTiles);
            mat.DisableKeyword("_FIRST_PASS");
            mat.EnableKeyword("_READ_BACK");
        }

        DrawRendererSettings drawSettings;
        FilterRenderersSettings filterSettings;

        #region culling
        ScriptableCullingParameters cullingParameters;
        if (!CullResults.GetCullingParameters(camera, out cullingParameters)) {
            return;
        }

        CullResults.Cull(ref cullingParameters, context, ref cull);


        #endregion
        Graphics.SetRenderTarget(finalImage);

        #region clearing
        cameraBuffer.ClearRenderTarget(true, true, camera.backgroundColor);


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


        #endregion

        context.Submit();

        foreach (ObjData obj in sceneObjects) {
            Material mat = obj.obj.GetComponent<Renderer>().material;
            mat.EnableKeyword("_FIRST_PASS");
            mat.DisableKeyword("_READ_BACK");
        }

        Graphics.Blit(finalImage, camera.activeTexture);
    }

    //### other methodes #############################################################################################################


    //returns rendertexture
    RenderTexture CreateIntermediateCSTarget(int size, RenderTextureFormat rtFormat) {
        var result = new RenderTexture(size * 2, size, 0, rtFormat, RenderTextureReadWrite.Default);
        result.enableRandomWrite = true;
        result.filterMode = FilterMode.Point;
        result.anisoLevel = 0;
        result.Create();

        return result;
    }

    //returns vertices of the gameobject go
    Vector3[] GetVertices(GameObject go) { //Source: https://answers.unity.com/questions/697616/get-all-vertices-in-gameobject-to-array.html
        MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();
        List<Vector3> vList = new List<Vector3>();
        foreach (MeshFilter mf in mfs) {
            vList.AddRange(mf.mesh.vertices);
        }
        return vList.ToArray();
    }

    Vector3[] GetNormals(GameObject go) { //Source: https://answers.unity.com/questions/697616/get-all-vertices-in-gameobject-to-array.html
        MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();
        List<Vector3> vList = new List<Vector3>();
        foreach (MeshFilter mf in mfs) {
            vList.AddRange(mf.mesh.normals);
        }
        return vList.ToArray();
    }

    public void SaveTexture(string name, int width, int height, RenderTexture rt) { //source: https://answers.unity.com/questions/37134/is-it-possible-to-save-rendertextures-into-png-fil.html
        byte[] bytes = toTexture2D(rt, width, height).EncodeToPNG();
        System.IO.File.WriteAllBytes("C:/Users/TIm/Desktop/"+name+".png", bytes);
    }

    Texture2D toTexture2D(RenderTexture rTex, int width, int height)  {//source: https://answers.unity.com/questions/37134/is-it-possible-to-save-rendertextures-into-png-fil.html
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }
}