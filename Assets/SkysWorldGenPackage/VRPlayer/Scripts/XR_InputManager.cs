using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class XR_InputManager : MonoBehaviour
{
    public InputDevice headset;
    List<InputDevice> foundHeadsets = new List<InputDevice>();
    InputDeviceCharacteristics headsetCharacteristics = InputDeviceCharacteristics.HeadMounted;


    public InputDevice leftController;
    List<InputDevice> foundLeftControllers = new List<InputDevice>();
    InputDeviceCharacteristics leftControllerCharacteristics = InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;
    
    
    public InputDevice rightController;
    List<InputDevice> foundRightControllers = new List<InputDevice>();
    InputDeviceCharacteristics rightControllerCharacteristics = InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;

    public bool allDevicesFound;

    [Header("Trigger")]
    public float leftTrigger_value;
    public float rightTrigger_value;

    [Header("Grab")]
    public float leftGrip_value;
    public float rightGrip_value;

    [Header("Left Buttons")]
    public bool l_primary_button;
    public bool l_secondary_button;
    public bool menu_button;
    public bool l_stick_click;

    [Header("Right Buttons")]
    public bool r_primary_button;
    public bool r_secondary_button;
    public bool r_stick_click;


    void Start()
    {
        StartCoroutine(GetDevices());
    }

    public IEnumerator GetDevices()
    {
        InputDevices.GetDevicesWithCharacteristics(headsetCharacteristics, foundHeadsets);
        InputDevices.GetDevicesWithCharacteristics(leftControllerCharacteristics, foundLeftControllers);
        InputDevices.GetDevicesWithCharacteristics(rightControllerCharacteristics, foundRightControllers);
    
        // if no controllers found, try again
        if (foundLeftControllers.Count == 0 || foundRightControllers.Count == 0 || foundHeadsets.Count == 0)
        {
            yield return new WaitForSeconds(0.5f);
            StartCoroutine(GetDevices());
        }
        else
        {
            headset = foundHeadsets[0];
            leftController = foundLeftControllers[0];
            rightController = foundRightControllers[0];

            Debug.Log("Found Devices");
            allDevicesFound = true;
            Debug.Log(headset.name + headset.characteristics);
            Debug.Log(leftController.name + leftController.characteristics);
            Debug.Log(rightController.name + rightController.characteristics);
        }

        yield return null;
    }

    private void FixedUpdate()
    {
        GetControllerValues();


    }

    public void GetControllerValues()
    {

        // GRIP =================================================
        leftController.TryGetFeatureValue(CommonUsages.grip, out leftGrip_value);
        rightController.TryGetFeatureValue(CommonUsages.grip, out rightGrip_value);

        if (leftGrip_value < 0.01) leftGrip_value = 0;
        if (rightGrip_value < 0.01) rightGrip_value = 0;


        // TRIGGER =================================================

        leftController.TryGetFeatureValue(CommonUsages.trigger, out leftTrigger_value);
        rightController.TryGetFeatureValue(CommonUsages.trigger, out rightTrigger_value);

        if (leftTrigger_value < 0.01) leftTrigger_value = 0;
        if (rightTrigger_value < 0.01) rightTrigger_value = 0;

        // BUTTONS ================================================
        leftController.TryGetFeatureValue(CommonUsages.primaryButton, out l_primary_button);
        rightController.TryGetFeatureValue(CommonUsages.primaryButton, out r_primary_button);

        leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out l_secondary_button);
        rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out r_secondary_button);

        leftController.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out l_stick_click);
        rightController.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out r_stick_click);

        leftController.TryGetFeatureValue(CommonUsages.menuButton, out menu_button);

    }

}
