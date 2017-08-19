using UnityEngine;
using System.Collections;

public class EnemyScript : MonoBehaviour {

    static float gravityStrength = 10.0f;

    public string mobName = "monster";
    public float agroRange = 10f;
    public float attackRange = 2f;
    

    [HideInInspector] // Hides var below
    public bool isDead = false;
    [HideInInspector]
    public bool agro = false;
    [HideInInspector]
    public Vector3 lastSeenPlayerPosition;
    [HideInInspector]
    public float timeSinceLastSeenPlayer = 5;
    [HideInInspector]
    public bool targetInSight = false;
    [HideInInspector]
    public bool isInViewOfPlayer = false;

    EnemyHealth enemyHealth;
    CharacterController controller;
    
    int playerMask;
    int layerMask;

    // Use this for initialization
    void Start () {
        
    }

    void Awake()
    {
        playerMask = (1 << LayerMask.NameToLayer("Player"));
        layerMask = (1 << LayerMask.NameToLayer("Terrain")) | (1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("Unwalkable"));

        enemyHealth = GetComponent<EnemyHealth>();
        controller = GetComponent<CharacterController>();
    }
	
	void LateUpdate () {

        if (RayHitPlayer())
        {
            agro = true;
            targetInSight = true;

            timeSinceLastSeenPlayer = 0;
            lastSeenPlayerPosition = Player.GetPosition();
        }
        else
        {
            timeSinceLastSeenPlayer += Time.deltaTime;
        }

        // add gravity
        Vector3 gravity = new Vector3();
        gravity.y -= gravityStrength;
        controller.Move(gravity * Time.deltaTime);

        // death cases
        if (enemyHealth.HP <= 0)
        {
            if (isInViewOfPlayer || timeSinceLastSeenPlayer < 5)
                LogManager.AddLogEntry("The " + mobName + " is slain");
            else
                LogManager.AddLogEntry("You hear something die in the distance.");
            Die();
        }
        if (transform.position.y < -5)
        {
            if (isInViewOfPlayer || timeSinceLastSeenPlayer < 5)
                LogManager.AddLogEntry("The " + mobName + " fell down a pit.");
            else
                LogManager.AddLogEntry("From afar you hear something falling down a pit.");
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        enemyHealth.Destroy();
        gameObject.SetActive(false);
    }

    bool RayHitPlayer()
    {
        RaycastHit hit;
        Vector3 from = transform.position + new Vector3(0, 1, 0);
        Vector3 towardsPlayer = (Player.GetPosition() - from);

        // check if goblin is so close to the player that spherecast wont work
        if (Physics.CheckSphere(from, 1, playerMask))
        {
            return true;
        }

        // check if an unwalkable collider is between us and the player i.e. goblin cant see him
        if (Physics.SphereCast(from, 1, towardsPlayer, out hit, Player.viewDistance, layerMask))
        {
            if (hit.transform.tag == "Player")
            {
                isInViewOfPlayer = true;
                if (hit.distance < agroRange && Vector3.Dot(transform.forward, towardsPlayer) > 0.2f || agro)
                {
                    return true;
                }
            }
            else
            {
                isInViewOfPlayer = false;
            }
        }

        return false;
    }

    public void ActivateAgro()
    {
        agro = true;
        timeSinceLastSeenPlayer = 0;
        lastSeenPlayerPosition = Player.GetPosition();
    }
}
