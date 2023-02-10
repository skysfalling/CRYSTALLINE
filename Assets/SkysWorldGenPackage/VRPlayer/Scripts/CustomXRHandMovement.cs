using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class CustomXRHandMovement : MonoBehaviour
{
    public XR_InputManager inputManager;
    public VRMovementManager movementManager;
    public bool isLeftController;
    public bool isRightController;

    [Header("Physics Hands")]
    public XR_PhysicsHand physicsHand;

    [Header("Control Values")]
    [Range(0, 1)]
    public float gripDeadzone = 0.5f;
    public bool isGripping;

    [Header("Climbing")]
    public LayerMask climbLayer;
    public float triggerRadius = 0.1f;
    public bool onClimbObj;


    void Start()
    {
        if (isLeftController && isRightController) { Debug.LogError("Um... which controller is this?"); }
    }

    void FixedUpdate()
    {
        // <<<< ENABLE / DISABLE MOVEMENT >>>>

        onClimbObj = HandOnClimbableObj();

        // movement enabled
        if ( (movementManager.inAirMovement || HandOnClimbableObj()) && CheckControllerGrip())
        {
            physicsHand.hookesLawEnabled = true;

        }
        // movement disabled
        else
        {
            physicsHand.hookesLawEnabled = false;
        }


    }

    // <<<< CHECK IF CAN CLIMB >>>>
    public bool HandOnClimbableObj()
    {
        Collider[] foundColliders = Physics.OverlapSphere(transform.position, triggerRadius, climbLayer);

        if (foundColliders.Length > 0)
            return true;
        else
            return false;
    }

    // <<<< IS CONTROLLER GRIPPING? >>>>
    public bool CheckControllerGrip()
    {
        // if left or right controller gripping ...
        if (((isLeftController && inputManager.leftGrip_value > gripDeadzone) ||
            (isRightController && inputManager.rightGrip_value > gripDeadzone)))
        {
            return true;
        }
        else { return false; }
    }

    public bool GetThisPrimaryButton()
    {
        if (isLeftController) { return inputManager.l_primary_button; }
        else if (isRightController) { return inputManager.r_primary_button; }

        return false;
    }

    public bool GetThisSecondaryButton()
    {
        if (isLeftController) { return inputManager.l_secondary_button; }
        else if (isRightController) { return inputManager.r_secondary_button; }

        return false;
    }

    public bool GetTHisJoystickClick()
    {
        if (isLeftController) { return inputManager.l_stick_click; }
        else if (isRightController) { return inputManager.r_stick_click; }

        return false;
    }
}
