using UnityEngine;
using System.Collections;

public class Explosion : MonoBehaviour {

    public float lifetime = 5;
    public float damage = 3; // amount of time the sound clip takes to end

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        lifetime -= Time.deltaTime;

        if (lifetime <= 0)
        {
            Destroy(gameObject);
        }
    }
}
