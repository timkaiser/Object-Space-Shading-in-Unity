using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnObjectCreation : MonoBehaviour {
    public int id;
	// Use this for initialization
	void Awake () {
        id = Cam.getNewId(
                this.gameObject,
                this.GetComponent<Renderer>().material.mainTexture.width,
                this.GetComponent<Renderer>().material.mainTexture.height
            );
        this.GetComponent<Renderer>().material.SetInt("_ID", id);
    }
    
}
