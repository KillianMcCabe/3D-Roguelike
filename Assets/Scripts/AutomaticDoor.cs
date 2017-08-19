using UnityEngine;
using System.Collections;

public class AutomaticDoor : MonoBehaviour {

    [SerializeField]
    private Door door;

    bool playerOverlapping = false;

    int mobCount = 0;

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            playerOverlapping = true;
        }
        if (other.tag == "Mob")
        {
            mobCount++;
            door.OpenDoor();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            playerOverlapping = false;
        }
        if (other.tag == "Mob")
        {
            mobCount--;
            if (mobCount <= 0 && !playerOverlapping)
                door.CloseDoor();
        }
    }
}
