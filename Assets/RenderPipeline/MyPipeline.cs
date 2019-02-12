// This is the render pipline for object space shading
//
// Render pipeline:
//      Preparation: 
//              uvRender:
//                  Renders vertexIds and baycentric coordinates in uv space for each object,
//                  Sets ID for each object and adds them to a list
//      Rendering:   
//              firstPass:
//                  Renders uv coordinates, mip level and id for each visible part of each object
//
//              runTileMaskShader:
//                  calculates which part of each texture is visible on screen 
//
//              runObjectSpaceComputeShader:
//                  shades visible textures in uv space
//          
//              readBack:
//                  Renders objects with shaded texture

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System;

public class MyPipeline : RenderPipeline {
    //### variables ################################################################################################################
    //Render pipeline variables
    CullResults cull;
    CommandBuffer cameraBuffer = new CommandBuffer {
        name = "Render Camera"
    };
    
    //statistics for tests
    public static long pixelCountStat = 0;
    public static long tileCountStat = 0;

    //texture constants
    public const int MIP_MAP_COUNT = 12;
    public const int MAX_TEXTURE_SIZE = 1 << MIP_MAP_COUNT;
    public const int TILE_SIZE = 64;


    //compute shader
    private ComputeShader tileMaskShader;
    private int tileMaskKernel;
    private ComputeShader worldPosShader;
    private int worldPosKernel;
    private ComputeShader clearTextureShader;
    private int clearTextureKernel;
    private ComputeShader countShader;
    private int countKernel;

    //Render targets for first pass shader and read back shader
    public static RenderTexture[] firstPassTargets;
    public static RenderTexture finalImage;

    //datastructure list of objects in scene 
    [System.Serializable]
    public class ObjData {
        public GameObject obj;
        public RenderTexture tileMask;
        public RenderTexture bayCent;
    }

    [SerializeField]
    public static List<ObjData> sceneObjects;

    //### Constructor ##############################################################################################################
    public MyPipeline() {
        //load compute shader
        tileMaskShader = (ComputeShader)Resources.Load("Shader/UVCoordReader");
        tileMaskKernel = tileMaskShader.FindKernel("CSMain");

        worldPosShader = (ComputeShader)Resources.Load("Shader/ObjectSpaceComputeShader");
        worldPosKernel = worldPosShader.FindKernel("CSMain");

        clearTextureShader = (ComputeShader)Resources.Load("Shader/ClearTexture");
        clearTextureKernel = clearTextureShader.FindKernel("CSMain");

        countShader = (ComputeShader)Resources.Load("Shader/CountRedPixels");
        countKernel = countShader.FindKernel("Count");

        int width = GameObject.FindObjectOfType<Camera>().pixelWidth;
        int height = GameObject.FindObjectOfType<Camera>().pixelHeight;

        //setup rendertargets for first pass
        firstPassTargets = new RenderTexture[4] {
            CreateRenderTexture(width, height, RenderTextureFormat.RGInt), //ID and Mip Map
            CreateRenderTexture(width, height, RenderTextureFormat.RGFloat), //UV
            CreateRenderTexture(width, height, RenderTextureFormat.ARGB32), //WorldPos
            CreateRenderTexture(width, height, 32, RenderTextureFormat.Depth), //Depth
        };

        //setup rendertargets for read back
        finalImage = CreateRenderTexture(width, height, 32, RenderTextureFormat.ARGBFloat);

    }


    //### Rendering #######################################################################################################
    //called by unity at the beginning of each frame
    //Input:    renderContext:  ScriptableRenderContext
    //          cameras:        array of cameras in scene
    public override void Render(ScriptableRenderContext renderContext, Camera[] cameras) {
        //Stop rendering when game is not running (because it causes to much bugs)
        if (!Application.isPlaying) { return; }

        //calling base render methode
        base.Render(renderContext, cameras);

        //call new render methode for every camera
        foreach (var camera in cameras) {
            Render(renderContext, camera);
        }
    }

    //called once for every camera by methode above
    //this methode calls the all methodes mention in description in order
    //Input:    renderContext:  ScriptableRenderContext
    //          camera:         Camera in scene
    public void Render(ScriptableRenderContext context, Camera camera) {
        //statistics
        pixelCountStat = 0;
        tileCountStat = 0;

        //setup rendercontext for camera
        context.SetupCameraProperties(camera);

        //culling ______________________________________________________________________________________________________
        ScriptableCullingParameters cullingParameters;
        if (!CullResults.GetCullingParameters(camera, out cullingParameters)) {
            return;
        }

        CullResults.Cull(ref cullingParameters, context, ref cull);


        //UV Renderer __________________________________________________________________________________________________
        //Renders vertexIds and baycentric coordinates in uv space for each object´if not already done
        if (sceneObjects == null) {
            sceneObjects = UVRenderer(context, camera);
        }

        //First pass ___________________________________________________________________________________________________
        //Renders uv coordinates, mip level and id for each visible part of each object
        FirstPass(context, camera);

        //Compute shader _______________________________________________________________________________________________
        foreach (ObjData obj in sceneObjects) {
            //calculate which part of each texture is visible on screen 
            RunTileMaskShader(obj, firstPassTargets[0], firstPassTargets[1]);
            //shade visible textures in uv space
            RunObjectSpaceComputeShader(obj);
        }

        //Read-Back ____________________________________________________________________________________________________
        //Render objects with shaded textures 
        ReadBack(context, camera);

        //reset render target
        Graphics.SetRenderTarget(null);
        

    }

    //### main render methodes #############################################################################################################

    //this methode reenders vertexIds and baycentric coordinates in uv space for each object
    //it's called by the Render(...) at the beginning
    //Input:    renderContext:  ScriptableRenderContext
    //          camera:         Camera in scene
    //Output:   List<ObjData>, list of all objects with renderer in scene
    List<ObjData> UVRenderer(ScriptableRenderContext context, Camera camera) {//initialize list
        List<ObjData> sceneObjects = new List<ObjData>();       //used as return value

        //object id

        int id = 0;
        //iterate objects
        foreach (MeshFilter mesh in GameObject.FindObjectsOfType<MeshFilter>()) {
            //setup _______________________________________________________________________________________________________________________________________________
            //set camera to only render uv render layer
            int camCullMask = camera.cullingMask;
            camera.cullingMask = 1 << 9;
            context.SetupCameraProperties(camera);

            //setup struct for object
            ObjData obj = new ObjData();
            obj.obj = mesh.gameObject;
            obj.obj.GetComponent<Renderer>().material.SetInt("_ID", id++);
            obj.tileMask = CreateRenderTexture(MAX_TEXTURE_SIZE / TILE_SIZE*2, MAX_TEXTURE_SIZE / TILE_SIZE, RenderTextureFormat.R8);

            //move object to uv render layer
            int objLayer = obj.obj.layer;
            obj.obj.layer = 9;

            //Initilize RenderTexture for baycentric coordinate/vertex id
            RenderTexture bayCent = new RenderTexture(MAX_TEXTURE_SIZE, MAX_TEXTURE_SIZE, 0, RenderTextureFormat.ARGB32);
            bayCent.filterMode = FilterMode.Point;
            bayCent.anisoLevel = 0;
            bayCent.Create();

            RenderTexture vertexIds = new RenderTexture(MAX_TEXTURE_SIZE, MAX_TEXTURE_SIZE, 24, RenderTextureFormat.ARGBInt);
            vertexIds.filterMode = FilterMode.Point;
            vertexIds.anisoLevel = 0;
            vertexIds.Create();

            obj.obj.GetComponent<Renderer>().material.SetTexture("_TextureAtlas", CreateRenderTexture(MAX_TEXTURE_SIZE*2, MAX_TEXTURE_SIZE, RenderTextureFormat.ARGB32));            

            //culling _____________________________________________________________________________________________________________________________________________
            ScriptableCullingParameters cullingParameters;
            if (!CullResults.GetCullingParameters(camera, out cullingParameters)) {
                return null;
            }

            CullResults.Cull(ref cullingParameters, context, ref cull);


            //set render target ___________________________________________________________________________________________________________________________________
            RenderBuffer[] cBuffer = new RenderBuffer[2] { bayCent.colorBuffer, vertexIds.colorBuffer };
            camera.SetTargetBuffers(cBuffer, bayCent.depthBuffer);

            //clearing ____________________________________________________________________________________________________________________________________________
            //clearing render target
            cameraBuffer.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));
            context.ExecuteCommandBuffer(cameraBuffer);
            cameraBuffer.Clear();

            //drawing _____________________________________________________________________________________________________________________________________________
            //setting
            DrawRendererSettings drawSettings = new DrawRendererSettings(camera, new ShaderPassName("SRPDefaultUnlit"));
            drawSettings.sorting.flags = SortFlags.CommonOpaque;

            FilterRenderersSettings filterSettings = new FilterRenderersSettings(true);

            drawSettings.SetOverrideMaterial(new Material(Shader.Find("Custom/UVRenderer")), 0);

            //draw unlit opaque materials
            context.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);           
            context.Submit();


            //finish _____________________________________________________________________________________________________________________________________________
            //move object and camera back to original layer
            obj.obj.layer = objLayer;
            camera.cullingMask = camCullMask;

            //set object properties
            obj.obj.GetComponent<Renderer>().material.SetTexture("_BaycentCoords", bayCent);
            obj.bayCent = bayCent;
            obj.obj.GetComponent<Renderer>().material.SetTexture("_VertexIDs", vertexIds);

            //add object to result list
            sceneObjects.Add(obj);

            //reset render taget
            Graphics.SetRenderTarget(null);
        }

        //return list
        return sceneObjects;
    }

    //this methode renders uv coordinates, mip level and id for every visible part of every visible object on screen
    //it's called by the Render(...) every frame
    //Input:    renderContext:  ScriptableRenderContext
    //          camera:         Camera in scene
    void FirstPass(ScriptableRenderContext context, Camera camera) {
        //set render target
        camera.SetTargetBuffers(new RenderBuffer[3] { firstPassTargets[1].colorBuffer, firstPassTargets[0].colorBuffer, firstPassTargets[2].colorBuffer }, firstPassTargets[3].depthBuffer);

        //clearing _____________________________________________________________________________________________________________________________________________
        CameraClearFlags clearFlags = camera.clearFlags;
        cameraBuffer.ClearRenderTarget(
            (clearFlags & CameraClearFlags.Depth) != 0,
            (clearFlags & CameraClearFlags.Color) != 0,
            new Color(0, 0, 1, 0) //camera.backgroundColor
        );

        context.ExecuteCommandBuffer(cameraBuffer);
        cameraBuffer.Clear();

        // drawing _____________________________________________________________________________________________________________________________________________
        //setup settings
        DrawRendererSettings drawSettings = new DrawRendererSettings(camera, new ShaderPassName("SRPDefaultUnlit"));
        drawSettings.sorting.flags = SortFlags.CommonOpaque;
        FilterRenderersSettings filterSettings = new FilterRenderersSettings(true);

        //draw
        context.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);
        context.Submit();

        Graphics.SetRenderTarget(null);
    }

    //this methode uses a compute shader to create a mask of which part of the texture of object obj is visible
    //it's called by the Render(...) every frame for every object
    //Input:    obj:            ObjData, obj, for which the tilemask should be created
    //          renderContext:  ScriptableRenderContext
    //          camera:         Camera in scene
    void RunTileMaskShader(ObjData obj, RenderTexture IDandMip, RenderTexture uv) {
        //clear tilemask _____________________________________________________________________________________________________________________________________________
        clearTextureShader.SetTexture(clearTextureKernel, "Texture", obj.tileMask);
        clearTextureShader.Dispatch(clearTextureKernel, obj.tileMask.width / 8, obj.tileMask.height / 8, 1);
        
        //setup shader parameters ____________________________________________________________________________________________________________________________________
        tileMaskShader.SetTexture(tileMaskKernel, "Output", obj.tileMask);
        tileMaskShader.SetTexture(tileMaskKernel, "IDandMip", IDandMip);
        tileMaskShader.SetTexture(tileMaskKernel, "UV", uv);
        tileMaskShader.SetInt("ID", obj.obj.GetComponent<Renderer>().material.GetInt("_ID"));

        //setup variables for statistic use
        int[] pixelStats = new int[1];
        ComputeBuffer statBuffer = new ComputeBuffer(pixelStats.Length, sizeof(int));
        statBuffer.SetData(pixelStats);
        tileMaskShader.SetBuffer(tileMaskKernel, "Stats", statBuffer);

        //Call compute shader ________________________________________________________________________________________________________________________________________
        tileMaskShader.Dispatch(tileMaskKernel, IDandMip.width / 8, IDandMip.height / 8, 1);

        //statistics _________________________________________________________________________________________________________________________________________________
        //pixel statistic
        statBuffer.GetData(pixelStats);
        pixelCountStat += pixelStats[0];
        statBuffer.Release();
        statBuffer = null;

        //tile statistic
        int[] tileStats = new int[1];
        ComputeBuffer tileStatBuffer = new ComputeBuffer(tileStats.Length, sizeof(int));
        tileStatBuffer.SetData(tileStats);

        //cs to count red pixels
        countShader.SetBuffer(countKernel, "Output", tileStatBuffer);
        countShader.SetTexture(countKernel, "Input", obj.tileMask);      
        countShader.Dispatch(countKernel, obj.tileMask.width / 8, obj.tileMask.height / 8, 1);

        tileStatBuffer.GetData(tileStats);

        tileCountStat += tileStats[0];

        tileStatBuffer.GetData(pixelStats);
        tileStatBuffer.Release();
        tileStatBuffer = null;

    }

    //this methode shades the objects in uv space
    //it's called by the Render(...) every frame for every object
    //Input:    obj:            ObjData, obj, for which the tilemask should be created
    //          renderContext:  ScriptableRenderContext
    //          camera:         Camera in scene
    void RunObjectSpaceComputeShader(ObjData obj) {
        //vertex location buffer
        Vector3[] locPos = GetVertices(obj.obj);
        ComputeBuffer locationBuffer = new ComputeBuffer(locPos.Length, sizeof(float) * 3);
        locationBuffer.SetData(locPos);

        //vertex normal buffer
        Vector3[] normals = GetNormals(obj.obj);
        ComputeBuffer normalBuffer = new ComputeBuffer(normals.Length, sizeof(float) * 3);
        normalBuffer.SetData(normals);
       
        //setup cs parameters
        worldPosShader.SetMatrix("localToWorldMatrix", obj.obj.GetComponent<Renderer>().localToWorldMatrix);
        worldPosShader.SetTexture(worldPosKernel, "tileMask", obj.tileMask);
        worldPosShader.SetTexture(worldPosKernel, "vertexIds", obj.obj.GetComponent<Renderer>().material.GetTexture("_VertexIDs"));
        worldPosShader.SetTexture(worldPosKernel, "objectTexture", obj.obj.GetComponent<Renderer>().material.GetTexture("_Texture"));
        worldPosShader.SetTexture(worldPosKernel, "baycentCoords", obj.obj.GetComponent<Renderer>().material.GetTexture("_BaycentCoords"));
        worldPosShader.SetBuffer(worldPosKernel, "vertexPositions", locationBuffer);
        worldPosShader.SetBuffer(worldPosKernel, "vertexNormals", normalBuffer);
        worldPosShader.SetTexture(worldPosKernel, "textureTiles", obj.obj.GetComponent<Renderer>().material.GetTexture("_TextureAtlas"));

        //Call compute shader
        worldPosShader.Dispatch(worldPosKernel, MAX_TEXTURE_SIZE * 2 / 8, MAX_TEXTURE_SIZE / 8, 1);

        //release buffer
        locationBuffer.Release();
        normalBuffer.Release();
    }

    //this methode renders the scene with the now shaded textures
    //it's called by the Render(...) every frame
    //Input:    renderContext:  ScriptableRenderContext
    //          camera:         Camera in scene
    void ReadBack(ScriptableRenderContext context, Camera camera) {
        //setup _____________________________________________________________________________________________________________________________________________
        //reset camera buffer
        cameraBuffer.Clear();

        //set object shader to read back
        foreach (ObjData obj in sceneObjects) {
            Material mat = obj.obj.GetComponent<Renderer>().material;
            mat.DisableKeyword("_FIRST_PASS");
            mat.EnableKeyword("_READ_BACK");
        }

        //set new render target
        Graphics.SetRenderTarget(finalImage);

        //culling ___________________________________________________________________________________________________________________________________________
        ScriptableCullingParameters cullingParameters;
        if (!CullResults.GetCullingParameters(camera, out cullingParameters)) {
            return;
        }
        CullResults.Cull(ref cullingParameters, context, ref cull);      

        //clearing ___________________________________________________________________________________________________________________________________________
        cameraBuffer.ClearRenderTarget(true, true, camera.backgroundColor);
        context.ExecuteCommandBuffer(cameraBuffer);
        cameraBuffer.Clear();

        //drawing ____________________________________________________________________________________________________________________________________________
        //setup settings for rendering
        DrawRendererSettings drawSettings = new DrawRendererSettings(camera, new ShaderPassName("SRPDefaultUnlit"));
        drawSettings.sorting.flags = SortFlags.CommonOpaque;
        FilterRenderersSettings filterSettings = new FilterRenderersSettings(true);

        //draw
        context.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);
        context.Submit();

        //finish _____________________________________________________________________________________________________________________________________________
        //set object shader back to firstpass
        foreach (ObjData obj in sceneObjects) {
            Material mat = obj.obj.GetComponent<Renderer>().material;
            mat.EnableKeyword("_FIRST_PASS");
            mat.DisableKeyword("_READ_BACK");
        }

        //display image on screen
        Graphics.Blit(finalImage, camera.activeTexture);
    }

    //### other methodes #############################################################################################################


    //This methode creates a rendertexture with all parameters set
    //Input:    width:      int, width of new rendertexture
    //          height:     int, height of new rendertexture
    //          depth:      int, depth of new rendertexture
    //          reFormat:   RenderTextureFormat, format of new rendertexture
    //Output:   rendertexture
    RenderTexture CreateRenderTexture(int width, int height, int depth, RenderTextureFormat rtFormat) {
        var result = new RenderTexture(width, height, depth, rtFormat, RenderTextureReadWrite.Default);
        if (rtFormat != RenderTextureFormat.Depth) {
            result.enableRandomWrite = true;
        }
        result.filterMode = FilterMode.Point;
        result.anisoLevel = 0;
        result.Create();

        return result;
    }

    //This methode creates a rendertexture with depht 0 by calling the methode above
    //Input:    width:      int, width of new rendertexture
    //          height:     int, height of new rendertexture
    //          reFormat:   RenderTextureFormat, format of new rendertexture
    //Output:   rendertexture
    RenderTexture CreateRenderTexture(int width, int height, RenderTextureFormat rtFormat) {
        return CreateRenderTexture(width, height, 0, rtFormat);
    }

    //This methode returns the vertex location buffer of a gameobject as array
    //Input:    go: Gameobject, object of which we want to get the location buffer
    //Output:   array of Vector3, vertex locations
    //Source: https://answers.unity.com/questions/697616/get-all-vertices-in-gameobject-to-array.html
    Vector3[] GetVertices(GameObject go) { 
        MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();
        List<Vector3> vList = new List<Vector3>();
        foreach (MeshFilter mf in mfs) {
            vList.AddRange(mf.mesh.vertices);
        }
        return vList.ToArray();
    }

    //This methode returns the vertex normal buffer of a gameobject as array
    //Input:    go: Gameobject, object of which we want to get the normal buffer
    //Output:   array of Vector3, vertex normals
    //Based on source: https://answers.unity.com/questions/697616/get-all-vertices-in-gameobject-to-array.html
    Vector3[] GetNormals(GameObject go) { 
        MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();
        List<Vector3> vList = new List<Vector3>();
        foreach (MeshFilter mf in mfs) {
            vList.AddRange(mf.mesh.normals);
        }
        return vList.ToArray();
    }

    //This methode saves a rendertexture as a .png in the folder Logs
    //Input:    name:   string, name of the file (.png will be added)
    //          rt:     RenderTexture, texture to be saved
    public static void SaveTexture(string name, RenderTexture rt) { //source: https://answers.unity.com/questions/37134/is-it-possible-to-save-rendertextures-into-png-fil.html
        byte[] bytes = ToTexture2D(rt).EncodeToPNG();
        System.IO.File.WriteAllBytes(Environment.CurrentDirectory+ "\\Logs\\" + name+".png", bytes);
        Debug.Log("Saving Texture: " + name + ".png");
    }

    //This methode converts a Rendertexture to a texture, used by SaveTexture(...)
    //Input:    rTex:       RenderTexture, rendertexture that should be converted
    //Output:   Texture2D:  converted texture
    static Texture2D ToTexture2D(RenderTexture rTex)  {//source: https://answers.unity.com/questions/37134/is-it-possible-to-save-rendertextures-into-png-fil.html
        Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.ARGB32, false);
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }

    //methodes for putput log
    //stringbuilder to store output log before saving
    static StringBuilder output;
  
    //This methode adds a message to the output log
    //Input:    arguments:  Array of Strings, message that should be added to the output log
    public static void Log(params string[] arguments) {
        if (output == null) {
            output = new StringBuilder();
        }
        foreach(string a in arguments) {
            output.Append(a);
        }
    }
    
    //This methode adds the current statistics about shaded pixel/tiles to the output log
    //Needed for first test and called by FirstTestCamera
    public static void LogPixelStats() {
        if (output == null) {
            output = new StringBuilder();
        }
        Camera cam = GameObject.FindObjectOfType<Camera>();
        
        output.Append(Time.frameCount).Append(",").Append(pixelCountStat).Append(",").Append(tileCountStat).Append(",").Append(cam.pixelWidth).Append(",").Append(cam.pixelHeight).Append(",").Append(MyPipeline.TILE_SIZE).Append("\n");
    }

    //This methode saves the log to a .csv file
    //Input:    name:   string, name of the file (.csv, date and other parameters will be added)
    public static void SaveLogCSV(string name) {
        if (output == null) {
            Debug.Log("Saving failed");
            return;
        }

        Camera cam = GameObject.FindObjectOfType<Camera>();
        DateTime time = System.DateTime.Now;
        string s = name + "_" + time.Year + "-" + time.Month + "-" + time.Day + "_" + (time.Hour <= 9 ? "0" : "") + time.Hour + (time.Minute<=9?"0":"") + time.Minute + "_" + cam.pixelWidth +"x"+cam.pixelHeight+"_"+TILE_SIZE+".csv";

        File.AppendAllText(Environment.CurrentDirectory+ "\\Logs\\" +s, output.ToString());
        Debug.Log("Saved as " + s);
        ResetLog();
    }

    //This methode clears the output log
    public static void ResetLog() {
        output = null;
    }
}