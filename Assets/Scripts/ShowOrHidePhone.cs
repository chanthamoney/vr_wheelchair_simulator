using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowOrHidePhone : MonoBehaviour
{

    public GameObject phone;
    public GameObject collider;
    private bool isActive;

    // Start is called before the first frame update
    void Start()
    {
        phone.active = true;
        collider.active = true;
        isActive = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (isActive)
        {
            phone.active = true;
            collider.active = true;
        } else {
            phone.active = false;
            collider.active = false;
        }
        if (OVRInput.Get(OVRInput.Button.One))
            isActive = !isActive;
    }
}
