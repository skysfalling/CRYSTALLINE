using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRMovementManager : MonoBehaviour
{
    public Rigidbody playerRb;

    public CustomXRHandMovement leftHandMovement;
    public CustomXRHandMovement rightHandMovement;
    
    public bool inAirMovement;
    public bool isClimbing;

    [Header("isGrounded")]
    public LayerMask groundLayer;
    public float checkGroundDist = 1;
    public bool isGrounded;


    [Space(10)]
    public int jumpCount = 2;
    int init_jumpCountValue;

    private void Awake()
    {
        init_jumpCountValue = jumpCount;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        isGrounded = CheckIfGrounded();
        HandleJumpCount();

        if (jumpCount > 0)
            inAirMovement = true;
        else
            inAirMovement = false;

        isClimbing = CheckIfClimbing();
        if (CheckIfClimbing()) { 
            playerRb.useGravity = false;
            //playerRb.velocity = new Vector3(playerRb.velocity.x, playerRb.velocity.y * 0.2f, playerRb.velocity.z);
        }

        else { playerRb.useGravity = true; }
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

    public void HandleJumpCount()
    {
        if (isGrounded)
            jumpCount = 1;
        else
            jumpCount = 0;

    }

}
