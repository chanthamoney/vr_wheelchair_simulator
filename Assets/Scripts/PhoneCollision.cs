using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhoneCollision : MonoBehaviour
{
    public SpriteRenderer renderer1;
    public SpriteRenderer renderer2;
    public SpriteRenderer renderer3;
    private Vector3 originalPosition;
    private Vector3 originalWCPosition;
    private Quaternion originalWCOrientation;
    private Color originalColor1;
    private Color originalColor2;
    private Color originalColor3;
    public GameObject canvas;
    public bool isCanvasActive;
    public GameObject wheelChair;
    public GameObject chair;
    private bool isWalking;
    private bool isWheelchair;
	private bool isColliding;

    // Start is called before the first frame update
    void Start()
    {
        originalPosition = transform.localPosition;
        originalWCPosition = wheelChair.transform.position;
        originalWCOrientation = wheelChair.transform.rotation;
        originalColor1 = renderer1.color;
        originalColor2 = renderer2.color;
        originalColor3 = renderer3.color;
        isCanvasActive = canvas.activeInHierarchy;
        isWalking = false;
        isWheelchair = true;
    }

    // Update is called once per frame
    void Update()
    {
		isColliding = false;
        transform.localPosition = originalPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
		if (isColliding) return;
		isColliding = true;
        if (other.gameObject.name == "WheelchairCollider")
        {
            renderer1.color = new Color(0.783f, 0.783f, 0.783f, 1f);
            // set to wheelchair
            if (isWalking) // is walking now, switch to wheelchair
            {
                wheelChair.GetComponent<OVRPlayerController>().enabled = false;
				wheelChair.GetComponent<CharacterController>().enabled = false;
                wheelChair.transform.position = new Vector3(-4.99f, 1.922f, -9.18f);
                wheelChair.transform.rotation = Quaternion.Euler(0, 270, 0);
                chair.active = true;
                isWalking = false;
                isWheelchair = true;
                wheelChair.GetComponent<OvrPlayerControllerModified>().enabled = true;
				wheelChair.GetComponent<CharacterController>().enabled = true;
            }
            else // is wheelchair now, switch to walking
            {
                Debug.Log("Switching to walking");
                wheelChair.GetComponent<OvrPlayerControllerModified>().enabled = false;
				wheelChair.GetComponent<CharacterController>().enabled = false;
                wheelChair.transform.position = new Vector3(-4.99f, 1.922f, -9.18f);
                wheelChair.transform.rotation = Quaternion.Euler(0, 270, 0);
                chair.active = false;
                isWalking = true;
                isWheelchair = false;
				wheelChair.GetComponent<OVRPlayerController>().enabled = true;
				wheelChair.GetComponent<CharacterController>().enabled = true;
            }

            //walking.SetActive(isWalking);
            //wheelChair.SetActive(isWheelchair);
        }
        else if (other.gameObject.name == "WalkingCollider")
        {
            renderer2.color = new Color(0.783f, 0.783f, 0.783f, 1f);
            // set to walking
        }
        else if (other.gameObject.name == "MinimapCollider")
        {
            renderer3.color = new Color(0.783f, 0.783f, 0.783f, 1f);
            if (isCanvasActive == true)
            {
                isCanvasActive = false;
            }
            else
            {
                isCanvasActive = true;
            }
            canvas.SetActive(isCanvasActive);
            // show minimap
        }
        else
        { // hit something else, should not happen
            return;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        renderer1.color = originalColor1;
        renderer2.color = originalColor2;
        renderer3.color = originalColor3;
    }
}
