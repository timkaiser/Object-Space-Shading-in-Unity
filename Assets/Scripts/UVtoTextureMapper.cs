using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UVtoTextureMapper : MonoBehaviour {
    //compute shader and compute shader kernel
    private ComputeShader uvToTextureMappingCS;
    private int CSkernel;

    //new and old textures
    [SerializeField]
    private RenderTexture newTexture;
    [SerializeField]
    private Texture oldTexture;
    [SerializeField]
    private RenderTexture renderTarget;


    // Use this for initialization
    void Start() {
        renderTarget = (gameObject.GetComponent<UVCoordinateRenderer>()).getRenderTarget();

        oldTexture = gameObject.GetComponent<Material>().mainTexture;

        //initialize empty texture for mapping (I probably have to redo this for moving images)
        newTexture = new RenderTexture(oldTexture.width, oldTexture.height, 24);
        newTexture.enableRandomWrite = true;
        newTexture.Create();


        //Find compute shader
        uvToTextureMappingCS = (ComputeShader)Instantiate(Resources.Load("Shader/HiddenSurfaceRemovalShader"));
        CSkernel = uvToTextureMappingCS.FindKernel("UVtoTextureMappingCS");


        //set parameters of the compute shader
        uvToTextureMappingCS.SetTexture(CSkernel, "result", newTexture);
        uvToTextureMappingCS.SetTexture(CSkernel, "uvCoords", renderTarget);
        uvToTextureMappingCS.SetTexture(CSkernel, "objectTexture", oldTexture);
    }

    //removes parts of objects that are hidden
    public void mapUVtoTexture() {
        renderTarget = (gameObject.GetComponent<UVCoordinateRenderer>()).getRenderTarget();
        //call compute shader
        //uvToTextureMappingCS.Dispatch(CSkernel, renderTarget.width / 8, renderTarget.height / 8, 1);
    }
}
