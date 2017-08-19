using UnityEngine;
using System.Collections;

public class TrailBounce : MonoBehaviour {

    // Update is called once per frame

    public float t = 0;
    bool up_down = true;
    public float tolerance = 0.1f;
    public Vector3 direction = Vector3.up;


    void Update()
    {


        move();
    }


    void move()
    {

        t += Time.deltaTime;

        if (t > tolerance)
        {

            if (up_down)
            {
                gameObject.transform.position += direction * 0.13f;
                t = 0;
                up_down = false;
            }
            else
            {
                gameObject.transform.position -= direction * 0.13f;
                t = 0;
                up_down = true;
            }
        }


    }
}
