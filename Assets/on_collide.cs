using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class on_collide : MonoBehaviour
{
    public AudioSource edelgard_gang;
    public bool canPlay;
    // Start is called before the first frame update
    void Start()
    {
        edelgard_gang = GetComponent<AudioSource>();
        canPlay = true;
    }

    // Update is called once per frame
    void Update()
    {
        GameObject me = GameObject.Find("OVRPlayerController");
        float distance = (me.transform.position.x - transform.position.x) * (me.transform.position.x - transform.position.x) +
            (me.transform.position.y - transform.position.y) * (me.transform.position.y - transform.position.y) +
            (me.transform.position.z - transform.position.z) * (me.transform.position.z - transform.position.z);
        Debug.Log("Distance: " + distance);
        if (distance < 25.0f && canPlay)
        {
            canPlay = false;
            edelgard_gang.Play();
            Debug.Log("EDELGARD GANG");
        }
        if (distance > 50.0f)
        {
            canPlay = true;
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        edelgard_gang.Play();
        Debug.Log("EDELGARD GANG");
    }
}
