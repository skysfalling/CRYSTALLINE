using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenManager : MonoBehaviour
{
    DungeonGenerationManager dunGenManager;
    Environment_Manager envManager;
    NPC_Manager npcManager;

    public TileGenerationManager startTile;

    [Header("Generation Settings")]
    public int individualTileSize = 10;

    [Range(0, 1)]
    public float obstacleSpawnWeight = 0.7f;

    [Range(0, 1)]
    public float roomSpawnWeight = 0.6f;

    [Range(0, 1)]
    public float npcSpawnWeight = 0.1f;

    public bool spawnCeilings = true;

    public void Start()
    {
        dunGenManager = GetComponentInChildren<DungeonGenerationManager>();
        envManager = GetComponentInChildren<Environment_Manager>();
        npcManager = GetComponentInChildren<NPC_Manager>();
    }

    private void Update()
    {

        // get start tile
        if (startTile == null && dunGenManager.dungeonStartTile != null)
        {
            startTile = dunGenManager.dungeonStartTile.GetComponent<TileGenerationManager>();
        }

    }

    public void GenerateWorld()
    {

        dunGenManager.individualTileSize = individualTileSize;
        dunGenManager.obstacleSpawnWeight = obstacleSpawnWeight;
        dunGenManager.roomSpawnWeight = roomSpawnWeight;

        envManager.spawnCeilings = spawnCeilings;

        npcManager.spawnWeight = npcSpawnWeight;


        StartCoroutine(dunGenManager.Generate(Vector3.zero));
    }
}
