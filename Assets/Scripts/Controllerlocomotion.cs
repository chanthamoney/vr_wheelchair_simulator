using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controllerlocomotion : MonoBehaviour
{
	// determines which controller should be used for locomotion
    public enum Controller { Left, Right };
    public Controller controller = Controller.Right;
	bool viewDirectedSteering = true;

	// the maximum movement speed in meters per second
    public float maxSpeed = 3.0f;
	
	// the deadzone is the area close to the center of the thumbstick
    public float moveDeadzone = 0.2f;
		
	// for snap turn functionality (step 3)
	public float snapturnThreshold = 0.75f;
	public bool canTurn = true;
	public bool canSwitch = true;

    OVRCameraRig cameraRig = null;
	public Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
		// this script is meant to be used on the OVRCameraRig game object
        cameraRig = GetComponent<OVRCameraRig>();
		rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
		if (canSwitch) {
			if (OVRInput.Get(OVRInput.Button.One)) viewDirectedSteering = !viewDirectedSteering; // check if button A was pressed and switch steering method if so.
			canSwitch = false;
		}
		
		if (!canSwitch) { // prevent weird issue where pressing the A button sometimes does not switch steering techniques?
			if (!OVRInput.Get(OVRInput.Button.One)) canSwitch = true;
		}
		
		// stores the x and y values of the thumbstick
		Vector2 thumbstickVector = new Vector2();

		// read the thumbstick values from either the right or left controller
		if (controller == Controller.Right)
			thumbstickVector = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
		else
			thumbstickVector = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
		
		if (canTurn) {
			// snap turn
			// threshold = 0.75, this is for left/right so we are limited to the x component
			
			if (thumbstickVector.x > snapturnThreshold) { // right?
				// turn instantaneously 45 degrees in the direction
				transform.RotateAround(transform.position, Vector3.up, 45);
				canTurn = false;
			} else if (thumbstickVector.x < -snapturnThreshold) { // left?
				transform.RotateAround(transform.position, Vector3.up, -45);
				canTurn = false;
			}
		}
		
		if (!canTurn) {
			if (thumbstickVector.x == 0) 
				canTurn = true;
		}
		
		if (viewDirectedSteering) {
			// if the thumbstick has been pushed outside the dead zone
			if (thumbstickVector.y > moveDeadzone || thumbstickVector.y < -moveDeadzone)
			{	
				// step 1 - create a Vector3 that contains the values for movement
				// this calculation will require maxSpeed, thumbstickVector.y, and Time.deltaTime
				Vector3 movement = new Vector3(0, 0, maxSpeed * thumbstickVector.y * Time.deltaTime);
				
				// step 2 - multiply by movement vector by the head orientation
				// this can be retrieved using cameraRig.centerEyeAnchor.rotation
				movement = cameraRig.centerEyeAnchor.rotation * movement;
				
				// step 3 - add this movement vector to the current position of the game object
				// this can be found using transform.localPosition
				transform.localPosition += movement;
			}
		} else {			
			// if the thumbstick has been pushed outside the dead zone
            if (thumbstickVector.y > moveDeadzone || thumbstickVector.y < -moveDeadzone)
            {
                // step 1 - create a Vector3 that contains the values for movement
                // this calculation will require maxSpeeed, thumstickVector.y, and Time.deltaTime
                Vector3 movement = new Vector3(0, 0, maxSpeed * thumbstickVector.y * Time.deltaTime);

				// step 2
                movement = cameraRig.rightHandAnchor.rotation * movement;

                // step 3 - add this movement vector to the current position of the game object
                // this can be found using transform.localPosition
                transform.localPosition += movement;
            }
		}
    }
	
	void OnCollisionEnter(Collision col) {
		Debug.Log("Test");
		rb.velocity = Vector3.zero;
		rb.angularVelocity = Vector3.zero;
		rb.freezeRotation = true;
	}
	
	void OnCollisionExit(Collision col) {
		Debug.Log("Test2");
		rb.freezeRotation = false;
	}
}
