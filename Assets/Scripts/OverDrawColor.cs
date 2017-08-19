using UnityEngine;
using System.Collections;

public class OverDrawColor : MonoBehaviour {

    // Use this for initialization
    void Start () {
        GetComponent<SkinnedMeshRenderer>().materials[0].SetColor("_OverDrawColor", new Color(0.7f, 0, 0));
    }
	
	// Update is called once per frame
	void Update () {
        //GetComponent<SkinnedMeshRenderer>().materials[0].SetColor("_OverDrawColor", new Color(0.7f, 0, 0));
    }
}
