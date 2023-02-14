using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRMovementManager : MonoBehaviour
{
    XR_InputManager inputManager;
    public GameManager gameManager;
    public Rigidbody playerRb;

    public CustomXRHandMovement leftHandMovement;
    public CustomXRHandMovement rightHandMovement;


    [Header("Forces")]
    public float baseMovementForce = 100;


    [Header("Bools")]
    public bool inAirMovement;
    public bool isClimbing;

    [Header("isGrounded")]
    public LayerMask groundLayer;
    public float checkGroundDist = 1;
    public bool isGrounded;

    [Header("isSwinging")]
    public bool isSwinging;
    public bool startedSwing;
    public SpringJoint swingJoint;


    [Space(10)]
    public int jumpCount = 2;
    int init_jumpCountValue;

    private void Awake()
    {
        inputManager = GetComponentInParent<XR_InputManager>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        init_jumpCountValue = jumpCount;
    }

    private void Update()
    {
        if (inputManager.allDevicesFound)
        {
            BasicMove(inputManager.l_joystick);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        isGrounded = CheckIfGrounded();
        HandleJumpCount();

        /*
        if (jumpCount > 0)
            inAirMovement = true;
        else
            inAirMovement = false;
            */

        inAirMovement = false;

        isClimbing = CheckIfClimbing();
        if (CheckIfClimbing()) { 
            playerRb.useGravity = false;
            //playerRb.velocity = new Vector3(playerRb.velocity.x, playerRb.velocity.y * 0.2f, playerRb.velocity.z);
        }

        else { playerRb.useGravity = true; }


        isSwinging = CheckIfSwinging();
        if (isSwinging)
        {
            GameObject swingObject = leftHandMovement.GetSwingableObject();
            if (swingObject == null) { swingObject = rightHandMovement.GetSwingableObject(); }


            Rigidbody swingRb = swingObject.GetComponent<Rigidbody>();

            swingRb.velocity = playerRb.velocity;

            // playerRb.velocity = swingRb.velocity;

            /*
            swingJoint = playerRb.gameObject.AddComponent<SpringJoint>();
            swingJoint.autoConfigureConnectedAnchor = false;
            swingJoint.connectedAnchor = swingObject.transform.position;

            float distanceFromPoint = Vector3.Distance(playerRb.transform.position, swingObject.transform.position);

            swingJoint.maxDistance = distanceFromPoint * 0.4f;
            swingJoint.minDistance = distanceFromPoint * 0.1f;

            swingJoint.spring = 4f;
            swingJoint.damper = 10f;
            swingJoint.massScale = 5f;
            */
            
        }
        else if (!leftHandMovement.CheckControllerGrip())
        {
            //Destroy(swingJoint);

            startedSwing = false;
        }

    }

    public void BasicMove(Vector2 joystickInput)
    {

        Vector3 forwardDirection = Camera.main.transform.forward;
        Vector3 rightDirection = Camera.main.transform.right;
        Vector3 movement = (forwardDirection * joystickInput.y + rightDirection * joystickInput.x).normalized;

        playerRb.AddForce(movement * baseMovementForce * playerRb.mass * Time.fixedDeltaTime);

    }

    public bool CheckIfGrounded()
    {
        Debug.DrawRay(transform.position, Vector3.down * checkGroundDist, Color.yellow);

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, checkGroundDist, groundLayer))
        {
            //Debug.Log("ground check: " + hit.collider.name);
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool CheckIfClimbing()
    {
        if ((leftHandMovement.HandOnClimbableObj() && leftHandMovement.CheckControllerGrip()) ||
            (rightHandMovement.HandOnClimbableObj() && rightHandMovement.CheckControllerGrip()))
        {
            return true;
        }
        else
            return false;
    }

    public bool CheckIfSwinging()
    {
        if ((leftHandMovement.GetSwingableObject() && leftHandMovement.CheckControllerGrip()) ||
            (rightHandMovement.GetSwingableObject() && rightHandMovement.CheckControllerGrip()))
        {
            return true;
        }
        else
            return false;
    }

    public void HandleJumpCount()
    {
        if (isGrounded)
            jumpCount = 1;
        else
            jumpCount = 0;

    }

}
