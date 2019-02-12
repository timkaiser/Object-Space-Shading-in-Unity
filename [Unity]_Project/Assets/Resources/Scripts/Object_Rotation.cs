//This script rotates the object with a set speed
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Object_Rotation : MonoBehaviour {
    //Rotation Speed
    public float speed = 10f;
    
    // Update is called once per frame
	void Update () {
        //Rotate
       this.transform.Rotate(Vector3.up * speed*Time.deltaTime);
       //this.transform.Rotate(Vector3.left * speed * Time.deltaTime * 2);
    }
}
