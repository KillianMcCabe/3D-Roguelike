using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LogManager : MonoBehaviour {

    static LogManager instance;
    
    static GameObject logList;
    public GameObject listElement;

    public AudioClip newMessage;

    void Start () {

        //Check if instance already exists
        if (instance == null)
        {
            instance = this;

            logList = transform.FindChild("MessageLogPanel").transform.FindChild("ElementGrid").gameObject;
            if (logList == null)
            {
                print("ERROR: LogManager failed to locate logList in children");
            }

            AddLogEntry("Welcome to the dungeon, take your time exploring and see if you can unlock the secrets hidden in the deeper levels.");
        }
        else if (instance != this)
            Destroy(gameObject);

        //Sets this to not be destroyed when reloading scene
        DontDestroyOnLoad(gameObject);
    }

    public static void AddLogEntry(string message)
    {
        GameObject go = Instantiate(instance.listElement, new Vector2(0, 0), Quaternion.identity) as GameObject;
        go.name = "New Log Entry";
        go.GetComponent<Text>().text = message;
        go.transform.SetParent(logList.transform);

        SoundManager.instance.PlaySingle(instance.newMessage, 0.6f);
    }
}
