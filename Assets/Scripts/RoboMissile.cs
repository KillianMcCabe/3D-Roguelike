using UnityEngine;
using System.Collections;

public class RoboMissile : MonoBehaviour
{

    public static float speed = 20;
    [SerializeField, Tooltip("Explsosion to be spawned at point of impact.")]
    public GameObject explosionPrefab;
    public float lifetime = 5;
    float explosionRadius = 5;
    float explosionDamage = 20;
    
    private Vector3 direction;
    Rigidbody rb;
    
    // Use this for initialization
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        //direction = -transform.right + (transform.up * Random.Range(-0.025f, 0.025f)) + (transform.forward * Random.Range(-0.025f, 0.025f));
        direction = transform.forward + (transform.up * Random.Range(-0.025f, 0.025f)) + (transform.right * Random.Range(-0.025f, 0.025f));
        rb.AddForce(direction * speed, ForceMode.Impulse);
        
    }

    // Update is called once per frame
    void Update()
    {
        //rb.AddForce(-transform.right * speed * Time.deltaTime, ForceMode.Acceleration);
        rb.AddForce(direction * speed *4 * Time.deltaTime, ForceMode.Acceleration);

        lifetime -= Time.deltaTime;

        if (lifetime <= 0)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider col)
    {
        //add an explosion or something
        // CAMERA SHAKING - V IMPORTANT!
        if (lifetime <= 4.95)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.Euler(0, Random.Range(0, 360), 0));

            float dist = Vector3.Distance(transform.position, Player.instance.transform.position);
            float impactEffect = Mathf.Clamp(1 - (dist / explosionRadius), 0, 1);
            Player.instance.takeDamage(explosionDamage * impactEffect);

            Destroy(gameObject);
        }
    }
    
}
