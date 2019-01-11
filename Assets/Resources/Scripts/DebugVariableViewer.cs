//this class is for accessing diffrent parts of the render pipeline from the unity interface for debug purposes only

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugVariableViewer : MonoBehaviour {
    #if DEBUG
    //variables that have to be observed
    public RenderTexture[] firstPassTargets;
    public RenderTexture finalImage;
    public float fps = 0;
    public List<MyPipeline.ObjData> sceneObjects;
    public Object[] objects;
    public int tilesize = MyPipeline.TILE_SIZE;
   
    //update variables
    private void Update() {
        fps = 1 / Time.deltaTime;

        firstPassTargets = MyPipeline.firstPassTargets;
        finalImage = MyPipeline.finalImage;

        sceneObjects = MyPipeline.sceneObjects;

        objects = Resources.FindObjectsOfTypeAll<Texture>();
    }
    #endif
}
