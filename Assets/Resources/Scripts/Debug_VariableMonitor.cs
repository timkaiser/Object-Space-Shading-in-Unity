//this class is for accessing diffrent parts of the render pipeline from the unity interface for debug purposes only

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debug_VariableMonitor : MonoBehaviour {
    #if DEBUG
    //variables that have to be observed
    public RenderTexture[] firstPassTargets;
    public RenderTexture finalImage;
    public float fps = 0;
    public List<Pipeline_OSS.ObjData> sceneObjects;
    public Object[] objects;
    public int tilesize = Pipeline_OSS.TILE_SIZE;
    public int framecount = 0;

    //update variables
    private void Update() {
        fps = 1 / Time.deltaTime;
        tilesize = Pipeline_OSS.TILE_SIZE;

        firstPassTargets = Pipeline_OSS.firstPassTargets;
        finalImage = Pipeline_OSS.finalImage;

        sceneObjects = Pipeline_OSS.sceneObjects;

        objects = Resources.FindObjectsOfTypeAll<Texture>();
        framecount = Time.frameCount;
    }
    #endif
}
