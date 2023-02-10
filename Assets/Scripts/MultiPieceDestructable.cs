using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MultiPieceDestructable : MonoBehaviour
{

    public bool destroyed;

    [Range(0, 1)]
    public float overlapRange;
    public Vector3 overlapPosOffset;
    public LayerMask collisionLayer;
    public List<GameObject> pieces = new List<GameObject>();
    public bool piecesAreXRInteractable;
    public Collider[] overlapColliders;

    public GameObject lightObj;

    private void Awake()
    {
        // freeze all pieces
        foreach (GameObject piece in pieces)
        {
            piece.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            if (piecesAreXRInteractable) { piece.GetComponent<XRGrabInteractable>().enabled = false; }
        }
    }

    private void Update()
    {
        if (!destroyed)
        {
            // << CHECK FOR COLLISION >>
            overlapColliders = Physics.OverlapSphere(transform.position + overlapPosOffset, overlapRange, collisionLayer);

            foreach (Collider col in overlapColliders)
            {
                // check if object not part of piece list
                if (!pieces.Contains(col.gameObject))
                {
                    Destruct();
                    //Debug.Log(col.gameObject.name + " destroyed crystal");

                }
            }
        }


    }

    public void Destruct()
    {
        // UNfreeze all pieces
        foreach (GameObject piece in pieces)
        {
            piece.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
            if (piecesAreXRInteractable) { piece.GetComponent<XRGrabInteractable>().enabled = true; }

            //piece.AddForce(Vector3.up * 0.5f, ForceMode.Impulse);
        }

        Destroy(lightObj);

        destroyed = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + overlapPosOffset, overlapRange);
    }
}
