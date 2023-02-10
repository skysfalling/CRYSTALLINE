using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandUI : MonoBehaviour
{
    public VRMovementManager movementManager;
    public CustomXRHandMovement handMovement;
    public XR_InputManager inputManager;

    public Text ui;

    [SerializeField]private bool leftController;
    [SerializeField]private bool rightController;

    Dictionary<string, bool> bool_log = new Dictionary<string, bool>();
    Dictionary<string, int> int_log = new Dictionary<string, int>();
    Dictionary<string, float> float_log = new Dictionary<string, float>();

    public void LeftHandUI()
    {
        DisplayFloat("Trigger", inputManager.leftTrigger_value);
        DisplayFloat("Grip", inputManager.leftGrip_value);
        DisplayBool("Primary", inputManager.l_primary_button);
        DisplayBool("Secondary", inputManager.l_secondary_button);
        DisplayBool("Menu", inputManager.menu_button);

    }

    public void RightHandUI()
    {
        DisplayFloat("Trigger", inputManager.rightTrigger_value);
        DisplayFloat("Grip", inputManager.rightGrip_value);
        DisplayBool("Primary", inputManager.r_primary_button);
        DisplayBool("Secondary", inputManager.r_secondary_button);
    }


    private void Update()
    {
        ui.text = ""; // reset 

        if (leftController)
            LeftHandUI();
        else
            RightHandUI();

    }

    // ==================== VALUES =============================

    // display specific bool in hud
    public void DisplayBool(string name, bool value)
    {
        // update value in log
        if (bool_log.ContainsKey(name))
        {
            bool_log[name] = value;
        }
        else
        {
            bool_log.Add(name, value);
        }

        // update value in ui
        ui.text += name + ": " + value + "\r\n";

    }

    public void DisplayInt(string name, int value)
    {
        // update value in log
        if (int_log.ContainsKey(name))
        {
            int_log[name] = value;
        }
        else
        {
            int_log.Add(name, value);
        }

        // update value in ui
        ui.text += name + ": " + value + "\r\n";

    }

    public void DisplayFloat(string name, float value)
    {
        // update value in log
        if (float_log.ContainsKey(name))
        {
            float_log[name] = value;
        }
        else
        {
            float_log.Add(name, value);
        }

        // update value in ui
        ui.text += name + ": " + value + "\r\n";

    }
}
