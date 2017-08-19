using UnityEngine;
using System.Collections;

public class Goblin : MonoBehaviour {

    static float attackRange = 2f;
    static float attackRadius = .75f;

    

    private Animator animator;
    private CharacterController controller;
    private EnemyScript enemyScript;

    private GameObject leftClaw;
    private GameObject rightClaw;

    int speedHash = Animator.StringToHash("Speed");
    int attackHash = Animator.StringToHash("Attack");

    // Use this for initialization
    void Start () {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        enemyScript = GetComponent<EnemyScript>();

        leftClaw = transform.FindChild("Armature").FindChild("Body").FindChild("Shoulder_L").FindChild("UpperArm_L").FindChild("LowerArm_L").FindChild("Hand_L").FindChild("goblin claw").gameObject;
        leftClaw.SetActive(false);
        rightClaw = transform.FindChild("Armature").FindChild("Body").FindChild("Shoulder_R").FindChild("UpperArm_R").FindChild("LowerArm_R").FindChild("Hand_R").FindChild("goblin claw").gameObject;
        rightClaw.SetActive(false);
    }
    
	void LateUpdate () {
        if (enemyScript.isDead)
        {
            if (GetComponent<EnemyPathing>() == null || !GetComponent<EnemyPathing>().pathRequested)
            {
                Destroy(gameObject);
            }
            
            return;
        }

        if (enemyScript.agro)
        {
            leftClaw.SetActive(true);
            rightClaw.SetActive(true);

            float distToPlayer = Vector3.Distance(transform.position, Player.GetPosition());
            if (distToPlayer < attackRange) // in position to attack the player
            {
                animator.SetTrigger(attackHash);
            }
        }
        else
        {
            leftClaw.SetActive(false);
            rightClaw.SetActive(false);
        }

        animator.SetFloat(speedHash, Mathf.Abs(controller.velocity.x) + Mathf.Abs(controller.velocity.z));
    }

    

    public void BasicAttackDealDamage()
    {
        var layerMask = (1 << LayerMask.NameToLayer("Player"));
        if (Physics.CheckSphere(transform.position + transform.forward * attackRadius, attackRadius, layerMask))
        {
            Player.instance.takeDamage(10);
        }

        //GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //sphere.transform.position = transform.position + transform.forward * meleeAttackRadius;
    }

}
