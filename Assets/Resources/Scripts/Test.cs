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
    public RenderTexture[] worldPosMaps;

    public List<MyPipeline.ObjData> sceneObjects;

    void Start() {
        worldPosMaps = new RenderTexture[2];
        for(int i = 0; i<worldPosMaps.Length;i++) {
            worldPosMaps[i] = new RenderTexture(1024,512, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
            worldPosMaps[i].enableRandomWrite = true;
            worldPosMaps[i].filterMode = FilterMode.Point;
            worldPosMaps[i].anisoLevel = 0;
            worldPosMaps[i].Create();
        }
    }

    private void Update() {
        idMip = MyPipeline.idMipCopy;
        uv = MyPipeline.uvCopy;
        worldPosCopy = MyPipeline.worldPosCopy;
        finalImage = MyPipeline.finalImage;

        sceneObjects = MyPipeline.sceneObjects;
    }
    #endif
}
