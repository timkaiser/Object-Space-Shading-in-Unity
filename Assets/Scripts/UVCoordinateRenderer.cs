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
    //render texture with final uv coordinates of visible surfaces
    private RenderTexture renderTargetWithoutHiddenSurfaces;

    //compute shader and compute shader kernel
    private ComputeShader hiddenSurfaceRemovalCS;
    private int CSkernel;

    //image of main camera
    private static RenderTexture mainCameraImage;

    // Use this for initialization
    void Start () {
        //Find the main camera
        GameObject mainCamera = GameObject.FindWithTag("MainCamera");
        mainCameraImage = mainCamera.GetComponent<Camera>().targetTexture;


        //clone the main camera
        GameObject renderCamera = Instantiate((GameObject)Resources.Load("Prefabs/UVCamera"));
        renderCamera.GetComponent<UVCameraScript>().SetTargetObject(gameObject);
        renderCamera.transform.SetParent(mainCamera.transform);
        cam = renderCamera.GetComponent<Camera>();

        //set new render target to render texture of cloned main camera
        renderTarget = new RenderTexture(800,600,24);
        cam.targetTexture = renderTarget;

        //Find compute shader
        hiddenSurfaceRemovalCS = (ComputeShader)Instantiate(Resources.Load("Shader/HiddenSurfaceRemovalShader"));
        CSkernel = hiddenSurfaceRemovalCS.FindKernel("HiddenSurfaceRemovalCS");

        //Initialize Rendertexture for after hidden surface removal
        renderTargetWithoutHiddenSurfaces = new RenderTexture(renderTarget.width, renderTarget.height, renderTarget.depth);
        renderTargetWithoutHiddenSurfaces.enableRandomWrite = true;
        renderTargetWithoutHiddenSurfaces.Create();

        //set parameters of the compute shader
        hiddenSurfaceRemovalCS.SetTexture(CSkernel, "result", renderTargetWithoutHiddenSurfaces);
        hiddenSurfaceRemovalCS.SetTexture(CSkernel, "mainCamera", mainCameraImage);
        hiddenSurfaceRemovalCS.SetTexture(CSkernel, "renderTarget", renderTarget);
    }
    
    //removes parts of objects that are hidden
    public void RemoveHiddenSurface() {
        //call compute shader
        hiddenSurfaceRemovalCS.Dispatch(CSkernel, renderTargetWithoutHiddenSurfaces.width / 8, renderTargetWithoutHiddenSurfaces.height / 8, 1);      
    }

    public RenderTexture getRenderTarget() {
        return renderTarget;
    }
}
