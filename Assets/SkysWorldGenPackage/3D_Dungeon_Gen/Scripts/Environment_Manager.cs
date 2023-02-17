using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Environment_Manager : MonoBehaviour
{
    [Header("==== Wall Blocks ====")]
    public bool spawnWalls = true;
    public List<GameObject> wallEdgeObjs;

    [Header("==== Ground Blocks ====")]
    public bool spawnGround = true;
    public List<GameObject> groundPrefabs;

    [Header("==== Ceiling Blocks =====")]
    public bool spawnCeilings;
    public List<GameObject> ceilingPrefabs;

    [Header("==== EXIT RAMP =====")]
    public GameObject exit_ramp;

    [Header("==== End Dungeon ====")]
    public GameObject endLevelObject;


    // CELL_TYPE { NONE, INACTIVE, CHECKED, EMPTY_FLOOR, WALL, EXIT, PATHWAY, SIDE, CEILING, AIR}
    [Space(20)]

    [Header("EMPTY FLOOR CELLS")]
    [Range(0, 1)]
    public float empty_floor_spawn_weight = 0.1f;
    public List<GameObject> empty_floor_prefabs;

    [Header("WALL CELLS")]
    [Range(0, 1)]
    public float wall_spawn_weight = 0.1f;
    public List<GameObject> wall_prefabs;

    [Header("PATHWAY CELLS")]
    [Range(0, 1)]
    public float pathway_spawn_weight = 0.1f;
    public List<GameObject> pathway_prefabs;

    [Header("SIDE CELLS")]
    [Range(0, 1)]
    public float side_spawn_weight = 0.1f;
    public List<GameObject> side_prefabs;

    [Header("CEILING CELLS")]
    [Range(0, 1)]
    public float ceiling_spawn_weight = 0.1f;
    public List<GameObject> ceiling_prefabs;

    [Header("AIR CELLS")]
    [Range(0, 1)]
    public float air_spawn_weight = 0.1f;
    public List<GameObject> air_prefabs;

}
