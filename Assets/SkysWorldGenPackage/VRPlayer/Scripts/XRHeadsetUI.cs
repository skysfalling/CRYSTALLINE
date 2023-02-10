using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class XRHeadsetUI : MonoBehaviour
{
    public VRMovementManager movementManager;
    public CustomXRHandMovement leftHandMovement;
    public CustomXRHandMovement rightHandMovement;

    [Header("Console")]
    public Text console;
    public int console_length = 10;
    List<string> console_log = new List<string>();

    [Header("Bools")]
    public Text values;
    Dictionary<string, bool> bool_log = new Dictionary<string, bool>();
    Dictionary<string, int> int_log = new Dictionary<string, int>();


    private void Start()
    {
    }
    private void Update()
    {
        UpdateHeadsetUI();

        DisplayInt("JumpCount", movementManager.jumpCount);


        DisplayBool("Grounded", movementManager.isGrounded);

        DisplayBool("Climbing", movementManager.isClimbing);

        DisplayBool("Left Can Climb", leftHandMovement.HandOnClimbableObj());
        DisplayBool("Right Can Climb", rightHandMovement.HandOnClimbableObj());


    }

    void UpdateHeadsetUI()
    {
        // console ================================
        console.text = ""; // reset 
        foreach (string str in console_log)
        {
            console.text += str + "\r\n";
        }


        // values ===================================
        values.text = ""; // reset
    }

    // =================== CONSOLE ===========================
    void OnEnable()
    {
        Application.logMessageReceived += LogCallback;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= LogCallback;
    }

    void LogCallback(string logString, string stackTrace, LogType type)
    {
        // if log is too big, remove first message
        if (console_log.Count == console_length) { console_log.RemoveAt(0); }
        console_log.Add(logString); // add last message to list
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
        values.text += name + ": " + value + "\r\n";

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
        values.text += name + ": " + value + "\r\n";

    }
}
