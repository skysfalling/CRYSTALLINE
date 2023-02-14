using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//                      NONE () : Has not been set.
//                      INACTIVE (-3) : Nothing can be spawned here.
//                      CHECKED (-2) : Checked cell in A* Pathfinding.
//                      EMPTY_FLOOR (-1) : Empty cell, all spawn friendly.
//                      WALL (0) : Wall is spawned here.
//                      EXIT (1) : Reserved for exit spawns, nothing else can be spawned here.
//                      PATHWAY (2) : Reserved for pathway spawns, nothing else can be spawned here.
//                      SIDE (3) : Reserved for side of wall spawns, nothing else can be spawned here.
//                      CEILING (4) : Reserved for under ceiling spawns, nothing else can be spawned here.
//                      AIR (5) : Reserved for midair spawns, nothing else can be spawned here.


public enum CELL_TYPE { NONE, INACTIVE, CHECKED, EMPTY_FLOOR, WALL, EXIT, PATHWAY, SIDE, CEILING, AIR}

public class Cell : MonoBehaviour
{
    public DungeonGenerationManager dunGenManager;
    public TileGenerationManager tileGenManager;
    NPC_Manager npcManager;
    Environment_Manager env;

    [Space(10)]
    public GameObject debugWall;
    public GameObject debugCube;
    
    [Space(10)]
    public Material debug_noneMat;
    public Material debug_inactiveMat;
    public Material debug_emptyMat;
    public Material debug_wallMat;
    public Material debug_exitMat;
    public Material debug_pathMat;
    public Material debug_sideMat;
    public Material debug_ceilingMat;
    public Material debug_airMat;

    [Space(10)]
    public Vector3 coord;
    public CELL_TYPE cellType = CELL_TYPE.NONE;
    public int cellSize;
    public GameObject cellModel;
    public float modelScale = 0.4f;

    [Header("============== Model Types ================")]
    public bool randomizeInnerCellPosition;
    public bool randomizeRotation;

    // ===== PATHFINDING ====
    [HideInInspector]
    public float pathWeight;

    [Header("Edges ==========================================")]
    public int exitHeightDifference;
    public bool roomEdgeOverride;

    [Space(10)]
    public bool isCorner;

    public bool isLeftEdge;
    public Material leftEdgeMaterial;

    public bool isRightEdge;
    public Material rightEdgeMaterial;

    public bool isTopEdge;
    public Material topEdgeMaterial;

    public bool isBottomEdge;
    public Material bottomEdgeMaterial;

    [Header("Neighbors =====================================")]
    public List<Cell> cellNeighbors = new List<Cell>(4);

    private void Start()
    {
        tileGenManager = GetComponentInParent<TileGenerationManager>();
        dunGenManager = tileGenManager.dunGenManager;

        npcManager = dunGenManager.npc_manager;
        env = dunGenManager.env_manager;

        cellSize = tileGenManager.cellSize;
    }

    // <<<< DEBUG >>>> ===================================================
    public void SetDebugCube(bool enabled = true)
    {
        // is not enabled , make sure to disable
        if (enabled == false)
        {
            debugCube.SetActive(false);
            return;
        }

        // if not empty or a wall...
        if (cellType != CELL_TYPE.NONE && cellType != CELL_TYPE.WALL)
        {
            // set active
            debugCube.SetActive(true);
            MeshRenderer meshR = debugCube.GetComponent<MeshRenderer>();

            switch (cellType)
            {
                case CELL_TYPE.NONE:
                    meshR.material = debug_noneMat;
                    break;
                case CELL_TYPE.INACTIVE:
                    meshR.material = debug_inactiveMat;
                    break;
                case CELL_TYPE.EMPTY_FLOOR:
                    meshR.material = debug_emptyMat;
                    break;
                case CELL_TYPE.WALL:
                    meshR.material = debug_wallMat;
                    break;
                case CELL_TYPE.EXIT:
                    meshR.material = debug_exitMat;
                    break;
                case CELL_TYPE.PATHWAY:
                    meshR.material = debug_pathMat;
                    break;
                case CELL_TYPE.SIDE:
                    meshR.material = debug_sideMat;
                    break;
                case CELL_TYPE.CEILING:
                    meshR.material = debug_ceilingMat;
                    break;
                case CELL_TYPE.AIR:
                    meshR.material = debug_airMat;
                    break;
            }
        }
        else
        {
            debugCube.SetActive(false);
        }

        debugCube.transform.localScale = Vector3.one * modelScale * 0.5f;
    }

    public void TempChangeDebugMat(Material mat)
    {
        MeshRenderer meshR = debugCube.GetComponent<MeshRenderer>();
        meshR.material = mat;
    }

    // <<<< CELL MODELS >>>> ================================================
    public void SpawnCellModels()
    {
        // << DEBUG >>
        // if debug
        if (tileGenManager.indivDebug && cellType == CELL_TYPE.WALL)
        {
            cellModel = Instantiate(debugWall, transform.position, Quaternion.identity);
            cellModel.transform.localScale = new Vector3(cellSize, cellSize, cellSize);
            cellModel.transform.localPosition += new Vector3(0, cellSize / 2, 0);
            cellModel.transform.parent = transform;
            return;
        }


        // change "checked tile" to pathway tile fuckit
        if (cellType == CELL_TYPE.CHECKED) { cellType = CELL_TYPE.PATHWAY; }

        // create model and place as child of parent
        switch (cellType)
        {
            // inactive cell
            case CELL_TYPE.INACTIVE:
                break;
            // empty cell
            case CELL_TYPE.EMPTY_FLOOR:

                // randomly decide to spawn obj 
                if (Random.Range((float)0, (float)1) < env.emptySpaceSpawnWeight)
                {
                    // choose random model from list
                    GameObject randomEmptyCellObj = env.emptySpaceObjs[Random.Range(0, env.emptySpaceObjs.Count)];

                    cellModel = Instantiate(randomEmptyCellObj, randomEmptyCellObj.transform.localPosition + transform.position, Quaternion.identity);
                    cellModel.transform.localScale *= cellSize * modelScale;
                    cellModel.transform.parent = this.transform;
                }

                // randomly decide to spawn npcs
                if (Random.Range((float)0, (float)1) < npcManager.spawnWeight)
                {
                    // choose random model from list
                    GameObject npc_obj = npcManager.SpawnNPC(npcManager.prefabs[Random.Range(0, npcManager.prefabs.Count)], GetComponent<Cell>());
                    cellModel.transform.localScale *= cellSize * modelScale;
                    npc_obj.transform.parent = this.transform;

                }

                break;

            // case 0 == WALL
            case CELL_TYPE.WALL:
                if (tileGenManager.roomTile)
                {
                    SpawnRoomObstacles(tileGenManager.wallHeight); // focus on room walls first before spawning everything else
                }
                else
                {
                    SpawnObstacles(tileGenManager.wallHeight);
                }

                // add to source mesh filters to be combined
                //tileGenManager.sourceMeshFilters.Add(cellModel.GetComponent<MeshFilter>());

                break;

            // case 1 == exit
            case CELL_TYPE.EXIT:

                exitHeightDifference = ExitHeightDifference();
                SpawnExitModels(cellSize);

                break;

            // case 2 == pathway
            case CELL_TYPE.PATHWAY:
                // randomly decide to spawn obj 
                if (env.pathObjs.Count > 0 && Random.Range((float)0, (float)1) < env.pathSpawnWeight)
                {
                    // choose random model from list
                    GameObject randomPathObj = env.pathObjs[Random.Range(0, env.pathObjs.Count)];

                    cellModel = Instantiate(randomPathObj, randomPathObj.transform.localPosition + transform.position, Quaternion.identity);
                    cellModel.transform.localScale *= cellSize * modelScale;
                    cellModel.transform.parent = this.transform;

                }
                break;

            // case 2 == pathway
            case CELL_TYPE.CEILING:
                // randomly decide to spawn obj 
                if (env.ceilingObjs.Count > 0 && Random.Range((float)0, (float)1) < env.ceilingSpawnWeight)
                {
                    // choose random model from list
                    GameObject randomCeilingObj = env.ceilingObjs[Random.Range(0, env.ceilingObjs.Count)];

                    cellModel = Instantiate(randomCeilingObj, transform.position, Quaternion.identity);
                    cellModel.transform.localScale *= cellSize * modelScale;
                    cellModel.transform.parent = this.transform;

                }
                break;
        }

        // << RANDOMIZE MODEL POSITIONS >>
        if (cellModel != null)
        {
            // randomize inner cell pos
            if (randomizeInnerCellPosition)
            {
                cellModel.transform.localPosition += new Vector3(Random.Range((float)-cellSize / 3, (float)cellSize / 3), 0,
                                                            Random.Range((float)-cellSize / 3, (float)cellSize / 3));
            }

            // randomize rotation of obj
            if (randomizeRotation)
            {
                cellModel.transform.rotation = Quaternion.Euler(new Vector3(0, Random.Range((float)0, (float)180), 0));
            }
        }

    }

    public void SetCellNeighbors()
    {
        cellNeighbors.Clear();

        cellNeighbors.Add(GetCellNeighbor(Vector3.right));
        cellNeighbors.Add(GetCellNeighbor(Vector3.left));
        cellNeighbors.Add(GetCellNeighbor(Vector3.up));
        cellNeighbors.Add(GetCellNeighbor(Vector3.down));
        cellNeighbors.Add(GetCellNeighbor(Vector3.forward));
        cellNeighbors.Add(GetCellNeighbor(Vector3.back));


    }

    public Cell GetCellNeighbor(Vector3 direction)
    {
        return tileGenManager.GetCell(coord + direction);
    }

    // ================================================= WALL MODELS ============================================================
    public void SpawnObstacles(int wallHeight)
    {


        // ** NOTE : WALL EDGES ARE IF YOU WOULD LIKE TO GO BACK TO USING "CELLED" WALLS
        // currently the walls are one big block instead of many small ones

        /*
        // <<<< WALL EDGES >>>>
        // if is edge or corner, use different model
        if (!roomEdgeOverride && env.wallEdgeObjs.Count > 0 && (isLeftEdge || isRightEdge || isTopEdge || isBottomEdge || isCorner))
        {
            // << SPAWN WALL AT HEIGHT >>
            for (int heightLevel = 0; heightLevel < wallHeight; heightLevel++)
            {
                GameObject randomWallEdgeObj = env.wallEdgeObjs[Random.Range(0, env.wallEdgeObjs.Count)];

                cellModel = Instantiate(randomWallEdgeObj, transform.position, Quaternion.identity);
                cellModel.transform.localScale = new Vector3(cellSize / 2, cellSize / 2, cellSize / 2); // adjust scale to cell size
                cellModel.transform.localPosition += new Vector3(0, (heightLevel * cellSize) + (cellSize / 2), 0); // vertical offset
                cellModel.transform.parent = transform;


                // edge wall cells shouldn't be randomized
                randomizeRotation = false;
                randomizeInnerCellPosition = false;

                // << OFFSET ROTATION >>
                float rotationOffset = 0;
                if (isLeftEdge) { rotationOffset = 180; }
                else if (isRightEdge) { rotationOffset = 0; }
                else if (isTopEdge) { rotationOffset = 270; }
                else if (isBottomEdge) { rotationOffset = 90; }

                cellModel.transform.rotation = Quaternion.Euler(new Vector3(0, rotationOffset, 0));
            }
        }
        */


        // <<<< MID ROOM OBSTACLES >>>>
        //else
        //{

        if ((!isLeftEdge && !isRightEdge && !isTopEdge && !isBottomEdge && !isCorner) || roomEdgeOverride) {
            // if not a room edge
            if (!roomEdgeOverride)
            {
                // check how many neighbor cells are available
                int emptyNeighborCellsCount = EmptyNeighborCells();

                // choose model list from neighbor cell count

                // **** 5 CELL ****
                if (emptyNeighborCellsCount >= 3)
                {

                    // change neighbor types to obstacle
                    //foreach (Cell cell in cellNeighbors) { cell.cellType = 0; }

                    // randomly decide to spawn obj
                    if (Random.Range((float)0, (float)1) < env.obstacle_5cell_spawnWeight)
                    {
                        // choose random model from list
                        GameObject obstacleObj = env.obstacleObjs_5cell[Random.Range(0, env.obstacleObjs_5cell.Count)];

                        cellModel = Instantiate(obstacleObj, transform.position, Quaternion.identity);
                        cellModel.transform.localScale = new Vector3(cellSize, cellSize, cellSize);
                        cellModel.transform.parent = transform;

                        return;
                    }
                }
            }



            // << DEFAULT SPAWN >>
            // **** 1 CELL ****
            // randomly decide to spawn obj
            float random = Random.Range((float)0, (float)1);

            //Debug.Log(random + "//" + env.obstacle_1cell_spawnWeight);

            if (random <= env.obstacle_1cell_spawnWeight)
            {
                // choose random model from list
                GameObject obstacleObj = env.obstacleObjs_1cell[Random.Range(0, env.obstacleObjs_1cell.Count)];

                cellModel = Instantiate(obstacleObj, transform.position, Quaternion.identity);
                cellModel.transform.localScale = new Vector3(cellSize, cellSize, cellSize);
                cellModel.transform.parent = transform;
            }


            // obstacles should be randomized
            randomizeRotation = true;
            randomizeInnerCellPosition = true;
        }
        //}

    }

    public void SpawnRoomObstacles(int wallHeight)
    {
        // check neighbors && DONT spawn walls between room neighbors
        List<bool> edgeType = new List<bool>() { isLeftEdge, isRightEdge, isTopEdge, isBottomEdge };

        // iterate through neighbors
        int cornerRoomNeighborCount = 0;
        for (int i = 0; i < 4; i++)
        {
            TileGenerationManager neighbor = tileGenManager.tileNeighbors[i];

            // if corner is between two room tiles, dont spawn
            if (isCorner)
            {
                if (neighbor != null && neighbor.roomTile && edgeType[i] == true) { cornerRoomNeighborCount++; }
                if (cornerRoomNeighborCount >= 2) { return; }
            }
            else
            {
                // if neighbor is valid && is roomTile && proper edge THEN DONT spawn edge wall
                if (neighbor != null && neighbor.roomTile && edgeType[i] == true) {

                    roomEdgeOverride = true; // dont spawn walls on room edges

                    // check if neighbor tile is higher than current tile
                    if (neighbor.tileHeightLevel > tileGenManager.tileHeightLevel)
                    {
                        int heightDiff = neighbor.tileHeightLevel - tileGenManager.tileHeightLevel;
                        SpawnWallClimbObjs(cellSize, heightDiff, env.wallClimbSpawnWeight);
                    }
                }
            }
        }

        SpawnObstacles(wallHeight);
    }

    public void SpawnWallClimbObjs(float gridCellSize, int wallHeight, float weight)
    {
        // << WALL ACCESSORIES >>
        if (!isCorner && env.wallClimbObjs.Count > 0 && Random.Range((float)0, (float)1) < weight)
        {
            // choose random model from list
            GameObject wallClimbObj = env.wallClimbObjs[Random.Range(0, env.wallClimbObjs.Count)];

            // spawn model at different heights
            for (int heightLevel = 0; heightLevel < wallHeight; heightLevel++)
            {
                // << OFFSET POSITION >> (move closer to wall)
                Vector3 offset = Vector3.zero;
                if (isLeftEdge) { offset = new Vector3(-gridCellSize / 2, gridCellSize / 2, 0); }
                else if (isRightEdge) { offset = new Vector3(gridCellSize / 2, gridCellSize / 2, 0); }
                else if (isTopEdge) { offset = new Vector3(0, gridCellSize / 2, gridCellSize / 2); }
                else if (isBottomEdge) { offset = new Vector3(0, gridCellSize / 2, -gridCellSize / 2); }

                offset += new Vector3(0, heightLevel * gridCellSize, 0); // adjust height level each time


                // << OFFSET ROTATION >>
                float rotationOffset = 0;
                if (isLeftEdge) { rotationOffset = 90; }
                else if (isRightEdge) { rotationOffset = 270; }
                else if (isTopEdge) { rotationOffset = 180; }
                else if (isBottomEdge) { rotationOffset = 0; }



                GameObject climbObj = Instantiate(wallClimbObj, transform.position, Quaternion.identity);
                climbObj.transform.localPosition += offset;
                climbObj.transform.rotation *= Quaternion.Euler(new Vector3(0, rotationOffset, 0));
                climbObj.transform.localScale = new Vector3(gridCellSize * modelScale, gridCellSize * modelScale, gridCellSize * modelScale);
                climbObj.transform.parent = transform;
            }
        }
    }

    public int EmptyNeighborCells()
    {
        int emptyCellsCount = 0;

        foreach (Cell cell in cellNeighbors)
        {
            if (cell == null) { continue; }

            if (cell.cellType == CELL_TYPE.EMPTY_FLOOR)
            {
                emptyCellsCount++;
            }
        }

        return emptyCellsCount;
    }

    // ================================================= EXIT MODELS ============================================================
    public void SpawnExitModels(float gridCellSize)
    {

        // << SPAWN EXIT CEILINGS >>
        if (dunGenManager.env_manager.spawnCeilings && !tileGenManager.roomTile)
        {
            // position ceiling at the height of the wall so that it's surrounded by walls instead of on top 
            GameObject ceilingBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            transform.parent = transform;

            ceilingBlock.transform.position = transform.position + new Vector3(0, (tileGenManager.wallHeight * cellSize) - (cellSize), 0); //height of cell, plus offset
            ceilingBlock.transform.localScale = new Vector3(cellSize, cellSize, cellSize);

            // add to sorce mesh filters to combine later
            tileGenManager.sourceMeshFilters.Add(ceilingBlock.GetComponentInChildren<MeshFilter>());
        }




        // << RAISE/LOWER HEIGHT >>
        // if going up, spawn up ramp
        if (ExitHeightDifference() == 1)
        {
            // << SPAWN RAMP >>
            GameObject ramp = Instantiate(env.exitRamps[0], transform.position, Quaternion.identity);
            ramp.transform.localScale = new Vector3(gridCellSize, gridCellSize, gridCellSize); // adjust scale to cell size
            ramp.transform.localPosition += new Vector3(0, gridCellSize / 2, 0); // vertical offset
            ramp.transform.parent = tileGenManager.tileGround.transform;

            // << OFFSET ROTATION >>
            float rotationOffset = 0;
            if (isLeftEdge) { rotationOffset = 180; }
            else if (isRightEdge) { rotationOffset = 0; }
            else if (isTopEdge) { rotationOffset = 270; }
            else if (isBottomEdge) { rotationOffset = 90; }

            ramp.transform.rotation = Quaternion.Euler(new Vector3(0, rotationOffset, 0));

            // exit cells shouldn't be randomized
            randomizeRotation = false;
            randomizeInnerCellPosition = false;
        }
        else if (ExitHeightDifference() > 1)
        {
            SpawnWallClimbObjs(gridCellSize, ExitHeightDifference(), 1); // always spawn wall climb at exits
        }


    }

    public int ExitHeightDifference()
    {
        //haha spaghetti code ... DEAL WITH IT
        
        if (isLeftEdge)
        {
            // if neighbor valid and is higher than current tile ..
            if (tileGenManager.tileNeighbors[0] != null)
            {
                return tileGenManager.tileNeighbors[0].tileHeightLevel - tileGenManager.tileHeightLevel;
            }
        }
        else if (isRightEdge)
        {
            // if neighbor valid and is higher than current tile ..
            if (tileGenManager.tileNeighbors[1] != null)
            {
                return tileGenManager.tileNeighbors[1].tileHeightLevel - tileGenManager.tileHeightLevel;
            }
        }
        else if (isTopEdge)
        {
            // if neighbor valid and is higher than current tile ..
            if (tileGenManager.tileNeighbors[2] != null)
            {
                return tileGenManager.tileNeighbors[2].tileHeightLevel - tileGenManager.tileHeightLevel;
            }
        }
        else if (isBottomEdge)
        {
            // if neighbor valid and is higher than current tile ..
            if (tileGenManager.tileNeighbors[3] != null)
            {
                return tileGenManager.tileNeighbors[3].tileHeightLevel - tileGenManager.tileHeightLevel;
            }
        }

        return 0;
    }
}