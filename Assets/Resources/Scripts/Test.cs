using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this class is only for accessing diffrent parts of the render pipeline from the unity interface for debug purposes only
public class Test : MonoBehaviour {
    #if DEBUG
    public RenderTexture[] firstPassTargets;

    
    public RenderTexture finalImage;

    public float fps = 0;

    public List<MyPipeline.ObjData> sceneObjects;

    public Object[] objects;

    private void Update() {
        fps = 1 / Time.deltaTime;

        firstPassTargets = MyPipeline.firstPassTargets;
        finalImage = MyPipeline.finalImage;

        sceneObjects = MyPipeline.sceneObjects;

        objects = Resources.FindObjectsOfTypeAll<Object>();
    }
    #endif
}
