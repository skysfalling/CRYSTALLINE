using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class FirstPersonMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed;
    public float groundDrag;

    [Space(10)]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;
    public float gravity = 9.5f;

    [Space(10)]
    public LayerMask climbableLayer;
    public float climbSpeed = 5;
    public bool canClimb;

    [HideInInspector] public float walkSpeed;
    [HideInInspector] public float sprintSpeed;

    [Header("Keybinds")]
    public Keyboard keyboard;
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    public Transform orientation;

    public InputAction playerMove;
    public InputAction playerJump;


    public Vector3 moveDirection;

    Rigidbody rb;

    public void OnEnable()
    {
        playerMove.Enable();
        playerJump.Enable();
    }

    public void OnDisable()
    {
        playerMove.Disable();
        playerJump.Disable();
    }

    private void Start()
    {
        keyboard = Keyboard.current;

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        readyToJump = true;
    }

    private void Update()
    {
        // ground check
        grounded = Physics.CheckSphere(transform.position, 1, whatIsGround);

        MyInput();
        SpeedControl();

        // handle drag
        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;

        // climb check
        canClimb = Physics.CheckSphere(transform.position, 0.5f, climbableLayer);

    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {

        // when to jump
        if(playerJump.triggered && readyToJump && grounded)
        {
            readyToJump = false;

            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void MovePlayer()
    {
        // calculate movement direction
        //moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        // vertical input                                     // horizontal input
        moveDirection = (orientation.forward * playerMove.ReadValue<Vector2>().y) + (orientation.right * playerMove.ReadValue<Vector2>().x);
        moveDirection.y *= 0; // negate any vertical movement in moveDirection

        // if moving
        if (moveDirection.normalized != Vector3.zero)
        {
            if (canClimb)
            {
                moveDirection.y = climbSpeed;
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
            }

            // on ground
            else if (grounded)
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

            // in air
            else if (!grounded)
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }



        rb.velocity += new Vector3(0, -gravity, 0);

    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // limit velocity if needed
        if(flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        // reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    private void ResetJump()
    {
        readyToJump = true;
    }
}