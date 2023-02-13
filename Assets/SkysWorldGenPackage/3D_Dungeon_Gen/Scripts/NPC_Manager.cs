using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC_Manager : MonoBehaviour
{
    DungeonGenerationManager dunGenManager;

    [Range(0, 1)]
    public float spawnWeight;
    public List<GameObject> prefabs = new List<GameObject>();

    private void Awake()
    {
        dunGenManager = GetComponent<DungeonGenerationManager>();
    }

    public GameObject SpawnNPC(GameObject prefab, Cell cell)
    {
        Debug.Log("Spawn NPC");

        // choose random model from list
        GameObject npc = Instantiate(prefab, cell.transform.position, Quaternion.identity);
        //npc.transform.localScale = new Vector3(cell.cellSize * modelScale, gridCellSize * modelScale, gridCellSize * modelScale);
        npc.transform.parent = dunGenManager.transform;
        npc.GetComponent<GroundAI>().curTile = cell.tileGenManager;
        cell.tileGenManager.allGroundAi.Add(npc.GetComponent<GroundAI>()); // add to tile manager's list

        return npc;
    }


}
