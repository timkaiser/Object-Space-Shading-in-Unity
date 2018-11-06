//parts taken from: https://github.com/rcm4/Unity-MRT/tree/master/MRT/Assets/Scripts

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Cam : MonoBehaviour {

    public enum RT {
        ID = 3,
        UV = 0,
        WorldPosition = 1,
        Normal = 2,
    }

    public RT rt;

    private Camera sourceCamera;

    public RenderTexture[] rts;
    private RenderBuffer[] colorBuffers;
    private RenderTexture depthBuffer;

    //compute shader and compute shader kernel
    private ComputeShader debugCS;
    private int CSkernel;

    public RenderTexture test;


    void Start() {

        this.sourceCamera = this.GetComponent<Camera>();

        /*this.rts = new RenderTexture[4] {
            new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 0, RenderTextureFormat.Default), //ID
            new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 0, RenderTextureFormat.Default), //UV
            new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 0, RenderTextureFormat.Default), //World Position
            new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 0, RenderTextureFormat.Default), //Normal

            */
        this.rts = new RenderTexture[4] {
            new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 0, RenderTextureFormat.RG16), //UV
            new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 0, RenderTextureFormat.ARGBFloat), //World Position
            new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 0, RenderTextureFormat.ARGB32), //Normal
            new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 0, RenderTextureFormat.RInt), //ID
        };

        rts[0].Create();
        rts[1].Create();
        rts[2].Create();
        rts[3].Create();



        this.colorBuffers = new RenderBuffer[4] { rts[0].colorBuffer, rts[1].colorBuffer, rts[2].colorBuffer , rts[3].colorBuffer};

        this.depthBuffer = new RenderTexture(sourceCamera.pixelWidth, sourceCamera.pixelHeight, 24, RenderTextureFormat.Depth);
        this.depthBuffer.Create();

        //compute shader
        test = new RenderTexture(512, 512, 24);
        test.enableRandomWrite = true;
        test.Create();

        debugCS = (ComputeShader)Instantiate(Resources.Load("Shader/DebugCS"));
        CSkernel = debugCS.FindKernel("DebugCS");

        debugCS.SetTexture(CSkernel, "Result", test);
        debugCS.SetTexture(CSkernel, "ID", rts[(int)RT.ID]);
        debugCS.SetTexture(CSkernel, "UV", rts[(int)RT.UV]);
        debugCS.SetTexture(CSkernel, "WorldPos", rts[(int)RT.WorldPosition]);
        debugCS.SetTexture(CSkernel, "Normal", rts[(int)RT.Normal]);


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
        rts[3].Release();

        depthBuffer.Release();
    }
}

