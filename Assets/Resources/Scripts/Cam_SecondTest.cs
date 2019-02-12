//This script renders the scene from 5 diffrent angles and stores the tile masks in a folder

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cam_SecondTest : MonoBehaviour {
    //camera positions and angles
    public bool isSponza = true;
    public static Vector3[] positions;
    public static Vector3[] rotations;
    string name = "";

    //Sponza
    public static Vector3[] positionsSponza = { new Vector3(4, 1.5f, 0), new Vector3(6, 4, -0.5f), new Vector3(-6, 4, -0.5f), new Vector3(0, 4, -3), new Vector3(0, 4, -2) };
    public static Vector3[] rotationsSponza = { new Vector3(-7, -100, 0), new Vector3(8, -80, 0), new Vector3(4, -280, 0), new Vector3(0, 0, 0), new Vector3(0, -90, 0) };
    string nameSponza = "Sponza";

    //lost empire
    public static Vector3[] positionsLostEmpire = { new Vector3(20, 60, -12.5f), new Vector3(25, 40, -80), new Vector3(-30, 50, -80), new Vector3(30, 25, 50), new Vector3(35, 25, -12.5f) };
    public static Vector3[] rotationsLostEmpire = { new Vector3(80, -90, 0), new Vector3(7, -40, 0), new Vector3(10, 24, 0), new Vector3(7, 200, 0), new Vector3(-10, -90, 0) };
    string nameLostEmpire = "lostEmpire";


    //current position
    public int index = 0;

    //counter to give the renderer a few moments between each camera angle
    int count = -10;

    //array of all tilemasks (5 positions * 14 objects)
    public RenderTexture[] tileMasks;

    //called at the beginning
    void Start() {
        name = isSponza ? nameSponza : nameLostEmpire;
        positions = isSponza ? positionsSponza : positionsLostEmpire;
        rotations = isSponza ? rotationsSponza : rotationsLostEmpire;

        //set starting position
        transform.SetPositionAndRotation(positions[0], Quaternion.Euler(rotations[0]));

        //setup array for tilemasks
        tileMasks = new RenderTexture[positions.Length* GameObject.FindObjectsOfType<MeshFilter>().Length];
    }

    //Update is called once per frame
    void Update() {
        //return if end is reached
        if (index == positions.Length) {
            Debug.Break();
            return;
        }

        //copy current tilemasks to array for all objects
        if (count == 5) {
            Debug.Log(index + " " + this.transform.position.ToString() + " " + this.transform.rotation.ToString());
            foreach (Pipeline_OSS.ObjData obj in Pipeline_OSS.sceneObjects) {
                int i = obj.obj.GetComponent<Renderer>().material.GetInt("_ID");
                tileMasks[index * Pipeline_OSS.sceneObjects.Count + i] = new RenderTexture(obj.tileMask.width, obj.tileMask.height, 0, RenderTextureFormat.ARGB32);
                tileMasks[index * Pipeline_OSS.sceneObjects.Count + i].Create();
                Graphics.Blit(obj.tileMask, tileMasks[index * Pipeline_OSS.sceneObjects.Count + i]);
            }
        }

        //go to next position
        if (count == 10) {
            index++;
            //if end is reached save all images and break
            if (index == positions.Length) {
                saveAllImages();
                //Debug.Break();
                return;
            }

            //Debug.Break();
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
            int index_tmp = (int)(i / Pipeline_OSS.sceneObjects.Count);
            int objId = i % Pipeline_OSS.sceneObjects.Count;
            Pipeline_OSS.SaveTexture("SecondTest\\"+name+"\\"+ Pipeline_OSS.TILE_SIZE+"\\"+cam.pixelWidth+"x"+cam.pixelHeight+"\\"+index_tmp+"_object"+objId, tileMasks[i]);
        }
    }


}
