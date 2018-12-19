using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this class is only for accessing diffrent parts of the render pipeline from the unity interface for debug purposes only
public class Test : MonoBehaviour {
    #if DEBUG
    public RenderTexture idMip;
    public RenderTexture uv;
    public RenderTexture worldPosCopy;

    
    public RenderTexture finalImage;

    public float fps = 0;

    public List<MyPipeline.ObjData> sceneObjects;

    void Start() {
       
    }

    private void Update() {
        fps = 1 / Time.deltaTime;

        idMip = MyPipeline.idMipCopy;
        uv = MyPipeline.uvCopy;
        worldPosCopy = MyPipeline.worldPosCopy;
        finalImage = MyPipeline.finalImage;

        sceneObjects = MyPipeline.sceneObjects;
        
    }
    #endif
}
