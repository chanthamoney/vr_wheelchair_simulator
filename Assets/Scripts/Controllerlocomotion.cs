using UnityEngine;

public class Controllerlocomotion : MonoBehaviour
{
    // variable to keep track of navigation
    public int nav = 0; // 0 is view 1 is hand
    // determines which controller should be used for locomotion
    public enum Controller { Left, Right };
    public Controller controller = Controller.Right;

    // the maximum movement speed in meters per second
    public float maxSpeed = 3.0f;

    // the deadzone is the area close to the center of the thumbstick
    public float moveDeadzone = 0.2f;

    OVRCameraRig cameraRig = null;

    // the deadzone is the area close to the center of the thumbstick
    public float turnDeadzone = 0.75f;

    // to know if we turn
    public bool isTurn = false;

    public Color lineColor = Color.black;
    public float lineWidth = 0.01f;
    public bool isPressed = false;
    private LineRenderer lineRenderer;
    public Vector3 rayPoint;

    // that one instant
    public float transitionTime = 0.1f;
    public float transitionDistance = 4.0f;

    Vector3 startPosition = Vector3.zero;
    Vector3 endPosition = Vector3.zero;
    float transitionProgress = 1.0f;

    // Start is called before the first frame update
    void Start()
    {
        // this script is meant to be used on the OVRCameraRig game object
        cameraRig = GetComponent<OVRCameraRig>();
        lineRenderer = cameraRig.rightHandAnchor.gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.widthMultiplier = lineWidth;
        lineRenderer.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        // if a is pressed
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            if (nav == 0)
            {
                nav = 1;
            }
            else
            {
                nav = 0;
            }
        }
        // stores the x and y values of the thumbstick
        Vector2 thumbstickVector = new Vector2();

        // read the thumbstick values from either the right or left controller
        if (controller == Controller.Right)
            thumbstickVector = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
        else
            thumbstickVector = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);

        // switch from view to hand 
        if (nav == 0)
        {
            // if the thumbstick has been pushed outside the dead zone
            if (thumbstickVector.y > moveDeadzone || thumbstickVector.y < -moveDeadzone)
            {
                // COMPLETE THIS SECTION OF CODE

                // step 1 - create a Vector3 that contains the values for movement
                // this calculation will require maxSpeeed, thumstickVector.y, and Time.deltaTime
                Vector3 movement = new Vector3(0, 0, Time.deltaTime * maxSpeed * thumbstickVector.y);

                // step 2 - multiply by movement vector by the head orientation
                // this can be retrieved using cameraRig.centerEyeAnchor.rotation
                var mul = cameraRig.centerEyeAnchor.rotation * movement;

                // step 3 - add this movement vector to the current position of the game object
                // this can be found using transform.localPosition

                var curPos = transform.localPosition + mul;
                transform.position = curPos;

            }
            // snap turns
            if (thumbstickVector.x > turnDeadzone)
            {
                if (isTurn == false) // false
                {
                    transform.rotation *= Quaternion.AngleAxis(45, Vector3.up);
                    isTurn = true;
                }
            }

            if (thumbstickVector.x == 0)
            {
                isTurn = false;
            }
            if (thumbstickVector.x < -turnDeadzone)
            {
                if (isTurn == false)
                {
                    transform.rotation *= Quaternion.AngleAxis(-45, Vector3.up);
                    isTurn = true;
                }
            }
        }

        else
        {

            // snap turns
            if (thumbstickVector.x > turnDeadzone)
            {
                if (isTurn == false)
                {
                    transform.rotation *= Quaternion.AngleAxis(45, Vector3.up);
                    // rightHand.transform.rotation *= cameraRig.centerEyeAnchor.rotation;
                    isTurn = true;
                }
            }

            if (thumbstickVector.x == 0)
            {
                isTurn = false;
            }
            if (thumbstickVector.x < -turnDeadzone)
            {
                if (isTurn == false)
                {
                    transform.localRotation *= Quaternion.AngleAxis(-45, Vector3.up);
                    //rightHand.transform.rotation *= cameraRig.centerEyeAnchor.rotation;
                    isTurn = true;

                }
            }
            // HAND DIRECTED STEERING
            // if the thumbstick has been pushed outside the dead zone
            if (thumbstickVector.y > moveDeadzone || thumbstickVector.y < -moveDeadzone)
            {
                // COMPLETE THIS SECTION OF CODE

                // step 1 - create a Vector3 that contains the values for movement
                // this calculation will require maxSpeeed, thumstickVector.y, and Time.deltaTime
                Vector3 movement = new Vector3(0, 0, Time.deltaTime * maxSpeed * thumbstickVector.y);

                // step 2 - multiply by movement vector by the controller orientation
                // this can be retrieved using cameraRig.centerEyeAnchor.rotation
                // var mul = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTrackedRemote) * movement;
                var mul = cameraRig.rightHandAnchor.rotation * movement;

                // step 3 - add this movement vector to the current position of the game object
                // this can be found using transform.localPosition

                var curPos = transform.localPosition + mul;
                transform.position = curPos;
            }

        }
        // get the current position
        Vector3 currentPosition = cameraRig.rightHandAnchor.transform.position;
        // perform a physics raycast in the forward direction
        // if we hit an object, get the selectable component
        RaycastHit hitInfo;
        if (Physics.Raycast(currentPosition, cameraRig.rightHandAnchor.transform.TransformDirection(Vector3.forward), out hitInfo, Mathf.Infinity))
        {
            Debug.Log("HIT ITEM");
        }

        // thumbstick button is pressed down
        if (OVRInput.Get(OVRInput.Button.SecondaryThumbstick))
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, currentPosition);
            lineRenderer.SetPosition(1, hitInfo.point);
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
            rayPoint = hitInfo.point;
            startPosition = transform.position;
            endPosition = hitInfo.point;

            if (hitInfo.point == new Vector3(0, 0, 0))
            {
                lineRenderer.enabled = false;
            }
        }
        if (OVRInput.Get(OVRInput.Button.SecondaryThumbstick) == false)
        {
            lineRenderer.enabled = false;

            if (rayPoint != new Vector3(0, 0, 0))
            {
                transitionProgress = 0;

            }
            rayPoint = new Vector3(0, 0, 0);

        }

        if (transitionProgress < 1)
        {
            transitionProgress += Time.deltaTime / transitionTime;
            transform.position = Vector3.Lerp(startPosition, endPosition, transitionProgress);
        }

    }
}