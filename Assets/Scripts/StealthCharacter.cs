using UnityEngine;
using System.Collections;

public class StealthCharacter : MonoBehaviour {
    
    private AbilityIconScript AbilityIcon1;
    private AbilityIconScript AbilityIcon2;

    [SerializeField, Tooltip("Blink Particle Effect")]
    private GameObject blinkParticlePrefab;
    [SerializeField]
    private GameObject blinkTargetParticlePrefab;

    [SerializeField]
    private AudioClip swipe1;
    [SerializeField]
    private AudioClip swipe2;
    [SerializeField]
    private AudioClip fleshHit;

    [SerializeField, Tooltip("Blink Particle Effect")]
    private GameObject bloodSplatter;

    [SerializeField]
    private GameObject trailObject;

    RenderEffects renderEffects;
    Animator animator;

    int attack1Hash = Animator.StringToHash("DaggerAttack1");
    int attack2Hash = Animator.StringToHash("DaggerAttack2");

    float meleeAttackRadius = .75f;

    float spell1CoolDown = 4;
    float jumpRange = 8;

    float spell2EffectTime = 3;
    float spell2CoolDown = 20;

    float timeSinceUsedSpell1 = 0;
    float timeSinceUsedSpell2 = 0;

    GameObject blinkTarget;

    // Use this for initialization
    void Start () {
        animator = GetComponent<Animator>();
        renderEffects = Camera.main.GetComponent<RenderEffects>();

        AbilityIcon1 = GameObject.Find("Ability1").GetComponent<AbilityIconScript>();
        AbilityIcon2 = GameObject.Find("Ability2").GetComponent<AbilityIconScript>();

        timeSinceUsedSpell1 = spell1CoolDown;
        timeSinceUsedSpell2 = spell2CoolDown;

        blinkTarget = Instantiate(blinkTargetParticlePrefab, Player.instance.transform.position, Quaternion.Euler(0, Random.Range(0, 360), 0)) as GameObject;
        DontDestroyOnLoad(blinkTarget);
        blinkTarget.SetActive(false);
    }

    void OnLevelWasLoaded()
    {
        AbilityIcon1 = GameObject.Find("Ability1").GetComponent<AbilityIconScript>();
        AbilityIcon2 = GameObject.Find("Ability2").GetComponent<AbilityIconScript>();
    }

    // Update is called once per frame
    void Update () {
        //if (Input.GetButtonDown("Attack"))
        //{
        //    if (Random.Range(1, 100) < 50)
        //        animator.SetTrigger(attack1Hash);
        //    else
        //        animator.SetTrigger(attack2Hash);
        //}
        if (Input.GetButtonDown("Attack1"))
        {
            animator.SetTrigger(attack1Hash);
            SoundManager.instance.PlaySingle(swipe1);
        }

        if (Input.GetButtonDown("Attack2"))
        {
            animator.SetTrigger(attack2Hash);
            SoundManager.instance.PlaySingle(swipe2);
        }

        if (Input.GetButton("Spell1") && timeSinceUsedSpell1 > spell1CoolDown)
        {
            blinkTarget.SetActive(true);

            int x = Screen.width / 2;
            int y = Screen.height / 2;

            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(x, y));
            var layerMask = (1 << LayerMask.NameToLayer("Walkable")) | (1 << LayerMask.NameToLayer("Unwalkable")) | (1 << LayerMask.NameToLayer("HardTerrain"));
            float radius = .3f;
            int jumpRange = 10;

            if (!Physics.CheckSphere(ray.origin, radius, layerMask))
            {
                if (Physics.SphereCast(ray, radius, out hit, jumpRange, layerMask))
                {
                    blinkTarget.transform.position = hit.point;
                }
                else
                {
                    blinkTarget.transform.position = ray.GetPoint(jumpRange);
                }
            }
        }

        if (Input.GetButtonUp("Spell1") && timeSinceUsedSpell1 > spell1CoolDown)
        {
            int x = Screen.width / 2;
            int y = Screen.height / 2;

            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(x, y));
            var layerMask = (1 << LayerMask.NameToLayer("Walkable")) | (1 << LayerMask.NameToLayer("Unwalkable")) | (1 << LayerMask.NameToLayer("HardTerrain"));
            float radius = .3f;

            Instantiate(blinkParticlePrefab, Player.instance.transform.position, Quaternion.Euler(0, Random.Range(0, 360), 0));

            //GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //sphere.transform.position = transform.position + transform.forward * meleeAttackRadius;

            if (!Physics.CheckSphere(ray.origin, radius, layerMask))
            {
                if (Physics.SphereCast(ray, radius, out hit, jumpRange, layerMask))
                {
                    Player.instance.transform.position = hit.point;
                }
                else
                {
                    Player.instance.transform.position = ray.GetPoint(jumpRange);
                }

                GameObject go = Instantiate(blinkParticlePrefab, Player.instance.transform.position, Quaternion.Euler(0, Random.Range(0, 360), 0)) as GameObject;
                go.transform.SetParent(transform, true);
            }

            blinkTarget.SetActive(false);
            timeSinceUsedSpell1 = 0;
        }

        if (Input.GetButtonDown("Spell2") && timeSinceUsedSpell2 > spell2CoolDown)
        {
            renderEffects.Enable();
            timeSinceUsedSpell2 = 0;
        }

        timeSinceUsedSpell1 += Time.deltaTime;
        timeSinceUsedSpell2 += Time.deltaTime;
        if (timeSinceUsedSpell2 >= spell2EffectTime)
            renderEffects.Disable();

        AbilityIcon1.SetFillAmount(timeSinceUsedSpell1, spell1CoolDown);
        AbilityIcon2.SetFillAmount(timeSinceUsedSpell2, spell2CoolDown);
    }

    public void BasicAttackDealDamage()
    {
        GameObject go = Instantiate(trailObject, transform.position, transform.rotation) as GameObject;
        go.GetComponent<Rigidbody>().AddForce(go.transform.forward * 15, ForceMode.Impulse);

        var targets = Physics.OverlapSphere(transform.position + transform.forward * meleeAttackRadius, meleeAttackRadius);

        //GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //sphere.transform.position = transform.position + transform.forward * meleeAttackRadius;

        foreach (Collider target in targets)
        {
            EnemyHealth enemyHealth = target.gameObject.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(10);
                SoundManager.instance.PlaySingle(fleshHit);
                Instantiate(bloodSplatter, target.transform.position, Quaternion.identity);
            }
        }
    }
}
