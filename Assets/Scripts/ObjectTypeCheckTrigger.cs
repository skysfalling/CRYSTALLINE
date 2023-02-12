using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectTypeCheckTrigger : MonoBehaviour
{
    public Vector3 overlapBoxSize = new Vector3(5, 1, 5);
    public LayerMask layerToCheck;
    public string checkTag = "Untagged";

    [Space(20)]
    public bool createNewMap;


    [Space(20)]
    public Collider[] overlapArea;

    public void Update()
    {
        // << OVERLAP AREA >>
        overlapArea = Physics.OverlapBox(transform.position, overlapBoxSize / 2, Quaternion.identity, layerToCheck);
        if (overlapArea.Length > 0)
        {
            // for each object in area ...
            foreach(Collider collider in overlapArea)
            {
                // if proper tag ... do given operations
                if (collider.CompareTag(checkTag))
                {

                    if (createNewMap)
                    {
                        GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().RestartScene();
                    }





                }
            }
        }

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, overlapBoxSize);
    }
}
