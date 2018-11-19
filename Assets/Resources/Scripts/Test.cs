using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour {
    #if DEBUG
    public RenderTexture[] rts;
    public RenderTexture[] CSoutputCopy;
    public RenderTexture mainImage;

    private void Update() {
        rts = MyPipeline.rts;
        CSoutputCopy = MyPipeline.CSoutputCopy;
        mainImage = MyPipeline.mainImage;

    }

    #endif
}
