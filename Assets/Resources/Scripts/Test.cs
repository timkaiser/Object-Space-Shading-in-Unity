using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour {
    #if DEBUG
    public RenderTexture[] rts;
    public RenderTexture tileMask;
    public RenderTexture worldPos;

    public RenderTexture baycentricCoords;
    public RenderTexture vertexIds;

    public RenderTexture finalImage;

    //public Matrix4x4 locToWorld;
    //public List<Vector3> vertices;

    private void Update() {
        rts = MyPipeline.rts;
        tileMask = MyPipeline.tileMaskCopy;
        baycentricCoords = MyPipeline.baycentricCoords;
        vertexIds = MyPipeline.vertexIds;
        finalImage = MyPipeline.finalImage;
        worldPos = MyPipeline.worldPosMapCopy;

        /*GameObject g = GameObject.FindGameObjectWithTag("RenderObject");

        //locToWorld = GameObject.FindGameObjectWithTag("RenderObject").GetComponent<Renderer>().localToWorldMatrix;
        
        MeshFilter[] mfs = g.GetComponentsInChildren<MeshFilter>();
        vertices = new List<Vector3>();
        foreach (MeshFilter mf in mfs) {
            vertices.AddRange(mf.sharedMesh.vertices);
        }*/
    }

    #endif
}
