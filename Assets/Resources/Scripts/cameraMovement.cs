using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraMovement : MonoBehaviour {
    public float speed = 0.1f;
    public bool loop = false;
    public Vector3[] positions =  { new Vector3(6,4,-0.5f), new Vector3(4,4,-2.5f), new Vector3(0,4,-2) };
    public Vector3[] rotations = { new Vector3(8,-80,0), new Vector3(8,-24,0), new Vector3(4.5f,-90,0) };
    public float[] distances = { 1, 1 };

    int index = 1;

    void Start () {
        transform.SetPositionAndRotation(positions[0],Quaternion.Euler(rotations[0]));
	}

    // Update is called once per frame
    float progress = 0;
	void Update () {
        if(index == positions.Length) {
            if (loop) {
                transform.SetPositionAndRotation(positions[0], Quaternion.Euler(rotations[0]));
                index = 1;

            } else {
                return;
            }
        }

        Vector3 newPos = Vector3.Lerp(positions[index - 1], positions[index], progress);
        Vector3 newRot = Vector3.Lerp(rotations[index - 1], rotations[index], progress);
        transform.SetPositionAndRotation(newPos, Quaternion.Euler(newRot));

        progress += speed / distances[index-1] *Time.deltaTime;
        if(progress > 1) {
            progress = 0;
            index++;
        }
	}
}
