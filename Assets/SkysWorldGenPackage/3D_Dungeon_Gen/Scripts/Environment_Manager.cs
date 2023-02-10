using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Environment_Manager : MonoBehaviour
{

    [Header("==== Wall Objs ====")]
    public List<GameObject> wallEdgeObjs;

    [Header("==== Ground / Ceiling ====")]
    public List<GameObject> groundPrefabs;

    [Space(5)]
    public bool spawnCeilings;
    public List<GameObject> ceilingPrefabs;
    
    [Header("==== Obstacles ====")]
    [Range(0, 1)]
    public float obstacle_1cell_spawnWeight = 0.8f;
    public List<GameObject> obstacleObjs_1cell; // if one cell is available
    [Space(10)]
    [Range(0, 1)]
    public float obstacle_5cell_spawnWeight = 0.4f;
    public List<GameObject> obstacleObjs_5cell; // if one cell and all of its neighbors are available


    [Space(10)]
    [Range(0, 1)]
    public float wallClimbSpawnWeight = 0.2f;
    public List<GameObject> wallClimbObjs;

    [Header("==== Pathway ====")]
    [Range(0, 1)]
    public float pathSpawnWeight = 0.3f;
    public List<GameObject> pathObjs;

    [Header("==== Exits ====")]
    public List<GameObject> exitRamps;
    
    [Space(10)]
    [Range(0, 1)]
    public float exitSpawnWeight = 1f;
    public List<GameObject> exitObjs;

    [Header("==== Empty Space ====")]
    [Range(0, 1)]
    public float emptySpaceSpawnWeight = 0.5f;
    public List<GameObject> emptySpaceObjs;

    [Header("==== End Dungeon ====")]
    public GameObject endTileCenterSpawnObject;

}
