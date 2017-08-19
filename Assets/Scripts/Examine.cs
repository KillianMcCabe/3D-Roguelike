using UnityEngine;
using System.Collections;

public class Examine : MonoBehaviour {

    public string[] findings;
    public bool logOnce;

    private bool hasBeenLogged = false;
    

    public void printFindings()
    {
        if (!(logOnce && hasBeenLogged))
        {
            //print(findings[Random.Range(0, findings.Length - 1)]);
            LogManager.AddLogEntry(findings[Random.Range(0, findings.Length)]);

            hasBeenLogged = true;
        }

        //
        // Check for applicable actions
        //

        Door door = GetComponent<Door>();
        if (door != null)
        {
            door.Toggle();
        }

        DissolveEffect dis = GetComponent<DissolveEffect>();
        if (dis != null)
        {
            dis.TriggerDissolve();
        }

        Chest chest = GetComponent<Chest>();
        if (chest != null)
        {
            chest.Open();
        }
        
    }
}
