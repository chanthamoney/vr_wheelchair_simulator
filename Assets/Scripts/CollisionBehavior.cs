using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionBehavior : MonoBehaviour
{
//	public Vector3 originalPosition;
	public Rigidbody rb;
	
    // Start is called before the first frame update
    void Start()
    {
		rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {

    }
	
	void OnCollisionEnter(Collision collided) {
		Debug.Log("hELP");
		Debug.Log("Is this working??");
		rb.velocity = Vector3.zero;
		rb.angularVelocity = Vector3.zero;
		rb.constraints = RigidbodyConstraints.FreezeAll;
		Unfreeze();
	}
	
	void Unfreeze() {
		rb.constraints = RigidbodyConstraints.None;
	}
}
