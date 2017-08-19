using UnityEngine;
using System.Collections;

public class Chest : MonoBehaviour {

    int openHash = Animator.StringToHash("Open");
    Animator anim;

    // Use this for initialization
    void Start () {
        anim = GetComponent<Animator>();

    }
	
	// Update is called once per frame
	void Update () {
	
	}

    public void Open()
    {    
        anim.SetTrigger(openHash);
    }
}
