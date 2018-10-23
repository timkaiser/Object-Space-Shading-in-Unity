using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this script renders the objects uv coordinates to  a render texture
public class UVCoordinateRenderer : MonoBehaviour {
    //Camera
    [SerializeField]
    private Camera cam;

    //Render target for this camera
    [SerializeField]
    private RenderTexture renderTarget;
    [SerializeField]
    private RenderTexture renderTargetFinal;

    //compute shader
    private ComputeShader hiddenSurfaceRemovalCS;
    private int CSkernel;

    //main
    private static RenderTexture mainImage;

    // Use this for initialization
    void Start () {
        //Find the main camera
        GameObject mainCamera = GameObject.FindWithTag("MainCamera");
        mainImage = mainCamera.GetComponent<Camera>().targetTexture;

        //clone the main camera
        GameObject renderCamera = Instantiate((GameObject)Resources.Load("Prefabs/UVCamera"));
        renderCamera.GetComponent<UVCameraScript>().SetTargetObject(gameObject);
        renderCamera.transform.SetParent(mainCamera.transform);
        cam = renderCamera.GetComponent<Camera>();

        //set new render target to render texture
        renderTarget = new RenderTexture(800,600,24);
        cam.targetTexture = renderTarget;

        //Find compute shader
        hiddenSurfaceRemovalCS = (ComputeShader)Resources.Load("Shader/HiddenSurfaceRemovalShader");
        CSkernel = hiddenSurfaceRemovalCS.FindKernel("HiddenSurfaceRemovalCS");

        //Initialize Rendertexture for after hidden surface removal
        renderTargetFinal = new RenderTexture(renderTarget.width, renderTarget.height, renderTarget.depth);
        renderTargetFinal.enableRandomWrite = true;
        renderTargetFinal.Create();
        hiddenSurfaceRemovalCS.SetTexture(CSkernel, "result", renderTargetFinal);


        hiddenSurfaceRemovalCS.SetTexture(CSkernel, "mainCamera", mainImage);
        hiddenSurfaceRemovalCS.SetTexture(CSkernel, "renderTarget", renderTarget);
    }
    
    public void RemoveHiddenSurface() {
        if (gameObject.name == "Mars") {
            hiddenSurfaceRemovalCS.Dispatch(CSkernel, renderTargetFinal.width / 8, renderTargetFinal.height / 8, 1);
        }
    }

}
