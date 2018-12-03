using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this class is only for accessing diffrent parts of the render pipeline from the unity interface for debug purposes only
public class Test : MonoBehaviour {
    #if DEBUG
    public RenderTexture[] rts;
    public RenderTexture tileMask;
    public RenderTexture worldPos;

    public RenderTexture baycentricCoords;
    public RenderTexture vertexIds;

    public RenderTexture finalImage;
    
    private void Update() {
        rts = MyPipeline.rts;
        tileMask = MyPipeline.tileMaskCopy;
        baycentricCoords = MyPipeline.baycentricCoords;
        vertexIds = MyPipeline.vertexIds;
        finalImage = MyPipeline.finalImage;
        worldPos = MyPipeline.worldPosMapCopy;
        
    }

    #endif
}
