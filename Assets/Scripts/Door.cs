using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour {

    bool isOpen = false;

    Animation anim;

    // Use this for initialization
    void Awake()
    {
        anim = GetComponent<Animation>();
    }

    public void OpenDoor()
    {
        if (!isOpen)
        {
            anim.Play("Open");
            anim["Open"].speed = 1;
            isOpen = true;
        }
    }

    public void CloseDoor()
    {
        if (isOpen)
        {
            // play the door opening animation in reverse
            anim["Open"].speed = -1;
            anim["Open"].time = anim["Open"].length;
            anim.Play("Open");
            isOpen = false;
        }
    }

    public void Toggle()
    {
        if (isOpen)
        {
            CloseDoor();
        }
        else
        {
            OpenDoor();
        }
    }
    
}
