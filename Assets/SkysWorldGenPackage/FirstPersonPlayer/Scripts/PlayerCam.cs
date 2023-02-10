using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCam : MonoBehaviour
{
    public InputAction mouseXpos;
    public InputAction mouseYpos;

    public float sensX, sensY;
    public Transform orientation;
    float xRotation, yRotation;

    private void OnEnable()
    {
        mouseXpos.Enable();
        mouseYpos.Enable();
    }

    private void OnDisable()
    {
        mouseXpos.Disable();
        mouseYpos.Disable();
    }

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        // get mouse input
        float mouseX = mouseXpos.ReadValue<float>() * Time.deltaTime * sensX;
        float mouseY = mouseYpos.ReadValue<float>() * Time.deltaTime * sensY;

        yRotation += mouseX;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //rotate cam and orientation
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(xRotation, yRotation, 0);

    }
}
