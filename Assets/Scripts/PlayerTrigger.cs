using UnityEngine;
using System.Collections;

public class PlayerTrigger : MonoBehaviour {

    public bool overlap = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            overlap = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            overlap = false;
        }
    }
}
