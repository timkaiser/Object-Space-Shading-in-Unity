using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this script renders the objects uv coordinates to  a render texture
public class UVCoordinateRenderer : MonoBehaviour {
    //Camera
    [SerializeField]
    private Camera camera;

    //Render target for this camera
    [SerializeField]
    private RenderTexture renderTarget;

    // Use this for initialization
    void Start () {
        //Find the main camera
        GameObject mainCamera = GameObject.FindWithTag("MainCamera");

        //clone the main camera
        GameObject renderCamera = Instantiate((GameObject)Resources.Load("Prefabs/UVCamera"));
        renderCamera.GetComponent<UVCameraScript>().SetTargetObject(gameObject);
        renderCamera.transform.SetParent(mainCamera.transform);
        camera = renderCamera.GetComponent<Camera>();

        //set new render target to render texture
        renderTarget = new RenderTexture(800,600,24);
        camera.targetTexture = renderTarget;
        
    }
}
