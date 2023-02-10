using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveMeshManager : MonoBehaviour
{
    public DungeonGenerationManager dunGenManager;

    public Transform playerHead;
    Vector3 sightHitPoint;

    [HideInInspector]
    public Vector3 activeMeshCenter;
    public float base_activeMeshRange = 100;
    public float environment_activeRange = 60;

    [Space(10)]
    public bool sightBased;
    public float sight_activeMeshRange = 30;

    public LayerMask externalEnvironment;
    // Update is called once per frame
    void Update()
    {
        if (sightBased)
        {
            RaycastHit hit;
            if (Physics.Raycast(playerHead.position, playerHead.forward, out hit, 150, externalEnvironment))
            {
                sightHitPoint = hit.point;
                sight_activeMeshRange = (int)hit.distance;
                Debug.DrawRay(playerHead.position, playerHead.forward * hit.distance, Color.yellow);



                //Debug.Log("Did Hit");
            }

        }

        activeMeshCenter = playerHead.position + (playerHead.forward * (base_activeMeshRange / 3));
        Debug.DrawRay(playerHead.position, activeMeshCenter, Color.yellow);

        // << ENABLE / DISABLE MESH >>
        foreach (TileGenerationManager t in dunGenManager.allTiles)
        {
            // if tile doesn't have mesh yet , continue to next
            if (t.tileGround == null)
                continue;

            bool active = true;
            // if tile is outside of distance, disable meshes
            if (t.centerCell != null &&
                Vector3.Distance(activeMeshCenter, t.centerCell.transform.position) > base_activeMeshRange) { active = false; }


            // enable/disable mesh
            Mesh_Enabled(t, active);
        }

        // << ENABLE / DISABLE AI >>
        foreach (TileGenerationManager t in dunGenManager.allTiles)
        {
            bool active = true;
            // if tile is outside of distance, disable ai
            if (t.centerCell != null &&
                Vector3.Distance(playerHead.position, t.centerCell.transform.position) > environment_activeRange) { active = false; }


            // enable/disable mesh
            AI_Enabled(t.allGroundAi, active);
        }

    }

    public void Mesh_Enabled(TileGenerationManager t, bool active)
    {            // set meshes to proper active state
        if (t.cellTransformParent.gameObject.activeSelf != active)
        {
            
            t.cellTransformParent.gameObject.SetActive(active);

            if (t.tileCeiling != null) { t.tileCeiling.SetActive(active); }
            t.tileGround.SetActive(active);
            t.wallParent.SetActive(active);
        }

    }

    public void AI_Enabled(List<GroundAI> groundAi, bool active)
    {
        foreach (GroundAI ai in groundAi)
        {
            if (active == false) { ai.curState = GroundAI.State.Idle; }


            ai.gameObject.SetActive(active);

        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(activeMeshCenter, base_activeMeshRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(sightHitPoint, sight_activeMeshRange);
    }
}
