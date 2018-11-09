//parts taken from: https://github.com/rcm4/Unity-MRT/tree/master/MRT/Assets/Scripts

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Cam : MonoBehaviour {

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
        results = new RenderTexture(512, 512, 24);
        results.enableRandomWrite = true;
        results.Create();

        //load compute shader
        debugCS = (ComputeShader)Instantiate(Resources.Load("Shader/DebugCS"));
        CSkernel = debugCS.FindKernel("DebugCS");     

        //set parameters for compute shader
        debugCS.SetTexture(CSkernel, "Result", results);
        debugCS.SetTexture(CSkernel, "ID", rtsCopy[(int)RT.ID]);
        debugCS.SetTexture(CSkernel, "UV", rtsCopy[(int)RT.UV]);
        debugCS.SetTexture(CSkernel, "WorldPos", rtsCopy[(int)RT.WorldPosition]);
        debugCS.SetTexture(CSkernel, "Normal", rtsCopy[(int)RT.Normal]);


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

        //Call compute shader
        debugCS.Dispatch(CSkernel, results.width / 8, results.height / 8, 1);
    }


    void OnDestroy() {
        rts[0].Release();
        rts[1].Release();
        rts[2].Release();
        rts[3].Release();

        depthBuffer.Release();
    }
}

