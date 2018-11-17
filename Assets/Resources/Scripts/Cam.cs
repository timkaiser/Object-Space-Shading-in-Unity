//parts taken from: https://github.com/rcm4/Unity-MRT/tree/master/MRT/Assets/Scripts

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Cam : MonoBehaviour {
    //object parameters
    [SerializeField]
    private static int currentUnusedIndex = 1;
    [SerializeField]
    private static List<int> textureDimensions = new List<int>();

    private static List<GameObject> gameObjects = new List<GameObject>();

    public const int MIP_MAP_COUNT = 11;
    public const int MAX_TEXTURE_SIZE = 1 << MIP_MAP_COUNT;

    //Enum to make switch between aktive RenderTargets in Inspector easier
    public enum RT {
        ID = 0,
        UV = 1,
        WorldPosition = 2,
        Normal = 3,
    }
    //Number of current active Render Target
    public RT rt;

    //Main Camera
    private Camera sourceCamera;

    //Render Targets the Camera renders to
    [SerializeField]
    private RenderTexture[] rts;            //Render Targets for the camera
    private RenderBuffer[] colorBuffers;    //ColorBuffers of the RenderTextures
    private RenderTexture depthBuffer;      //DepthBuffer for the Camera

    //debug for cs output
    public RenderTexture[] CSoutputCopy;

    //compute shader for mapping the normal/world position to a texture atlas
    private ComputeShader debugCS;
    private int CSkernel;

    //Called on start
    void Start() {
        //get main camera
        this.sourceCamera = this.GetComponent<Camera>();

        int width = sourceCamera.pixelWidth;
        int height = sourceCamera.pixelHeight;

        //Initialize Render Textures
        this.rts = new RenderTexture[4] {
            new RenderTexture(width, height, 0, RenderTextureFormat.RInt), //ID
            new RenderTexture(width, height, 0, RenderTextureFormat.RG16), //UV
            new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat), //World Position
            new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32), //Normal
        };

        //get color buffers from rendertextures
        this.colorBuffers = new RenderBuffer[4] { rts[0].colorBuffer, rts[1].colorBuffer, rts[2].colorBuffer, rts[3].colorBuffer};

        //create depth buffer
        this.depthBuffer = new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 32, RenderTextureFormat.Depth);
        this.depthBuffer.Create();


        //debug for cs output
        CSoutputCopy = new RenderTexture[MIP_MAP_COUNT];
        for (int i = 0; i < MIP_MAP_COUNT; ++i) {
            CSoutputCopy[i] = new RenderTexture(MAX_TEXTURE_SIZE, MAX_TEXTURE_SIZE, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            CSoutputCopy[i].filterMode = FilterMode.Point;
            CSoutputCopy[i].Create();
        }

        debugCS = (ComputeShader)Instantiate(Resources.Load("Shader/DebugCS"));
        CSkernel = debugCS.FindKernel("DebugCS");
    }

    //Called while image is rendered (?)
    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        sourceCamera.SetTargetBuffers(this.colorBuffers, this.depthBuffer.depthBuffer);

        Graphics.Blit(rts[rt.GetHashCode()], destination);

    }
    //Called after Image is rendered
    private void OnPostRender() {
        Graphics.SetRenderTarget(null);
        var result = CreateIntermediateTarget();

        debugCS.SetTexture(CSkernel, "Output", result);
        debugCS.SetTexture(CSkernel, "ID", rts[(int)RT.ID]);
        debugCS.SetTexture(CSkernel, "UV", rts[(int)RT.UV]);
        debugCS.SetTexture(CSkernel, "WorldPos", rts[(int)RT.WorldPosition]);
        debugCS.SetTexture(CSkernel, "Normal", rts[(int)RT.Normal]);

        //Call compute shader
        debugCS.Dispatch(CSkernel, sourceCamera.pixelWidth / 8, sourceCamera.pixelHeight/ 8, 1);

        //debug for cs output
        for (int i = 0; i < MIP_MAP_COUNT; ++i) {
        Graphics.CopyTexture(result, i, CSoutputCopy[i], 0);
        }

        result.Release();
    }

    RenderTexture CreateIntermediateTarget ()
    {
        var result = new RenderTexture(MAX_TEXTURE_SIZE, MAX_TEXTURE_SIZE, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        result.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        result.enableRandomWrite = true;
        result.filterMode = FilterMode.Point;
        result.anisoLevel = 0;
        result.volumeDepth = MIP_MAP_COUNT;
        result.Create();

        return result;
    }


    void OnDestroy() {
        rts[0].Release();
        rts[1].Release();
        rts[2].Release();
        rts[3].Release();

        depthBuffer.Release();
    }

    //returns current unused ID an increments it
    public static int getNewId(GameObject o, int textureWidth,int textureHeight) {
        textureDimensions.Add(textureWidth);
        textureDimensions.Add(textureHeight);
        gameObjects.Add(o);
        return currentUnusedIndex++;
    }

    public int approximateSizeOnScreen(GameObject o) {
        Vector3 screenSize = sourceCamera.WorldToScreenPoint(GameObject.FindGameObjectWithTag("Player").GetComponent<Renderer>().bounds.max) - sourceCamera.WorldToScreenPoint(GameObject.FindGameObjectWithTag("Player").GetComponent<Renderer>().bounds.min);
        return (int)screenSize.magnitude;
    }
}

