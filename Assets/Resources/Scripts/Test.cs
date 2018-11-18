using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour {
    #if DEBUG
    public RenderTexture[] rts;
    public RenderTexture[] CSoutputCopy;

    private void Update() {
        rts = MyPipline.rts;
        CSoutputCopy = MyPipline.CSoutputCopy;


    }

    #endif
}
