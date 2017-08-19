using UnityEngine;
using System.Collections;

public class DeleteAfterGridGenerated : MonoBehaviour {
	
	// Update is called once per frame
	void Update () {
	    if (TerrainGenerator.isReady)
        {
            Destroy(gameObject);
        }
	}
}
