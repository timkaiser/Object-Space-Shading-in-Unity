//parts taken from: https://github.com/rcm4/Unity-MRT/tree/master/MRT/Assets/Scripts

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Cam : MonoBehaviour {
    //object parameterst
    [SerializeField]
    private static int currentUnusedIndex = 1;
    [SerializeField]
    private static List<int> textureDimensions = new List<int>();

    public const int maxTextureSize = 2048;

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
    private RenderTexture[] rtsCopy;        //Copies of the RenderTextures (needed because of some bugs with the original textures)
    private RenderBuffer[] colorBuffers;    //ColorBuffers of the RenderTextures
    private RenderTexture depthBuffer;      //DepthBuffer for the Camera

    //debug for cs output
    public RenderTexture[] CSoutputCopy;

    //compute shader for mapping the normal/world position to a texture atlas
    private ComputeShader debugCS;
    private int CSkernel;

    //Result of the mapping of normal/world position to a texture atlas
    public RenderTexture results;



    //Called on start
    void Start() {
        //get main camera
        this.sourceCamera = this.GetComponent<Camera>();
        
        //Initialize Render Textures
        this.rts = new RenderTexture[4] {
            new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 24, RenderTextureFormat.RInt), //ID
            new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 24, RenderTextureFormat.RG16), //UV
            new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 24, RenderTextureFormat.ARGBFloat), //World Position
            new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 24, RenderTextureFormat.ARGB32), //Normal
        };

        //Initialize Render Textures
        this.rtsCopy = new RenderTexture[4] {
            new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 24, RenderTextureFormat.RInt), //ID
            new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 24, RenderTextureFormat.RG16), //UV
            new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 24, RenderTextureFormat.ARGBFloat), //World Position
            new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 24, RenderTextureFormat.ARGB32), //Normal
        };

        //Initialize Render Textures
        for (int i=0; i< rts.Length; i++) {
            rts[i].Create();
            rtsCopy[i].Create();
        }

        //get color buffers from rendertextures
        this.colorBuffers = new RenderBuffer[4] { rts[0].colorBuffer, rts[1].colorBuffer, rts[2].colorBuffer, rts[3].colorBuffer};

        //create depth buffer
        this.depthBuffer = new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 24, RenderTextureFormat.Depth);
        this.depthBuffer.Create();

        //create render texture as output for the compute shader
        results = new RenderTexture(maxTextureSize, maxTextureSize, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        results.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        results.enableRandomWrite = true;
        results.volumeDepth = 3;
        results.Create();

        /*finalImage = new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 24, RenderTextureFormat.Default);
        finalImage.enableRandomWrite = true;
        finalImage.Create();
        */

        //debug for cs output
        CSoutputCopy = new RenderTexture[3];
        for(int i=0; i<CSoutputCopy.Length; i++) {
            CSoutputCopy[i] = new RenderTexture(maxTextureSize, maxTextureSize, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            CSoutputCopy[i].Create();
        }



        //load compute shader
        int[] texDimArray = textureDimensions.ToArray();

        ComputeBuffer texDimBuffer = new ComputeBuffer(texDimArray.Length, sizeof(int));
        texDimBuffer.SetData(texDimArray);


        debugCS = (ComputeShader)Instantiate(Resources.Load("Shader/DebugCS"));
        CSkernel = debugCS.FindKernel("DebugCS");
        //set parameters for compute shader
        debugCS.SetTexture(CSkernel, "Output", results);
        debugCS.SetTexture(CSkernel, "ID", rtsCopy[(int)RT.ID]);
        debugCS.SetTexture(CSkernel, "UV", rtsCopy[(int)RT.UV]);
        debugCS.SetTexture(CSkernel, "WorldPos", rtsCopy[(int)RT.WorldPosition]);
        debugCS.SetTexture(CSkernel, "Normal", rtsCopy[(int)RT.Normal]);
        debugCS.SetBuffer (CSkernel, "TextureDimensions", texDimBuffer);
        //debugCS.SetTexture(CSkernel, "FinalImage", finalImage);

    }

    //Called while image is rendered (?)
    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        sourceCamera.SetTargetBuffers(this.colorBuffers, this.depthBuffer.depthBuffer);
        
        Graphics.Blit(rts[rt.GetHashCode()], destination);
    }

    //Called after Image is rendered
    private void OnPostRender() {
        //Copy RenderTextures to other array (to prevent a bug)
        for (int i = 0; i < rts.Length; i++) {
            Graphics.CopyTexture(rts[i], rtsCopy[i]);
        }
        //empty result image
        results.Release();
        results = new RenderTexture(maxTextureSize, maxTextureSize, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        results.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        results.enableRandomWrite = true;
        results.volumeDepth = 3;
        results.Create();
        debugCS.SetTexture(CSkernel, "Output", results);


        //Call compute shader
        debugCS.Dispatch(CSkernel, sourceCamera.pixelWidth / 8, sourceCamera.pixelHeight/ 8, 1);

        //debug for cs output
        for (int i = 0; i < CSoutputCopy.Length; i++) {
            Graphics.CopyTexture(results,i,0, CSoutputCopy[i],0,0);
        }
    }


    void OnDestroy() {
        rts[0].Release();
        rts[1].Release();
        rts[2].Release();
        rts[3].Release();

        depthBuffer.Release();
    }

    //returns current unused ID an increments it
    public static int getNewId(int textureWidth,int textureHeight) {
        textureDimensions.Add(textureWidth);
        textureDimensions.Add(textureHeight);
        return currentUnusedIndex++;
    }
}

