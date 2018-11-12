using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnObjectCreation : MonoBehaviour {

	// Use this for initialization
	void Awake () {
        this.GetComponent<Renderer>().material.SetInt("_ID", 
            Cam.getNewId(
                this.gameObject,
                this.GetComponent<Renderer>().material.mainTexture.width,
                this.GetComponent<Renderer>().material.mainTexture.height
            )
        );
    }
    
}
