//This script renders the scene from 5 diffrent angles and stores the tile masks in a folder

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecondTestCam : MonoBehaviour {
    //camera positions and angles
    public static Vector3[] positions = {new Vector3(4, 1.5f, 0), new Vector3(6, 4, -0.5f), new Vector3(-6, 4, -0.5f), new Vector3(0, 4, -3), new Vector3(0, 4, -2)  };
    public static Vector3[] rotations = {new Vector3(-7, -100, 0), new Vector3(8, -80, 0), new Vector3(4, -280, 0), new Vector3(0, 0, 0), new Vector3(0, -90, 0) };

    //current position
    public int index = 0;

    //counter to give the renderer a few moments between each camera angle
    int count = -10;

    //array of all tilemasks (5 positions * 14 objects)
    public RenderTexture[] tileMasks;

    //called at the beginning
    void Start() {
        //set starting position
        transform.SetPositionAndRotation(positions[0], Quaternion.Euler(rotations[0]));

        //setup array for tilemasks
        tileMasks = new RenderTexture[positions.Length* GameObject.FindObjectsOfType<MeshFilter>().Length];
    }

    //Update is called once per frame
    void Update() {
        //return if end is reached
        if (index == positions.Length) {
            return;
        }

        //copy current tilemasks to array for all objects
        if (count == 5) {
            Debug.Log(index + " " + this.transform.position.ToString() + " " + this.transform.rotation.ToString());
            foreach (MyPipeline.ObjData obj in MyPipeline.sceneObjects) {
                int i = obj.obj.GetComponent<Renderer>().material.GetInt("_ID");
                tileMasks[index * MyPipeline.sceneObjects.Count + i] = new RenderTexture(obj.tileMask.width, obj.tileMask.height, 0, RenderTextureFormat.ARGB32);
                tileMasks[index * MyPipeline.sceneObjects.Count + i].Create();
                Graphics.Blit(obj.tileMask, tileMasks[index * MyPipeline.sceneObjects.Count + i]);
            }
        }

        //go to next position
        if (count == 10) {
            index++;
            //if end is reached save all images and break
            if (index == positions.Length) {
                saveAllImages();
                Debug.Break();
                return;
            }

            transform.SetPositionAndRotation(positions[index], Quaternion.Euler(rotations[index]));
            
            count = 0;
            
        } else {
            count++;
        }
    }

    //saves all images stored in tileMasks to file
    public void saveAllImages() {
        Camera cam = GameObject.FindObjectOfType<Camera>();
        for(int i=0; i< tileMasks.Length; i++) {
            int index_tmp = (int)(i / MyPipeline.sceneObjects.Count);
            int objId = i % MyPipeline.sceneObjects.Count;
            MyPipeline.SaveTexture("SecondTest\\"+MyPipeline.TILE_SIZE+"\\"+cam.pixelWidth+"x"+cam.pixelHeight+"\\"+index_tmp+"_object"+objId, tileMasks[i]);
        }
    }


}
