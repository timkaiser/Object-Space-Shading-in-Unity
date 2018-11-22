using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Camtest : MonoBehaviour {
    public RenderTexture test;

	void Start () {
        Camera cam = this.gameObject.GetComponent<Camera>();
        test = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.Default);

        test.filterMode = FilterMode.Point;
        test.anisoLevel = 1;
        test.Create();
        
        cam.SetTargetBuffers(test.colorBuffer, test.depthBuffer);

        
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Graphics.Blit(test, destination);

    }

}
