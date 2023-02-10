using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClimbableObject : MonoBehaviour
{
    GameManager gameManager;
    FirstPersonMovement fpMovement;


    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
        fpMovement = gameManager.fp_player.GetComponent<FirstPersonMovement>();
    }



    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) { fpMovement.canClimb = true; }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) { fpMovement.canClimb = false; }
    }
}
