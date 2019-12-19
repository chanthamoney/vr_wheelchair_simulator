/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Licensed under the Oculus Utilities SDK License Version 1.31 (the "License"); you may not use
the Utilities SDK except in compliance with the License, which is provided at the time of installation
or download, or which otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at
https://developer.oculus.com/licenses/utilities-1.31

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows grabbing and throwing of objects with the OVRGrabbable component on them.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class OVRGrabber : MonoBehaviour
{
    public enum Controller { Left, Right };
    public Controller controller = Controller.Left;
    public float turnDeadzone = 0.4f;
    // Grip trigger thresholds for picking up objects, with some hysteresis.
    public float grabBegin = 0.55f;
    public float grabEnd = 0.35f;

    // Demonstrates parenting the held object to the hand's transform when grabbed.
    // When false, the grabbed object is moved every FixedUpdate using MovePosition.
    // Note that MovePosition is required for proper physics simulation. If you set this to true, you can
    // easily observe broken physics simulation by, for example, moving the bottom cube of a stacked
    // tower and noting a complete loss of friction.
    [SerializeField]
    protected bool m_parentHeldObject = false;

    // Child/attached transforms of the grabber, indicating where to snap held objects to (if you snap them).
    // Also used for ranking grab targets in case of multiple candidates.
    [SerializeField]
    protected Transform m_gripTransform = null;
    // Child/attached Colliders to detect candidate grabbable objects.
    [SerializeField]
    protected Collider[] m_grabVolumes = null;

    // Should be OVRInput.Controller.LTouch or OVRInput.Controller.RTouch.
    [SerializeField]
    protected OVRInput.Controller m_controller;

    [SerializeField]
    protected Transform m_parentTransform;

    [SerializeField]
    protected GameObject m_player;

    protected bool m_grabVolumeEnabled = true;
    protected Vector3 m_lastPos;
    protected Quaternion m_lastRot;
    protected Quaternion m_anchorOffsetRotation;
    protected Vector3 m_anchorOffsetPosition;
    protected float m_prevFlex;
    protected OVRGrabbable m_grabbedObj = null;
    protected Vector3 m_grabbedObjectPosOff;
    protected Quaternion m_grabbedObjectRotOff;
    protected Dictionary<OVRGrabbable, int> m_grabCandidates = new Dictionary<OVRGrabbable, int>();
    protected bool operatingWithoutOVRCameraRig = true;

    public float pushedForward;
    public bool grabbing;
    public bool turning;
    public float allowWheelMove;

    /// <summary>
    /// The currently grabbed object.
    /// </summary>
    public OVRGrabbable grabbedObject
    {
        get { return m_grabbedObj; }
    }

    public void ForceRelease(OVRGrabbable grabbable)
    {
        bool canRelease = (
            (m_grabbedObj != null) &&
            (m_grabbedObj == grabbable)
        );
        if (canRelease)
        {
            GrabEnd();
        }
    }

    protected virtual void Awake()
    {
        m_anchorOffsetPosition = transform.localPosition;
        m_anchorOffsetRotation = transform.localRotation;

        // If we are being used with an OVRCameraRig, let it drive input updates, which may come from Update or FixedUpdate.

        OVRCameraRig rig = null;
        if (transform.parent != null && transform.parent.parent != null)
            rig = transform.parent.parent.GetComponent<OVRCameraRig>();

        if (rig != null)
        {
            rig.UpdatedAnchors += (r) => { OnUpdatedAnchors(); };
            operatingWithoutOVRCameraRig = false;
        }
    }

    protected virtual void Start()
    {
        Debug.Log("STARTING...");
        m_lastPos = transform.position;
        m_lastRot = transform.rotation;
        if (m_parentTransform == null)
        {
            if (gameObject.transform.parent != null)
            {
                m_parentTransform = gameObject.transform.parent.transform;
            }
            else
            {
                m_parentTransform = new GameObject().transform;
                m_parentTransform.position = Vector3.zero;
                m_parentTransform.rotation = Quaternion.identity;
            }
        }
        grabbing = false;
        pushedForward = 0;
        turning = false;
        allowWheelMove = 0;
    }

    void FixedUpdate()
    {
        if (operatingWithoutOVRCameraRig)
            OnUpdatedAnchors();
    }

    // Hands follow the touch anchors by calling MovePosition each frame to reach the anchor.
    // This is done instead of parenting to achieve workable physics. If you don't require physics on
    // your hands or held objects, you may wish to switch to parenting.
    void OnUpdatedAnchors()
    {
        Vector3 handPos = OVRInput.GetLocalControllerPosition(m_controller);
        Quaternion handRot = OVRInput.GetLocalControllerRotation(m_controller);
        Vector3 destPos = m_parentTransform.TransformPoint(m_anchorOffsetPosition + handPos);
        Quaternion destRot = m_parentTransform.rotation * handRot * m_anchorOffsetRotation;
        GetComponent<Rigidbody>().MovePosition(destPos);
        GetComponent<Rigidbody>().MoveRotation(destRot);

        if (!m_parentHeldObject)
        {
            MoveGrabbedObject(destPos, destRot);
        }
        m_lastPos = transform.position;
        m_lastRot = transform.rotation;

        float prevFlex = m_prevFlex;
        // Update values from inputs
        m_prevFlex = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, m_controller);

        CheckForGrabOrRelease(prevFlex);
    }

    void OnDestroy()
    {
        if (m_grabbedObj != null)
        {
            GrabEnd();
        }
    }

    void OnTriggerEnter(Collider otherCollider)
    {
        Debug.Log("Trigger Pressed");
        // Get the grab trigger
        OVRGrabbable grabbable = otherCollider.GetComponent<OVRGrabbable>() ?? otherCollider.GetComponentInParent<OVRGrabbable>();
        if (grabbable == null) return;

        // Add the grabbable
        int refCount = 0;
        m_grabCandidates.TryGetValue(grabbable, out refCount);
        m_grabCandidates[grabbable] = refCount + 1;
    }

    void OnTriggerExit(Collider otherCollider)
    {
        OVRGrabbable grabbable = otherCollider.GetComponent<OVRGrabbable>() ?? otherCollider.GetComponentInParent<OVRGrabbable>();
        if (grabbable == null) return;

        // Remove the grabbable
        int refCount = 0;
        Debug.Log("TRIGGER EXITING...");
        bool found = m_grabCandidates.TryGetValue(grabbable, out refCount);
        if (!found)
        {
            return;
        }

        if (refCount > 1)
        {
            m_grabCandidates[grabbable] = refCount - 1;
        }
        else
        {
            m_grabCandidates.Remove(grabbable);
        }

    }

    protected void CheckForGrabOrRelease(float prevFlex)
    {
        if ((m_prevFlex >= grabBegin) && (prevFlex < grabBegin))
        {
            GrabBegin();
        }
        else if ((m_prevFlex <= grabEnd) && (prevFlex > grabEnd))
        {
            GrabEnd();
        }
    }

    protected virtual void GrabBegin()
    {
        Debug.Log("Grabbing Now!");
        float closestMagSq = float.MaxValue;
        grabbing = true;
        OVRGrabbable closestGrabbable = null;
        Collider closestGrabbableCollider = null;

        // Iterate grab candidates and find the closest grabbable candidate
        foreach (OVRGrabbable grabbable in m_grabCandidates.Keys)
        {
            bool canGrab = !(grabbable.isGrabbed && !grabbable.allowOffhandGrab);
            if (!canGrab)
            {
                continue;
            }

            for (int j = 0; j < grabbable.grabPoints.Length; ++j)
            {
                Collider grabbableCollider = grabbable.grabPoints[j];
                // Store the closest grabbable
                Vector3 closestPointOnBounds = grabbableCollider.ClosestPointOnBounds(m_gripTransform.position);
                float grabbableMagSq = (m_gripTransform.position - closestPointOnBounds).sqrMagnitude;
                if (grabbableMagSq < closestMagSq)
                {
                    closestMagSq = grabbableMagSq;
                    closestGrabbable = grabbable;
                    closestGrabbableCollider = grabbableCollider;
                }
            }
        }

        // Disable grab volumes to prevent overlaps
        GrabVolumeEnable(false);

        if (closestGrabbable != null)
        {
            if (closestGrabbable.isGrabbed)
            {
                closestGrabbable.grabbedBy.OffhandGrabbed(closestGrabbable);
            }

            m_grabbedObj = closestGrabbable;
            m_grabbedObj.GrabBegin(this, closestGrabbableCollider);

            m_lastPos = transform.position;
            m_lastRot = transform.rotation;

            // Set up offsets for grabbed object desired position relative to hand.
            if (m_grabbedObj.snapPosition)
            {
                m_grabbedObjectPosOff = m_gripTransform.localPosition;
                if (m_grabbedObj.snapOffset)
                {
                    Vector3 snapOffset = m_grabbedObj.snapOffset.position;
                    if (m_controller == OVRInput.Controller.LTouch) snapOffset.x = -snapOffset.x;
                    m_grabbedObjectPosOff += snapOffset;
                }
            }
            else
            {
                Vector3 relPos = m_grabbedObj.transform.position - transform.position;
                relPos = Quaternion.Inverse(transform.rotation) * relPos;
                m_grabbedObjectPosOff = relPos;
            }

            if (m_grabbedObj.snapOrientation)
            {
                m_grabbedObjectRotOff = m_gripTransform.localRotation;
                if (m_grabbedObj.snapOffset)
                {
                    m_grabbedObjectRotOff = m_grabbedObj.snapOffset.rotation * m_grabbedObjectRotOff;
                }
            }
            else
            {
                Quaternion relOri = Quaternion.Inverse(transform.rotation) * m_grabbedObj.transform.rotation;
                m_grabbedObjectRotOff = relOri;
            }

            // Note: force teleport on grab, to avoid high-speed travel to dest which hits a lot of other objects at high
            // speed and sends them flying. The grabbed object may still teleport inside of other objects, but fixing that
            // is beyond the scope of this demo.
            MoveGrabbedObject(m_lastPos, m_lastRot, true);
            SetPlayerIgnoreCollision(m_grabbedObj.gameObject, true);
            if (m_parentHeldObject)
            {
                m_grabbedObj.transform.parent = transform;
            }
        }
    }

    // wheel.transform.GetChild(2);
    // Debug.Log("Player Controller Name: " + wheel.transform.GetChild(2).name);
    // Debug.Log("PC Old Local Pos: " + wheel.transform.GetChild(2).localPosition);
    // Debug.Log("PC Old Global Pos: " + wheel.transform.GetChild(2).position);
    // Debug.Log("Old Local Position: " + wheel.transform.localPosition);
    // Debug.Log("New Local Position: " + wheel.transform.localPosition);
    // Debug.Log("PC New Local Pos: " + wheel.transform.GetChild(2).localPosition);
    // Debug.Log("PC New Global Pos: " + wheel.transform.GetChild(2).position);
    //  Debug.Log(m_grabbedObj.name + " tea ");
    // wheel.transform.localPosition
    // += Vector3.forward * Time.deltaTime * 1.0f;
    // Debug.Log("Local Position: "+ wheel.transform.localPosition);
    // Debug.Log("~~~~~~~~");
    // Debug.Log("Not Local: " + wheel.transform.position);
    // wheel.transform.localPosition =
    // new Vector3(wheel.transform.localPosition.x, wheel.transform.localPosition.y, wheel.transform.localPosition.z+.1f);
    // Debug.Log("2nd Local Position: " + wheel.transform.localPosition);
    // Debug.Log("~~~~~~~~");
    // Debug.Log("2nd Not Local: " + wheel.transform.position);

    float getPushedForward()
    {
        return pushedForward;
    }

    void setPushedForward(float change)
    {
        pushedForward = change;
    }



    void MoveForward(GameObject wheel, float move)
    {
        Debug.Log("fuck me");
        if (move < 0.005 && move > 0.005)
        {
            return;
        }
        Vector3 curPos = new Vector3(0, 0, move*0.1f);
        curPos = wheel.transform.rotation * curPos;
        wheel.transform.localPosition += curPos;
        Quaternion initialRotation = m_grabbedObj.transform.localRotation;
        m_grabbedObj.transform.localRotation = Quaternion.Euler(30 * Time.deltaTime, 0, 0) * initialRotation;
        //if (allowWheelMove % 360 == 0)
        //{
        // m_grabbedObj.transform.RotateAround(m_grabbedObj.transform.position, Vector3.right, 30 * Time.deltaTime);
        //}
        return;
    }

    protected virtual void MoveGrabbedObject(Vector3 pos, Quaternion rot, bool forceTeleport = false)
    {
        // Debug.Log(m_grabbedObj.name + " quueen");
        GameObject wheel = GameObject.Find("OVRPlayerController");
        // Debug.Log(wheel.name + "pls");
        if (m_grabbedObj == null)
        {
            return;
        }
        float initial = rot.z;
        Rigidbody grabbedRigidbody = m_grabbedObj.grabbedRigidbody;
        Vector3 grabbablePosition = pos + rot * m_grabbedObjectPosOff;
        Quaternion grabbableRotation = rot * m_grabbedObjectRotOff;
        if (grabbing)
        {
            setPushedForward(initial);
            grabbing = false;
        }
        float diff = rot.z - getPushedForward();
        if (diff < 0)
        {
            diff *= -1;
        }
        // MoveForward(wheel, grabbableRotation.x-getPushedForward());
        if (diff > 0.05)
        {
            MoveForward(wheel, -diff);
        }
        // transform.eulerAngles = new Vector3(0, 0, 5.0f);
        // m_grabbedObj.transform.localRotation = Quaternion.AngleAxis(30, Vector3.up);
        // Debug.Log("FAVORITE FOOD" + m_grabbedObj.transform.rotation);
        // grabbableRotation = new Quaternion(0, 90, 0, grabbableRotation.w);

        // if (forceTeleport)
        // {
        //     //grabbedRigidbody.transform.position = grabbablePosition;
        //     //grabbedRigidbody.transform.rotation = grabbableRotation;
        // }
        // else
        // {
        //     //grabbedRigidbody.MovePosition(grabbablePosition);
        //     //grabbedRigidbody.MoveRotation(grabbableRotation);
        // }
    }



    protected void GrabEnd()
    {
        Debug.Log("Hey Time to End the Grab!");
        if (m_grabbedObj != null)
        {
            OVRPose localPose = new OVRPose { position = OVRInput.GetLocalControllerPosition(m_controller), orientation = OVRInput.GetLocalControllerRotation(m_controller) };
            OVRPose offsetPose = new OVRPose { position = m_anchorOffsetPosition, orientation = m_anchorOffsetRotation };
            localPose = localPose * offsetPose;

            OVRPose trackingSpace = transform.ToOVRPose() * localPose.Inverse();
            Vector3 linearVelocity = trackingSpace.orientation * OVRInput.GetLocalControllerVelocity(m_controller);
            Vector3 angularVelocity = trackingSpace.orientation * OVRInput.GetLocalControllerAngularVelocity(m_controller);

            GrabbableRelease(linearVelocity, angularVelocity);
        }

        // Re-enable grab volumes to allow overlap events
        GrabVolumeEnable(true);
        grabbing = false;
        setPushedForward(0f);
    }

    protected void GrabbableRelease(Vector3 linearVelocity, Vector3 angularVelocity)
    {
        m_grabbedObj.GrabEnd(linearVelocity, angularVelocity);
        if (m_parentHeldObject) m_grabbedObj.transform.parent = null;
        SetPlayerIgnoreCollision(m_grabbedObj.gameObject, false);
        m_grabbedObj = null;
    }

    protected virtual void GrabVolumeEnable(bool enabled)
    {
        if (m_grabVolumeEnabled == enabled)
        {
            return;
        }

        m_grabVolumeEnabled = enabled;
        for (int i = 0; i < m_grabVolumes.Length; ++i)
        {
            Collider grabVolume = m_grabVolumes[i];
            grabVolume.enabled = m_grabVolumeEnabled;
        }

        if (!m_grabVolumeEnabled)
        {
            m_grabCandidates.Clear();
        }
    }

    protected virtual void OffhandGrabbed(OVRGrabbable grabbable)
    {
        if (m_grabbedObj == grabbable)
        {
            GrabbableRelease(Vector3.zero, Vector3.zero);
        }
    }

    protected void SetPlayerIgnoreCollision(GameObject grabbable, bool ignore)
    {
        if (m_player != null)
        {
            Collider playerCollider = m_player.GetComponent<Collider>();
            if (playerCollider != null)
            {
                Collider[] colliders = grabbable.GetComponents<Collider>();
                foreach (Collider c in colliders)
                {
                    Physics.IgnoreCollision(c, playerCollider, ignore);
                }
            }
        }
    }

    void Update()
    {
        GameObject wheel = GameObject.Find("OVRPlayerController");
        Vector2 thumbstickVector = new Vector2();

        // read the thumbstick values from either the right or left controller
        if (controller == Controller.Left)
            thumbstickVector = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
        else
            thumbstickVector = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
        if (thumbstickVector.x > turnDeadzone)
        {
            if (turning)
            {
                int x = 0;
            }
            else
            {
                float rotating = 45;
                allowWheelMove += 45;
                wheel.transform.rotation *= Quaternion.AngleAxis(rotating, Vector3.up);
                turning = true;
            }
        }
        if (thumbstickVector.x == 0)
        {
            turning = false;
        }
        if (thumbstickVector.x < -turnDeadzone)
        {
            if (turning)
            {
                int x = 0;
            }
            else
            {
                float rotating = -45;
                allowWheelMove -= 45;
                wheel.transform.rotation *= Quaternion.AngleAxis(rotating, Vector3.up);
                turning = true;
            }
        }
    }
}
