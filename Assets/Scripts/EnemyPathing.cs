using UnityEngine;
using System.Collections;

public class EnemyPathing : MonoBehaviour {

    
    static float pathRefreshTime = 1.0f;
    
    public float walkSpeed = 2.5f;
    public float runSpeed = 5.5f;
    float turnSpeed;
    float runningTurnSpeed;

    Vector3[] path;
    Vector3 currentWaypoint;
    Vector3 towardsWaypoint;
    int targetWaypointIndex;

    [HideInInspector]
    public bool pathRequested = false;

    private EnemyScript enemyScript;
    private CharacterController controller;
    private Animator animator;

    private float timeSinceLastRefreshedPath = 0;

    // Use this for initialization
    void Start () {
        enemyScript = GetComponent<EnemyScript>();
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        path = new Vector3[0];
        targetWaypointIndex = 0;
        timeSinceLastRefreshedPath = pathRefreshTime;

        turnSpeed = 50.0f * walkSpeed;
        runningTurnSpeed = 50.0f * runSpeed;
    }
	
	// Update is called once per frame
	void Update () {
        if (enemyScript.agro)
        { 

            float distToPlayer = Vector3.Distance(transform.position, Player.GetPosition());

            var towardsPlayer = Player.GetPosition() - transform.position;
            towardsPlayer.y = 0;
            towardsPlayer = towardsPlayer.normalized;

            if (enemyScript.timeSinceLastSeenPlayer > 3.0) // lost track of player
            {
                // returning to regular pathing
                enemyScript.agro = false;
                path = new Vector3[1];
                path[0] = enemyScript.lastSeenPlayerPosition;
                targetWaypointIndex = 1;
                currentWaypoint = enemyScript.lastSeenPlayerPosition;
                currentWaypoint.y = 0;
            }
            else if (distToPlayer < enemyScript.attackRange) // in position to attack the player
            {
                // move into attack
                controller.transform.rotation = Quaternion.LookRotation(towardsPlayer, Vector3.up);
                controller.Move(transform.forward * runSpeed * Time.deltaTime);
            }
            else // chase the player
            {
                if (!pathRequested && timeSinceLastRefreshedPath > pathRefreshTime)
                {
                    pathRequested = true;
                    PathRequestManager.RequestPath(transform.position, Player.GetPosition(), OnPathFound);
                }

                towardsWaypoint = currentWaypoint - transform.position;
                towardsWaypoint.y = 0;
                towardsWaypoint = towardsWaypoint.normalized;

                // turn towards waypoint
                if (towardsWaypoint != Vector3.zero)
                    controller.transform.rotation = Quaternion.RotateTowards(controller.transform.rotation, Quaternion.LookRotation(towardsWaypoint), Time.deltaTime * runningTurnSpeed);

                // if facing waypoint, run forward
                if (Vector3.Dot(transform.forward, towardsWaypoint) > 0.25f)
                    controller.Move(transform.forward * runSpeed * Time.deltaTime);


                if (targetWaypointIndex >= path.Length) // reached end of path
                {
                    currentWaypoint = enemyScript.lastSeenPlayerPosition;
                }
                // onto next waypoint?
                else if (Vector3.Distance(transform.position, currentWaypoint) < 0.5f)
                {
                    targetWaypointIndex++;

                    if (targetWaypointIndex < path.Length) // reached end of path
                    {
                        currentWaypoint = path[targetWaypointIndex];
                    }
                    else
                    {
                        currentWaypoint = enemyScript.lastSeenPlayerPosition;
                    }
                }
            }

        }
        else
        {
            if (targetWaypointIndex < path.Length)
            {
                // move towards current waypoint
                towardsWaypoint = currentWaypoint - transform.position;
                towardsWaypoint.y = 0;
                towardsWaypoint = towardsWaypoint.normalized;

                if (towardsWaypoint != Vector3.zero)
                    controller.transform.rotation = Quaternion.RotateTowards(controller.transform.rotation, Quaternion.LookRotation(towardsWaypoint), Time.deltaTime * turnSpeed);

                if (Vector3.Dot(transform.forward, towardsWaypoint) > 0.25f)
                    controller.Move(transform.forward * walkSpeed * Time.deltaTime);

                // onto next waypoint?
                if (Vector3.Distance(transform.position, currentWaypoint) < 0.5f)
                {
                    targetWaypointIndex++;

                    if (targetWaypointIndex >= path.Length) // reached end of path
                    {
                        targetWaypointIndex = 0;
                        path = new Vector3[0];
                    }
                    else
                    {
                        currentWaypoint = path[targetWaypointIndex];
                    }
                }
            }
            else // not really doing anything atm
            {
                // wander or idle
                if (pathRequested)
                {
                    // wait for path
                }
                else if (!Grid.isWalkable(transform.position))
                {
                    currentWaypoint = PathRequestManager.GetClosestWalkablePosition(transform.position);

                    path = new Vector3[1];
                    path[0] = currentWaypoint;
                    targetWaypointIndex = 1;
                }
                else
                {
                    //pick random point on the map and create a walkable path from current location
                    PathRequestManager.RequestPath(transform.position, Grid.GetRandomGridPosition(), OnPathFound);
                    pathRequested = true;
                }
            }
        }

        timeSinceLastRefreshedPath += Time.deltaTime;
    }

    public void OnPathFound(Vector3[] newPath, bool pathSuccessful)
    {
        pathRequested = false;

        if (pathSuccessful)
        {
            if (newPath.Length > 0)
            {
                timeSinceLastRefreshedPath = 0;
                targetWaypointIndex = 0;
                path = newPath;
                currentWaypoint = path[0];
            }
        }
    }

    public void OnDrawGizmos()
    {
        // draw ray to player
        //Gizmos.color = Color.green;
        //Gizmos.DrawLine(transform.position + new Vector3(0, 1, 0) + (transform.forward * 0.4f), Player.GetPosition());

        if (path != null)
        {
            Gizmos.color = Color.yellow;
            if (!Grid.isWalkable(currentWaypoint))
                Gizmos.color = Color.red;
            Gizmos.DrawCube(currentWaypoint, Vector3.one);
            Gizmos.DrawLine(transform.position, currentWaypoint);

            for (int i = targetWaypointIndex; i < path.Length; i++)
            {

                Gizmos.color = Color.green;
                if (i == targetWaypointIndex)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(transform.position, path[i]);
                }
                else
                {
                    Gizmos.DrawLine(path[i - 1], path[i]);
                }
                Gizmos.DrawCube(path[i], Vector3.one);
            }
        }
    }
}
