using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//this script makes sure that only one object gets rendered to the render target
public class UVCameraScript : MonoBehaviour {


    //Object that has to be rendered
    [SerializeField]
    private GameObject targetObject;
    
    //UVCoordinateRenderer Script
    UVCoordinateRenderer uvScript;

    void Start() {
        //Set camera layer to render layer
        Camera camera = gameObject.GetComponent<Camera>();
        camera.cullingMask = (1 << LayerMask.NameToLayer("UVRenderLayer"));
        //set camera background
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.white;
        gameObject.tag = "UVCamera";
    }

    //camera layer the object is currently in 
    private int objectLayer = 0;

    //Set the object that has to be rendered
    public void SetTargetObject(GameObject o) {
        targetObject = o;
        objectLayer = o.layer;
        uvScript = targetObject.GetComponent<UVCoordinateRenderer>();
    }

    //move object to render layer
    //code source: https://answers.unity.com/questions/781931/render-one-object-to-each-camera.html [22.10.2018 - 16:40]
    void OnPreCull() {
        targetObject.layer = LayerMask.NameToLayer("UVRenderLayer");
        
    }

    //move object back to original layer
    void OnPostRender() {
        targetObject.layer = objectLayer;
        uvScript.RemoveHiddenSurface();
    }
}
