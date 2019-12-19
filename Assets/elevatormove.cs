using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class elevatormove : MonoBehaviour
{
    float origY;
    bool goingDown;
    float newY;
    // Start is called before the first frame update
    void Start()
    {
        origY = transform.position.y;
        goingDown = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (goingDown)
        {
            newY = origY - 0.05f;
            if (newY < 0.25)
            {
                goingDown = false;
            }
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
        else
        {
            newY = origY - 0.05f;
            if (newY > 1.85)
            {
                goingDown = true;
            }
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }
}
