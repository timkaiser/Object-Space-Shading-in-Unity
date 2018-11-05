//parts taken from: https://github.com/rcm4/Unity-MRT/tree/master/MRT/Assets/Scripts

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Cam : MonoBehaviour {

    public enum RT {
        UvAndId = 0,
        WorldPosition = 1,
        Normal = 2,
    }

    public RT rt;

    private Camera sourceCamera;

    private RenderTexture[] rts;
    private RenderBuffer[] colorBuffers;
    private RenderTexture depthBuffer;

    //compute shader and compute shader kernel
    private ComputeShader debugCS;
    private int CSkernel;

    public RenderTexture test;


    void Start() {

        this.sourceCamera = this.GetComponent<Camera>();

        this.rts = new RenderTexture[3] {
            new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 0, RenderTextureFormat.Default),
            new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 0, RenderTextureFormat.Default),
            new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 0, RenderTextureFormat.Default),
        };

        rts[0].Create();
        rts[1].Create();
        rts[2].Create();

        this.colorBuffers = new RenderBuffer[3] { rts[0].colorBuffer, rts[1].colorBuffer, rts[2].colorBuffer};

        this.depthBuffer = new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 24, RenderTextureFormat.Depth);
        this.depthBuffer.Create();

        //compute shader
        test = new RenderTexture(512, 512, 24);
        test.enableRandomWrite = true;
        test.Create();

        debugCS = (ComputeShader)Instantiate(Resources.Load("Shader/DebugCS"));
        CSkernel = debugCS.FindKernel("DebugCS");

        debugCS.SetTexture(CSkernel, "Result", test);
        debugCS.SetTexture(CSkernel, "UV", rts[0]);
        debugCS.SetTexture(CSkernel, "WorldPos", rts[1]);
        debugCS.SetTexture(CSkernel, "Normal", rts[2]);


    }

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        sourceCamera.SetTargetBuffers(this.colorBuffers, this.depthBuffer.depthBuffer);


        debugCS.Dispatch(CSkernel, test.width / 8, test.height / 8, 1);

        Graphics.Blit(rts[rt.GetHashCode()], destination);


    }

    void OnDestroy() {
        rts[0].Release();
        rts[1].Release();
        rts[2].Release();

        depthBuffer.Release();
    }
}

