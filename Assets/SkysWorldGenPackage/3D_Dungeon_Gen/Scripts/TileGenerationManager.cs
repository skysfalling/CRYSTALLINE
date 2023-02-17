using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TileGenerationManager : MonoBehaviour
{
    [Header("Debug")]
    public bool indivDebug;
    public float gizmosSphereSize = 1;

    [Header("Tile Size =============")]
    public Vector3 tilePosition;
    public int fullTileSize = 50;
    public int cellsPerTile = 10;
    public int cellSize; // fullTileSize / tileLengthCellCount


    [Header("Tile Data =============")]
    public Vector2 coord = new Vector2();
    public List<TileGenerationManager> tileNeighbors = new List<TileGenerationManager>(4); // tile neighbors (left, right, top, bottom)
    public List<TileGenerationManager> pathNeighbors = new List<TileGenerationManager>();
    [Space(10)]
    [HideInInspector]
    public DungeonGenerationManager dunGenManager;
    [HideInInspector]
    public Environment_Manager env_manager;
    public bool dungeonBeginning;
    public bool dungeonEnd;

    public bool pathBeginning;
    public bool inBranch;
    [Space(10)]
    public TileGenerationManager prevPathTile;
    public TileGenerationManager nextPathTile;
    public TileGenerationManager branchNode;

    [Header("Tile Spawns ===========")]
    public Cell centerCell;
    public Transform playerSpawn;
    public GameObject playerObject;
    [HideInInspector]
    public GameObject tileGround;
    [HideInInspector]
    public GameObject tileCeiling;
    [HideInInspector]
    public GameObject wallParent;


    [Header("Cell Data =============")]
    public GameObject cellPrefab;
    public Transform cellTransformParent;
    public int cellCoordMax;
    public List<Cell> allCells = new List<Cell>();
    public List<Cell> emptyFloorCells = new List<Cell>();

    [Header("Pathfinding")]
    public RandomPathFinder pathFinderScript;
    public List<Cell> pathA = new List<Cell>();
    public List<Cell> pathB = new List<Cell>();
    public bool debugPathAFound;
    public bool debugPathBFound;

    [Header("Generation Values")]
    [Range(0f, 1f)]
    public bool tileInitialized;

    [Header("Generate Room")]
    public bool roomTile;
    public List<TileGenerationManager> roomParts = new List<TileGenerationManager>();

    [Header("Height Data")]
    public bool transitionTile;
    public int transitionDirection = 0; // if transition tile, this gets updated to 1 or -1
    public int tileHeightLevel = 0;
    [Space(20)]
    public int wallHeight;

    [Header("Tile Edges")]
    public List<Cell> cornerCells = new List<Cell>();
    Cell topLeftCorner;
    Cell topRightCorner;
    Cell botLeftCorner;
    Cell botRightCorner;


    public List<Cell> leftEdgeCells = new List<Cell>();
    public List<Cell> rightEdgeCells = new List<Cell>();
    public List<Cell> topEdgeCells = new List<Cell>();
    public List<Cell> bottomEdgeCells = new List<Cell>();

    [Header("Tile Walls")]
    public List<GameObject> walls = new List<GameObject>();
    public bool leftWallDisabled;
    public bool rightWallDisabled;
    public bool topWallDisabled;
    public bool bottomWallDisabled;



    [Header("Tile Mesh Combination")]
    public List<MeshFilter> sourceMeshFilters = new List<MeshFilter>();
    public GameObject generatedMeshParent;

    [Header("Necessary Exit Points")]
    public bool needLeftExit;
    public int leftExitIndex = -1;
    public bool needRightExit;
    public int rightExitIndex = -1;
    public bool needTopExit;
    public int topExitIndex = -1;
    public bool needBottomExit;
    public int bottomExitIndex = -1;
    public List<Cell> exitCells = new List<Cell>();

    [Header("Current AI")]
    public List<GroundAI> allGroundAi = new List<GroundAI>();

    public void Start()
    {
        //if (indivDebug) { TileInit(true); }
    }

    public void Update()
    {
        tilePosition = transform.position;

        
        // << DISPLAY CELL CENTER POINTS >>
        foreach (Cell cell in allCells)
        {
            if (dunGenManager.debug)
            {
                cell.SetDebugCube(true);
            }
            else
            {
                cell.SetDebugCube(indivDebug);
            }
        }
        

        //DebugVisualization();
    }

    // Start is called before the first frame update
    public void TileInit()
    {
        pathFinderScript = GetComponent<RandomPathFinder>();
        dunGenManager = GetComponentInParent<DungeonGenerationManager>();
        env_manager = dunGenManager.env_manager;

        // set individual cell size
        cellSize = (fullTileSize / cellsPerTile);

        // makes sure cell size is never less than 1
        if (cellSize < 1)
        {
            Debug.LogWarning("Changed TileLengthCellCount from " + cellsPerTile + " to " + fullTileSize + " to keep cell size at minimum 1");

            // reset ratio of cellCount / tileSize to 1
            cellsPerTile = fullTileSize;
            dunGenManager.cellsPerTile = fullTileSize;

            // set individual cell size
            cellSize = (fullTileSize / cellsPerTile);
        }

        // set max value of coords
        cellCoordMax = (fullTileSize / cellSize) - 1;

        // set tile height
        tilePosition += new Vector3(0, tileHeightLevel * cellSize, 0);

        // depending on what the tallest tile is , the "total wall height" is tallest tile height + min wall height
        wallHeight = (dunGenManager.cur_tallestTileHeight + dunGenManager.minWallHeight) - tileHeightLevel; // offset so that all walls meet at the same height

        // set center cell of tile
        centerCell = GetCell(new Vector3(cellCoordMax / 2, 0, cellCoordMax / 2));

        // << CREATE BASE GROUND >>
        // tile ground is scaled for inner part of tile, edge grounds are spawned individually
        GameObject tileGroundPrefab = dunGenManager.env_manager.groundPrefabs[Random.Range(0, dunGenManager.env_manager.groundPrefabs.Count)];
        tileGround = Instantiate(tileGroundPrefab, transform);

        
        int fullDungeonHeight = (dunGenManager.maxTileHeight - dunGenManager.minTileHeight) * cellSize; // tile levels * cell size

        tileGround.transform.position = tilePosition + new Vector3(0, -fullDungeonHeight * 0.5f, 0);
        tileGround.transform.localScale = new Vector3(fullTileSize, fullDungeonHeight, fullTileSize);

        tileGround.name = "TileGround";

        // << CREATE WALL PARENT >>
        wallParent = new GameObject("Wall Parent");
        wallParent.transform.parent = transform;

        // << CREATE CEILING >>
        if (dunGenManager.env_manager.spawnCeilings)
        {
            GameObject tileCeilingPrefab = dunGenManager.env_manager.ceilingPrefabs[Random.Range(0, dunGenManager.env_manager.ceilingPrefabs.Count)];
            tileCeiling = Instantiate(tileCeilingPrefab, transform);

            tileCeiling.transform.position = tilePosition + new Vector3(0, (wallHeight * cellSize) + (cellSize * 0.25f), 0); //height of cell, plus offset


            if (roomTile)
            {
                tileCeiling.transform.localScale = new Vector3(fullTileSize, cellSize, fullTileSize);
            }
            else
            {
                tileCeiling.transform.localScale = new Vector3(fullTileSize, cellSize, fullTileSize);
            }

            tileCeiling.name = "TileCeiling";
            tileCeiling.transform.parent = transform;

            // add to sorce mesh filters to combine later
            sourceMeshFilters.Add(tileCeiling.GetComponentInChildren<MeshFilter>());
        }

        //Debug.Log("Grid Size: " + fullTileSize + " Cell Size: " + cellSize);

        StartGeneration();
    }


    // ========================================== GENERATION ==============================================

    // <<<< MAIN GENERATE FUNCTION >>>>
    public void StartGeneration()
    {
        DestroyAllCellParents();
        pathFinderScript.Reset();


        StartCoroutine(Generate());

    }

    // <<<< GENERATION >>>>
    public IEnumerator Generate()
    {
        yield return new WaitUntil(() => CreateCellParents());

        // set cell neighbors
        foreach (Cell cell in allCells) { cell.SetCellNeighbors(); }

        // determine which cells are edges of the grid
        DetermineEdges();

        /*
        // find paths from exits
        StartCoroutine(FindPaths());

        yield return new WaitUntil(() => debugPathAFound);
        yield return new WaitUntil(() => debugPathBFound);
        */

        tileInitialized = true;
        yield return new WaitUntil(() => dunGenManager.generationFinished);



        // create entrance / exit pairs
        CreateExits();

        // expand exit doorways without messing with pathfinding
        ExpandAllExits();


        /*
        // turn on / off walls based on rooms
        SetRoomWalls();

        // determine which cells are the sides of the grid
        DetermineSideCells();

        // determine which cells are ceiling cells
        DetermineCeilingCells();

        // create walls
        // CreateOptimizedWalls();


        // spawn models for each cell
        SpawnAllCellModels();

        // combine all cell meshes 
        CombineMeshes();

        // set all spawn points
        SetSpawns();
        */


    }

    // <<<< DESTROY ALL CELL PARENTS >>>>
    // used for reset & regeneration
    public void DestroyAllCellParents()
    {
        // destroy all spawned gameobjects
        foreach (Cell cell in allCells)
        {
            if (cell != null)
            {
                Destroy(cell.gameObject);
            }
        }

        // clear all lists in preperation for new gen
        allCells.Clear();
        leftEdgeCells.Clear();
        rightEdgeCells.Clear();
        topEdgeCells.Clear();
        bottomEdgeCells.Clear();
        emptyFloorCells.Clear();

        exitCells.Clear();
    }

    // <<<< FIND CENTER POS OF EACH CELL AND CREATE PARENT >>>>
    public bool CreateCellParents()
    {
        // << FIND CENTERS OF CELLS >>
        Vector3 botLeftCellCenter = tilePosition + new Vector3((float)-fullTileSize / 2, 0, (float)-fullTileSize / 2) + new Vector3((float)cellSize / 2, 0, (float)cellSize / 2);
        //Debug.Log("Top Left Cell Center: " + topLeftCellCenter);
        for (float x = 0; x < fullTileSize / cellSize; x += 1)
        {
            for (float y = 0; y < wallHeight + 1; y += 1)
            {
                for (float z = 0; z < fullTileSize / cellSize; z += 1)
                {
                    // get position of cell center
                    Vector3 cellCenterPos = botLeftCellCenter + new Vector3(x * cellSize, y * cellSize, z * cellSize);

                    // create cell , child of cell transform parent
                    GameObject cell = Instantiate(cellPrefab, cellCenterPos, Quaternion.identity);
                    cell.transform.parent = cellTransformParent.transform;

                    // get cellParent script
                    Cell cellParentScript = cell.GetComponent<Cell>();
                    allCells.Add(cellParentScript); // add to all cells
                    cellParentScript.coord = new Vector3(x, y, z); // set coord
                    cell.name = "CellParent" + cellParentScript.coord.ToString(); // name cell 
                    cellParentScript.tileGenManager = gameObject.GetComponent<TileGenerationManager>();
                    cellParentScript.dunGenManager = cellParentScript.tileGenManager.dunGenManager;
                }
            }
        }

        // set center cell of tile
        centerCell = GetCell(new Vector3(cellCoordMax / 2, 0, cellCoordMax / 2));

        return true;

    }

    // <<<< DETERMINE WHICH CELLS ARE EDGES >>>>
    public void DetermineEdges()
    {
        foreach (Cell cell in allCells)
        {
            if(cell.coord.y > 0) { continue; }

            // if corner
            if (cell.coord == Vector3.zero || cell.coord == new Vector3(0, 0, cellCoordMax) || cell.coord == new Vector3(cellCoordMax, 0, 0) || cell.coord == new Vector3(cellCoordMax, 0, cellCoordMax))
            {
                cell.isCorner = true;

                // set corners
                if (cell.coord == Vector3.zero) { botLeftCorner = cell; }
                else if (cell.coord == new Vector3(0, 0, cellCoordMax)) { topLeftCorner = cell; }
                else if (cell.coord == new Vector3(cellCoordMax, 0, 0)) { botRightCorner = cell; }
                else if (cell.coord == new Vector3(cellCoordMax, 0, cellCoordMax)) { topRightCorner = cell; }

                cornerCells.Add(cell);
            }

            // get left edge
            if (cell.coord.x == 0) { cell.isLeftEdge = true; cell.cellType = CELL_TYPE.WALL; }
            // get right edge
            else if (cell.coord.x == cellCoordMax) { cell.isRightEdge = true; cell.cellType = CELL_TYPE.WALL; }
            // get top edge
            else if (cell.coord.z == cellCoordMax) { cell.isTopEdge = true; cell.cellType = CELL_TYPE.WALL; }
            // get bottom edge
            else if (cell.coord.z == 0) { cell.isBottomEdge = true; cell.cellType = CELL_TYPE.WALL; }
            // else empty cell
            else
            {
                cell.cellType = CELL_TYPE.EMPTY_FLOOR;
                emptyFloorCells.Add(cell);
                continue;
            }

            // now add non-corners and ground cells to edge lists
            if (!cell.isCorner && cell.coord.y == 0)
            {
                if (cell.isLeftEdge) { leftEdgeCells.Add(cell); }
                else if (cell.isRightEdge) { rightEdgeCells.Add(cell); }
                else if (cell.isTopEdge) { topEdgeCells.Add(cell); }
                else if (cell.isBottomEdge) { bottomEdgeCells.Add(cell); }
            }
            
        }
    }


    #region EXITS =======================================================================================================

    // << SET NECESSARY EXITS BASED ON PATH NEIGHBORS >>
    public void SetNecessaryExitPoints()
    {

        // << SET PATH NEIGHBORS >>
        // if prev path tile not null and not in path neighbors list, add to list
        if (prevPathTile != null && !pathNeighbors.Contains(prevPathTile)) { pathNeighbors.Add(prevPathTile); }
        // if next path tile not null and not in path neighbors list, add to list
        if (nextPathTile != null && !pathNeighbors.Contains(nextPathTile)) { pathNeighbors.Add(nextPathTile); }
        // if next path tile not null and not in path neighbors list, add to list
        if (branchNode != null && !pathNeighbors.Contains(branchNode)) { pathNeighbors.Add(branchNode); }

        // << GET NECCESSARY EXITS >>
        TileGenerationManager leftNeighbor = GetTileNeighbor(Vector2.left);
        TileGenerationManager rightNeighbor = GetTileNeighbor(Vector2.right);
        TileGenerationManager topNeighbor = GetTileNeighbor(Vector2.up);
        TileGenerationManager bottomNeighbor = GetTileNeighbor(Vector2.down);

        // if neighbor not null AND neighbor is in path neighbors, need exit 
        foreach (TileGenerationManager tile in pathNeighbors)
        {
            if (leftNeighbor != null && pathNeighbors.Contains(leftNeighbor)) { needLeftExit = true;}
            else { needLeftExit = false; }
            if (rightNeighbor != null && pathNeighbors.Contains(rightNeighbor)) { needRightExit = true; }
            else { needRightExit = false; }
            if (topNeighbor != null && pathNeighbors.Contains(topNeighbor)) { needTopExit = true; }
            else { needTopExit = false; }
            if (bottomNeighbor != null && pathNeighbors.Contains(bottomNeighbor)) { needBottomExit = true;}
            else { needBottomExit = false; }
        }


    }

    // <<<< CHOOSE EXITS & ENTRANCES FROM EDGE CELLS >>>>
    public void CreateExits()
    {
        if (needLeftExit) { SetRandomExitCell(Vector2.left, leftEdgeCells); }
        if (needRightExit) { SetRandomExitCell(Vector2.right, rightEdgeCells); }
        if (needTopExit) { SetRandomExitCell(Vector2.up, topEdgeCells); }
        if (needBottomExit) { SetRandomExitCell(Vector2.down, bottomEdgeCells); }
    }

    public void SetRandomExitCell(Vector2 direction, List<Cell> edgeCells)
    {
        TileGenerationManager neighbor = GetTileNeighbor(direction);
        int neighborPairExitIndex = -1;
        int thisExitindex = -1;

        // if neighbor is null , can't place exit
        if (!neighbor)
        {
            // reset exit needs based on available tiles
            if (direction == Vector2.left) { needLeftExit = false; }
            if (direction == Vector2.right) { needRightExit = false; }
            if (direction == Vector2.up) { needTopExit = false; }
            if (direction == Vector2.down) { needBottomExit = false; }

            return;
        }

        // setup exit indexes variables
        if (direction == Vector2.left)
        {
            neighborPairExitIndex = neighbor.rightExitIndex;
            thisExitindex = leftExitIndex;
        }
        else if (direction == Vector2.right)
        {
            neighborPairExitIndex = neighbor.leftExitIndex;
            thisExitindex = rightExitIndex;
        }
        else if (direction == Vector2.up)
        {
            neighborPairExitIndex = neighbor.bottomExitIndex;
            thisExitindex = topExitIndex;
        }
        else if (direction == Vector2.down)
        {
            neighborPairExitIndex = neighbor.topExitIndex;
            thisExitindex = bottomExitIndex;
        }
        else { return; /* not a valid direction */ }


        // check if neighbor has chosen opposite exit already
        if (neighborPairExitIndex != -1) { thisExitindex = neighborPairExitIndex; }
        else
        {
            // randomly choose exit
            thisExitindex = Random.Range(0, edgeCells.Count);
        }

        // set cell to exit
        if (direction == Vector2.left)
        {
            leftExitIndex = thisExitindex;
            edgeCells[leftExitIndex].cellType = CELL_TYPE.EXIT;
            exitCells.Add(GetCell(new Vector3(thisExitindex + 1, 0, 0)));
        }
        else if (direction == Vector2.right)
        {
            rightExitIndex = thisExitindex;
            edgeCells[rightExitIndex].cellType = CELL_TYPE.EXIT;
            exitCells.Add(GetCell(new Vector3(thisExitindex + 1, 0, cellCoordMax)));
        }
        else if (direction == Vector2.up)
        {
            topExitIndex = thisExitindex;
            edgeCells[topExitIndex].cellType = CELL_TYPE.EXIT;
            exitCells.Add(GetCell(new Vector3(cellCoordMax, 0, thisExitindex + 1)));
        }
        else if (direction == Vector2.down)
        {
            bottomExitIndex = thisExitindex;
            edgeCells[bottomExitIndex].cellType = CELL_TYPE.EXIT;
            exitCells.Add(GetCell(new Vector3(0, 0, thisExitindex + 1)));
        }
    }

    // <<<< EXPAND EXITS TO PROPER SIZE >>>>
    public void ExpandAllExits()
    {

        int exitSize = dunGenManager.maxExitSize / 2;

        if (needLeftExit)
        {
            ExpandExit(exitSize, leftExitIndex, leftEdgeCells, Vector3.right);
        }

        if (needRightExit)
        {
            ExpandExit(exitSize, rightExitIndex, rightEdgeCells, Vector3.left);
        }


        if (needTopExit)
        {
            ExpandExit(exitSize, topExitIndex, topEdgeCells, Vector3.back);
        }


        if (needBottomExit)
        {
            ExpandExit(exitSize, bottomExitIndex, bottomEdgeCells, Vector3.forward);
        }
    }

    public void ExpandExit(int exitSize, int exitPointIndex, List<Cell> currEdgeCells, Vector3 oppositeDirection)
    {

        foreach (Cell cell in currEdgeCells)
        {
            if (cell != currEdgeCells[exitPointIndex])
            {
                cell.cellType = CELL_TYPE.EXITWALL; // default all exit cells to exitwall
            }
        }

        //inactive cell in front of init exit
        //currEdgeCells[exitPointIndex].GetCellNeighbor(oppositeDirection).cellType = CELL_TYPE.INACTIVE;

        // << EXTEND EXIT BASED ON EXIT SIZE >>
        for (int i = 0; i < exitSize; i++)
        {

            // if exit extension is valid... (lower index)
            if (exitPointIndex - i >= 0 && currEdgeCells[exitPointIndex - i] != null)
            {
                currEdgeCells[exitPointIndex - i].cellType = CELL_TYPE.EXIT;

                // if cell neighbor is valid
                if (currEdgeCells[exitPointIndex - i].GetCellNeighbor(oppositeDirection) != null)
                {
                    //currEdgeCells[exitPointIndex - i].GetCellNeighbor(oppositeDirection).cellType = CELL_TYPE.INACTIVE; // set cell in front of exit to inactive
                }
            }

            // if exit extension is valid... (higher index)
            if (exitPointIndex + i < currEdgeCells.Count && currEdgeCells[exitPointIndex + i] != null)
            {
                currEdgeCells[exitPointIndex + i].cellType = CELL_TYPE.EXIT;

                // if cell neighbor is valid
                if (currEdgeCells[exitPointIndex + i].GetCellNeighbor(oppositeDirection) != null)
                {
                    //currEdgeCells[exitPointIndex + i].GetCellNeighbor(oppositeDirection).cellType = CELL_TYPE.INACTIVE; // set cell in front of exit to inactive
                }
            }
        }
    }
    #endregion=======================================================================================================







    // <<<< DETERMINE SIDE CELLS >>>>
    public void DetermineSideCells()
    {
        foreach (Cell cell in leftEdgeCells)
        {
            if (cell.cellType == CELL_TYPE.WALL)
            {
                SetAllCellsAbove(GetCell(cell.coord), CELL_TYPE.WALL);
                SetAllCellsAbove(GetCell(cell.coord + Vector3.right), CELL_TYPE.SIDE);
            }
        }

        foreach (Cell cell in rightEdgeCells)
        {
            if (cell.cellType == CELL_TYPE.WALL)
            {
                SetAllCellsAbove(GetCell(cell.coord), CELL_TYPE.WALL);
                SetAllCellsAbove(GetCell(cell.coord + Vector3.left), CELL_TYPE.SIDE);
            }
        }

        foreach (Cell cell in topEdgeCells)
        {
            if (cell.cellType == CELL_TYPE.WALL)
            {
                SetAllCellsAbove(GetCell(cell.coord), CELL_TYPE.WALL);
                SetAllCellsAbove(GetCell(cell.coord + Vector3.back), CELL_TYPE.SIDE);
            }
        }

        foreach (Cell cell in bottomEdgeCells)
        {
            if (cell.cellType == CELL_TYPE.WALL)
            {
                SetAllCellsAbove(GetCell(cell.coord), CELL_TYPE.WALL);
                SetAllCellsAbove(GetCell(cell.coord + Vector3.forward), CELL_TYPE.SIDE);
            }
        }

        foreach (Cell cell in cornerCells)
        {
            if (cell.cellType == CELL_TYPE.WALL)
            {
                SetAllCellsAbove(GetCell(cell.coord), CELL_TYPE.WALL);
            }
        }
    }

    // <<<< DETERMINE CEILING CELLS >>>>
    public void DetermineCeilingCells()
    {
        foreach (Cell cell in allCells)
        {
            // horizontal placement // cant be in walls
            if (cell.coord.x > 0 && cell.coord.x < cellCoordMax && cell.coord.z > 0 && cell.coord.z < cellCoordMax)
            {
                if (cell.coord.y == wallHeight) { cell.cellType = CELL_TYPE.CEILING; }
            }
        }
    }

    // ==================================== PATHFINDING ============================================
    // <<<< FIND PATHS FROM EXITS >>>>
    public IEnumerator FindPaths()
    {

        // << FIND PATH A >>
        // if 1 or 2 exits
        if (exitCells.Count == 0) { 

            // if room tile, exits are not required
            if (roomTile) { Debug.LogWarning("Room tile with no exits");  debugPathAFound = true; debugPathBFound = true; yield break; }

            Debug.LogError("No Exits Found", this.gameObject); 
        }
        else if (exitCells.Count == 1)
        {
            //Debug.Log("Center Cell is " + centerCell, this.gameObject);

            // find path from center to exit
            pathA = pathFinderScript.FindRandomPath(allCells, centerCell, exitCells[0], true);
        }
        else if (exitCells.Count >= 2)
        {
            pathA = pathFinderScript.FindRandomPath(allCells, exitCells[0], exitCells[1], true);
        }

        // remove path from empty cells
        foreach (Cell cell in pathA)
        {
            if (emptyFloorCells.Contains(cell)) { emptyFloorCells.Remove(cell); }
        }

        yield return new WaitUntil(() => debugPathAFound);

        // << FIND PATH B >>
        // if more than 2 exits
        if (exitCells.Count > 2)
        {
            if (exitCells.Count == 3)
            {
                pathB = pathFinderScript.FindRandomPath(allCells, exitCells[2], exitCells[Random.Range(0, 2)], false, true); // (0,2) because 2 is exclusive
            }
            else if (exitCells.Count == 4)
            {
                pathB = pathFinderScript.FindRandomPath(allCells, exitCells[2], exitCells[3], false, true);
            }

            // remove path from empty cells
            foreach (Cell cell in pathB)
            {
                if (emptyFloorCells.Contains(cell)) { emptyFloorCells.Remove(cell); }
            }
        }
        else { debugPathBFound = true; }

    }



    // <<<< CREATE OPTIMIZED WALLS >>>>
    /*
     * If an exit is needed, create two seperate walls so that there's space in between
     * Else spawn one big wall
     */
    public void CreateOptimizedWalls()
    {


        // << ADJUST WALL HEIGHT >>
        wallParent.transform.position += new Vector3(0, -cellSize * 0.25f, 0);

        // << ADD WALLS TO GENERATED MESH >>
        foreach (GameObject wall in walls)
        {
            sourceMeshFilters.Add(wall.GetComponentInChildren<MeshFilter>());
        }

    }

    public GameObject ChooseRandomEdgeWall()
    {
        GameObject wall = Instantiate(env_manager.wallEdgeObjs[Random.Range(0, env_manager.wallEdgeObjs.Count)], transform);

        wall.transform.rotation = Quaternion.Euler(Vector3.zero);

        return wall;

    }


    // <<<< SET VALID ROOM WALLS >>>>
    public void SetRoomWalls()
    {
        TileGenerationManager left_neighbor = GetTileNeighbor(Vector2.left);
        TileGenerationManager right_neighbor = GetTileNeighbor(Vector2.right);
        TileGenerationManager top_neighbor = GetTileNeighbor(Vector2.up);
        TileGenerationManager bot_neighbor = GetTileNeighbor(Vector2.down);

        // left wall
        if (left_neighbor != null && left_neighbor.roomTile) 
        {
            leftWallDisabled = true;
            left_neighbor.rightWallDisabled = true;

            foreach (Cell cell in leftEdgeCells)
            {
                SetAllCellsAbove(cell, CELL_TYPE.AIR);
                cell.cellType = CELL_TYPE.EMPTY_FLOOR;
            }

            foreach (Cell cell in left_neighbor.rightEdgeCells)
            {
                SetAllCellsAbove(cell, CELL_TYPE.AIR);
                cell.cellType = CELL_TYPE.EMPTY_FLOOR;
            }

        }

        // right wall
        if (right_neighbor != null && right_neighbor.roomTile)
        {
            rightWallDisabled = true;
            right_neighbor.leftWallDisabled = true;

            foreach (Cell cell in rightEdgeCells)
            {
                SetAllCellsAbove(cell, CELL_TYPE.AIR);
                cell.cellType = CELL_TYPE.EMPTY_FLOOR;
            }

            foreach (Cell cell in right_neighbor.leftEdgeCells)
            {
                SetAllCellsAbove(cell, CELL_TYPE.AIR);
                cell.cellType = CELL_TYPE.EMPTY_FLOOR;
            }
        }

        // top wall
        if (top_neighbor != null && top_neighbor.roomTile)
        {
            topWallDisabled = true;
            top_neighbor.bottomWallDisabled = true;

            foreach (Cell cell in topEdgeCells)
            {
                SetAllCellsAbove(cell, CELL_TYPE.AIR);
                cell.cellType = CELL_TYPE.EMPTY_FLOOR;
            }

            foreach (Cell cell in top_neighbor.bottomEdgeCells)
            {
                SetAllCellsAbove(cell, CELL_TYPE.AIR);
                cell.cellType = CELL_TYPE.EMPTY_FLOOR;
            }
        }

        // bottom wall
        if (bot_neighbor != null && bot_neighbor.roomTile)
        {
            bottomWallDisabled = true;
            bot_neighbor.topWallDisabled = true;

            foreach (Cell cell in bottomEdgeCells)
            {
                SetAllCellsAbove(cell, CELL_TYPE.AIR);
                cell.cellType = CELL_TYPE.EMPTY_FLOOR;
            }

            foreach (Cell cell in bot_neighbor.topEdgeCells)
            {
                SetAllCellsAbove(cell, CELL_TYPE.AIR);
                cell.cellType = CELL_TYPE.EMPTY_FLOOR;
            }
        }

        // <<<< SET CORNERS >>

        // set all disabled walls to empty floor
        if (leftWallDisabled && bottomWallDisabled){ botLeftCorner.cellType = CELL_TYPE.EMPTY_FLOOR;}
        if (rightWallDisabled && bottomWallDisabled) { botRightCorner.cellType = CELL_TYPE.EMPTY_FLOOR; }
        if (leftWallDisabled && topWallDisabled) { topLeftCorner.cellType = CELL_TYPE.EMPTY_FLOOR; }
        if (rightWallDisabled && topWallDisabled) { topRightCorner.cellType = CELL_TYPE.EMPTY_FLOOR; }



    }

    // <<<< SPAWN MODEL FOR EACH CELL >>>>
    public void SpawnAllCellModels()
    {
        // activate all cell models
        foreach (Cell cell in allCells)
        {
            // spawn model from script
            cell.SpawnCellModels();
        }
    }

    // <<<< COMBINE BASIC MESHES - creates better performance >>>>
    
    public void CombineMeshes()
    {
        var combine = new CombineInstance[sourceMeshFilters.Count];

        // get each mesh and add to combine
        for (var i = 0; i < sourceMeshFilters.Count; i++)
        {
            combine[i].mesh = sourceMeshFilters[i].mesh;
            combine[i].transform = sourceMeshFilters[i].transform.localToWorldMatrix;
        }

        var combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combine);
        generatedMeshParent.GetComponent<MeshFilter>().mesh = combinedMesh;

        // DONT TOUCH POSITION (combined meshes act weird)
        generatedMeshParent.transform.localPosition = (tilePosition * -1) + new Vector3(0, cellSize * 0.25f, 0);

        // set shared mesh
        generatedMeshParent.GetComponent<MeshCollider>().sharedMesh = generatedMeshParent.GetComponent<MeshFilter>().mesh;

        // delete the temp objects
        foreach (MeshFilter mesh in sourceMeshFilters)
        {
            Destroy(mesh.gameObject);
        }

        generatedMeshParent.GetComponent<MeshRenderer>().material = dunGenManager.wallMaterial;
        generatedMeshParent.isStatic = true;  
    }
    

    // <<<< SET SPAWNS >>>>
    public void SetSpawns()
    {
        // set player spawn / dungeon start
        if (dungeonBeginning)
        {
            playerSpawn = centerCell.transform;
        }
    }

    public void SetMainPathEnd()
    {
        dungeonEnd = true;

        // destroy current model
        Destroy(centerCell.cellModel);

        // set end model
        GameObject endModel = Instantiate(env_manager.endLevelObject, env_manager.endLevelObject.transform.localPosition + centerCell.transform.position, Quaternion.identity);
        endModel.transform.localScale = new Vector3(cellSize * centerCell.modelScale, cellSize * centerCell.modelScale, cellSize * centerCell.modelScale);
        endModel.transform.parent = centerCell.transform;

        Debug.Log("Spawned end model ", endModel);
    }

    public void ChangeAllCellTypes(CELL_TYPE currCellType, CELL_TYPE changeType)
    {
        foreach (Cell cell in allCells)
        {
            if (cell.cellType == currCellType)
            {
                cell.cellType = changeType;
            }
        }
    }

    //=========================================== HELPER FUNCTIONS =============================================

    public Cell GetCell(Vector3 coord)
    {
        foreach (Cell cell in allCells)
        {
            if (cell.coord == coord) { return cell; }
        }

        return null;
    }

    // << GET EMPTY NEIGHBORS >> ** get all empty neighbor tiles
    public List<Vector2> GetEmptyNeighbors()
    {
        List<Vector2> emptyNeighbors = new List<Vector2>();

        if (tileNeighbors[0] == null) { emptyNeighbors.Add(coord + Vector2.left); }
        if (tileNeighbors[1] == null) { emptyNeighbors.Add(coord + Vector2.right); }
        if (tileNeighbors[2] == null) { emptyNeighbors.Add(coord + Vector2.up); }
        if (tileNeighbors[3] == null) { emptyNeighbors.Add(coord + Vector2.down); }

        return emptyNeighbors;

    }

    public TileGenerationManager GetTileNeighbor(Vector2 direction)
    {
        if (direction == Vector2.left) { return tileNeighbors[0]; }
        if (direction == Vector2.right) { return tileNeighbors[1]; }
        if (direction == Vector2.up) { return tileNeighbors[2]; }
        if (direction == Vector2.down) { return tileNeighbors[3]; }

        return null;

    }


    public void SetAllCellsAbove(Cell cell, CELL_TYPE cellType)
    {
        for (int i = 1; i <= wallHeight; i++)
        {
            GetCell(cell.coord + (Vector3.up * i)).cellType = cellType;
        }
    }

    public Vector3 CellPositionMidpoint(Cell a, Cell b)
    {
        return a.transform.position - (a.transform.position - b.transform.position) / 2;
    }

    public void DebugVisualization()
    {
        LineRenderer lr = GetComponentInChildren<LineRenderer>();
        List<Vector3> points = new List<Vector3>();

        // << GET VERTEX POINTS OF GROUND >>
        float vertOffset = -0.25f;
        Vector3 topLeftVertex = new Vector3(-fullTileSize / 2 + transform.position.x, vertOffset + transform.position.y, fullTileSize / 2 + transform.position.z);
        Vector3 botLeftVertex = new Vector3(-fullTileSize / 2 + transform.position.x, vertOffset + transform.position.y, -fullTileSize / 2 + transform.position.z);
        Vector3 topRightVertex = new Vector3(fullTileSize / 2 + transform.position.x, vertOffset + transform.position.y, fullTileSize / 2 + transform.position.z);
        Vector3 botRightVertex = new Vector3(fullTileSize / 2 + transform.position.x, vertOffset + transform.position.y, -fullTileSize / 2 + transform.position.z);

        points.Add(topLeftVertex);
        points.Add(botLeftVertex);
        points.Add(botRightVertex);
        points.Add(topRightVertex);
        points.Add(topLeftVertex);


        lr.positionCount = points.Count;
        lr.SetPositions(points.ToArray());

    }

}
