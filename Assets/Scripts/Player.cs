using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class Player : MonoBehaviour {

    public static Player instance = null;

    public enum Action
    {
        None,
        MoveUpLevel,
        MoveDownLevel,
        OpenDoor,
        CloseDoor,
        EnteredDungeon,
        FallenDown
    }

    public static Action action = Action.None;
    public static Action mostRecentAction = Action.EnteredDungeon;
    public static int depth = 1;
    public static Text actionText;
    Text depthText;

    bool overlappingStairsUp = false;
    bool overlappingStairsDown = false;

    public static Vector3 fallPosition;
    public static Quaternion fallRotation;
    bool positioned;

    private GameObject lampLight;

    [SerializeField]
    private Stat health;
    [HideInInspector]
    public static float viewDistance = 15;

    int examinelayerMask;

    //Awake is always called before any Start functions
    void Awake()
    {
        //Check if instance already exists
        if (instance == null)
            instance = this;

        //If instance already exists and it's not this:
        else if (instance != this)
            Destroy(gameObject);

        //Sets this to not be destroyed when reloading scene
        DontDestroyOnLoad(gameObject);
    }

    // Use this for initialization
    void Start () {
        depthText = GameObject.Find("DepthText").GetComponent<Text>();
        actionText = GameObject.Find("ActionText").GetComponent<Text>();

        depthText.text = "depth: " + depth.ToString();
        positioned = false;

        lampLight = transform.FindChild("LampLight").gameObject;
        if (lampLight == null)
        {
            print("ERROR: LogManager failed to locate lampLight in children");
        }

        examinelayerMask = ~(1 << LayerMask.NameToLayer("TriggerArea"));
        health.Init();
    }

    void OnLevelWasLoaded()
    {
        depthText = GameObject.Find("DepthText").GetComponent<Text>();
        actionText = GameObject.Find("ActionText").GetComponent<Text>();
        depthText.text = "depth: " + depth.ToString();
    }

    // Update is called once per frame
    void Update () {
        if (!positioned)
        {
            if (TerrainGenerator.isReady)
            {
                health.Init();

                if (mostRecentAction == Action.FallenDown)
                {
                    transform.position = new Vector3(
                        fallPosition.x,
                        2,
                        fallPosition.z
                        );
                    transform.position = PathRequestManager.GetClosestWalkablePosition(transform.position);
                    transform.rotation = fallRotation;
                    takeDamage(25);
                }
                else if (mostRecentAction == Action.MoveDownLevel || mostRecentAction == Action.EnteredDungeon)
                {
                    transform.position = TerrainGenerator.stairsUpPosition;
                }
                else if (mostRecentAction == Action.MoveUpLevel)
                {
                    transform.position = TerrainGenerator.stairsDownPosition;
                }
                else
                {
                    print("ERROR: no player placement condition met");
                }
                positioned = true;
            }
            else return;
        }

        // examine
        if (Input.GetButtonDown("Examine"))
        {
            ExamineRayHit();
        }

        // show actions
        switch (action)
        {
            case Action.OpenDoor:
                actionText.text = "Press E open door";
                break;
            case Action.CloseDoor:
                actionText.text = "Press E close door";
                break;
            default:
                actionText.text = "";
                break;
        }

        if (overlappingStairsDown)
        {
            actionText.text = "Press E to climb down ladder";
        }
        else if (overlappingStairsUp)
        {
            if (depth == 1)
            {
                actionText.text = "The ladder back up to the surface is blocked. Looks like the only way to go is down.";
            }
            else
            {
                actionText.text = "Press E to climb up ladder";
            }
        }


        // take action
        if (Input.GetButtonDown("Action"))
        {

            if (overlappingStairsDown)
            {
                mostRecentAction = Action.MoveDownLevel;
                GoDown();
            }
            else if (overlappingStairsUp)
            {
                if (depth > 1)
                {
                    if (!LevelManager.ExistsLevelForDepth(depth))
                        LevelManager.addLevel(depth, MapGenerator.map, MapViewer.instance.fogTexture);
                    else
                        LevelManager.updateLevel(depth, MapViewer.instance.fogTexture);
                    depth--;
                    mostRecentAction = Action.MoveUpLevel;
                    SceneManager.LoadScene("default");
                }
            }
        }

        // toggle lamp
        if (Input.GetButtonDown("Flashlight"))
        {
            lampLight.SetActive(!lampLight.activeSelf);
        }

        if (transform.position.y < -4)
        {
            mostRecentAction = Action.FallenDown;
            fallPosition = transform.position;
            fallRotation = transform.rotation;
            GoDown();
        }

        action = Action.None;
	}

    private void GoDown()
    {
        if (!LevelManager.ExistsLevelForDepth(depth))
        {
            //LevelManager.addLevel(depth, MapGenerator.map, MapViewer.instance.fogTexture);
            LevelManager.addLevel(depth, MapGenerator.map, MapViewer.instance.fogCamera.targetTexture);
        }
        else
        {
            LevelManager.updateLevel(depth, MapViewer.instance.fogTexture);
        }

        depth++;
        positioned = false;
        TerrainGenerator.isReady = false;
        SceneManager.LoadScene("default");
    }

    public static Vector3 GetPosition()
    {
        return instance.transform.position;
    }

    void ExamineRayHit()
    {
        int x = Screen.width / 2;
        int y = Screen.height / 2;

        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(x, y));
        if (Physics.Raycast(ray, out hit, 10.0f, examinelayerMask))
        {
            print(hit.transform.gameObject.name);
            Examine examine = hit.transform.gameObject.GetComponent<Examine>();
            if (examine!= null)
                examine.printFindings();
        }
    }

    public void takeDamage(float dmg)
    {
        health.CurrentValue -= dmg;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Stairs Up")
        {
            overlappingStairsUp = true;
        }
        if (other.tag == "Stairs Down")
        {
            overlappingStairsDown = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Stairs Up")
        {
            overlappingStairsUp = false;
        }
        if (other.tag == "Stairs Down")
        {
            overlappingStairsDown = false;
        }
    }
}
