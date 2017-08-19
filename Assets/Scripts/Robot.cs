using UnityEngine;
using System.Collections;

public class Robot : MonoBehaviour {

    static float attackRange = 15f;
    static float reloadTime = 2;

    public GameObject missile;
    public AudioClip fireMissileSound;
    public Transform missileLaunchTransform;
    
    Animator anim;
    CharacterController controller;
    AudioSource audioSource;
    EnemyScript enemyScript;
    
    GunAimController aimController;
    HeadLookController headLookController;
    //private Transform gunHandTransform;
    private Vector3 lastSeenPlayerPosition;
    
    private float timeSinceLastFired = 0;
    
    int speedHash = Animator.StringToHash("Speed");
    //int fireHash = Animator.StringToHash("Fire");

    // Use this for initialization
    void Start () {
        anim = GetComponent<Animator>();
        enemyScript = GetComponent<EnemyScript>();
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        
        headLookController = GetComponent<HeadLookController>();
        headLookController.targetObject = Player.instance.gameObject;
        headLookController.trackTarget = false;

        aimController = GetComponent<GunAimController>();
        aimController.targetObject = Player.instance.gameObject;
        aimController.takeAim = false;
        
        //gunHandTransform = transform.FindChild("armature").FindChild("root").FindChild("body").FindChild("shoulder_R").FindChild("hand_R");
        missileLaunchTransform = transform.FindChild("armature").FindChild("root").FindChild("body").FindChild("shoulder_R").FindChild("hand_R").FindChild("MissileLaunchTransform");
    }
    
	void Update () {
        if (enemyScript.isDead)
        {
            if (GetComponent<EnemyPathing>() == null || !GetComponent<EnemyPathing>().pathRequested)
            {
                Destroy(gameObject);
            }

            return;
        }

        var towardsPlayer = Player.GetPosition() - transform.position;
        towardsPlayer.y = 0;
        towardsPlayer = towardsPlayer.normalized;

        if (enemyScript.agro)
        {
            if (enemyScript.targetInSight)
            {
                //controller.transform.rotation = Quaternion.RotateTowards(controller.transform.rotation, Quaternion.LookRotation(towardsPlayer), Time.deltaTime * turnSpeed);

                //anim.SetTrigger(fireHash);
                if (timeSinceLastFired >= reloadTime)
                {
                    FireMissile();
                    timeSinceLastFired = 0;
                }

                timeSinceLastFired += Time.deltaTime;
            }

            aimController.takeAim = true;
            headLookController.trackTarget = true;
        }
        else
        {
            aimController.takeAim = false;
            headLookController.trackTarget = false;
        }
        
    }

    void LateUpdate()
    {
        anim.SetFloat(speedHash, controller.velocity.x + controller.velocity.z);
    }

    public void FireMissile()
    {
        //Instantiate(missile, gunHandTransform.transform.position, gunHandTransform.transform.rotation * Quaternion.Euler(0, 0, 0));
        Instantiate(missile, missileLaunchTransform.position, missileLaunchTransform.rotation);
        PlaySound(fireMissileSound);
    }

    void PlaySound(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.Play();
    }

}
