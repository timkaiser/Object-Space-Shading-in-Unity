//This script moves the camera through the scene and logs statistics along the way

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Cam_FirstTest : MonoBehaviour {
    //movement speed
    public float speed = 0.1f;

    //loop movement?
    public bool loop = false;

    //positions along the path the camera is moving
    public bool isSponza = true;
    public static Vector3[] positions;
    public static Vector3[] rotations;
    string name = "";

    //Sponza
    public static Vector3[] positionsSponza = { new Vector3(6, 4, -0.5f), new Vector3(4, 4, -2.5f), new Vector3(0, 4, -2) };
    public static Vector3[] rotationsSponza = { new Vector3(8, -80, 0), new Vector3(8, -24, 0), new Vector3(4.5f, -90, 0) };
    string nameSponza = "Sponza";

    //lost empire
    public static Vector3[] positionsLostEmpire = { new Vector3(20, 60, -12.5f), new Vector3(20, 26.5f, -12.5f), new Vector3(-10, 26.5f, -12.5f) };
    public static Vector3[] rotationsLostEmpire = { new Vector3(80, -90, 0), new Vector3(10, -90, 0), new Vector3(-10, -3, 0) };
    string nameLostEmpire = "lostEmpire";

    //distance between positions
    public float[] distances = { 1, 1 };

    //next position to move to (start = 0)
    int index = 1;

    //progress between positions (between 0 and 1)
    float progress = 0;

    void Start () {
        name = isSponza ? nameSponza : nameLostEmpire;
        positions = isSponza ? positionsSponza : positionsLostEmpire;
        rotations = isSponza ? rotationsSponza : rotationsLostEmpire;

        //set starting position
        transform.SetPositionAndRotation(positions[0],Quaternion.Euler(rotations[0]));

        //reset log file
        Pipeline_OSS.ResetLog();
	}

    // Update is called once per frame
   void Update () {
        //log statistics  
        Pipeline_OSS.LogPixelStats();

        //if end of path is reached
        if (index == positions.Length) {

            //save logs
            Pipeline_OSS.SaveLogCSV(name+" FirstTest");
            Debug.Break();
            if (loop) { //return to start if loop is enabled
                transform.SetPositionAndRotation(positions[0], Quaternion.Euler(rotations[0]));
                index = 1;

            } else {
                return;
            }
        }

        //calculate new position
        Vector3 newPos = Vector3.Lerp(positions[index - 1], positions[index], progress);
        Vector3 newRot = Vector3.Lerp(rotations[index - 1], rotations[index], progress);
        transform.SetPositionAndRotation(newPos, Quaternion.Euler(newRot));

        progress += speed / distances[index - 1] * 1 / 30; //speed depending on framerate

        if (progress > 1) { //move to next waypoint if current one is reached
            progress = 0;
            index++;
        }
    }
}
