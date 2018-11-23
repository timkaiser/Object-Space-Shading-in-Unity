using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour {
    #if DEBUG
    public RenderTexture[] rts;
    public RenderTexture[] CSoutputCopy;

    public RenderTexture baycentricCoords;
    public RenderTexture vertexIds;

    private void Update() {
        rts = MyPipeline.rts;
        CSoutputCopy = MyPipeline.CSoutputCopy;
        baycentricCoords = MyPipeline.baycentricCoords;
        vertexIds = MyPipeline.vertexIds;
    }

    #endif
}
