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
    public int tileLengthCellCount = 10;

    [Header("Tile Data =============")]
    public Vector2 tileCoord = new Vector2();
    public List<TileGenerationManager> tileNeighbors = new List<TileGenerationManager>(4); // tile neighbors (left, right, top, bottom)
    List<TileGenerationManager> pathNeighbors = new List<TileGenerationManager>();
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
    public int cellSize; // fullTileSize / tileLengthCellCount
    public int cellCoordMax;
    [HideInInspector]
    public List<Cell> allCells = new List<Cell>();
    public List<Cell> emptyCells = new List<Cell>();

    [Header("Pathfinding")]
    public RandomPathFinder pathFinderScript;
    public List<Cell> pathA = new List<Cell>();
    public List<Cell> pathB = new List<Cell>();
    public bool debugPathAFound;
    public bool debugPathBFound;

    [Header("Generation Values")]
    [Range(0f, 1f)]
    public bool debugGenFinished;

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
    public Vector4 exitCode;
    public List<Cell> exitCells = new List<Cell>();

    [Header("Current AI")]
    public List<GroundAI> allGroundAi = new List<GroundAI>();

    public void Start()
    {
        if (indivDebug) { TileInit(true); }
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
                //cell.SetDebugCube(indivDebug);
            }
        }
        

        //DebugVisualization();
    }

    // Start is called before the first frame update
    public void TileInit(bool debug = false)
    {
        pathFinderScript = GetComponent<RandomPathFinder>();
        dunGenManager = GetComponentInParent<DungeonGenerationManager>();
        env_manager = dunGenManager.env_manager;

        // makes sure cell size is never less than 1
        if (fullTileSize < tileLengthCellCount)
        {
            Debug.LogWarning("Changed TileLengthCellCount from " + tileLengthCellCount + " to " + fullTileSize + " to keep cell size at minimum 1");

            // reset ratio of cellCount / tileSize to 1
            tileLengthCellCount = fullTileSize;
            dunGenManager.tileLengthCellCount = fullTileSize;

        }

        // set individual cell size
        cellSize = (fullTileSize / tileLengthCellCount);

        // set max value of coords
        cellCoordMax = (fullTileSize / cellSize) - 1;

        // set tile height
        tilePosition += new Vector3(0, tileHeightLevel * cellSize, 0);

        // depending on what the tallest tile is , the "total wall height" is tallest tile height + min wall height
        wallHeight = (dunGenManager.cur_tallestTileHeight + dunGenManager.minWallHeight) - tileHeightLevel; // offset so that all walls meet at the same height

        // set center cell of tile
        centerCell = GetCell(new Vector2(cellCoordMax / 2, cellCoordMax / 2));

        // << CREATE BASE GROUND >>
        // tile ground is scaled for inner part of tile, edge grounds are spawned individually
        GameObject tileGroundPrefab = dunGenManager.env_manager.groundPrefabs[Random.Range(0, dunGenManager.env_manager.groundPrefabs.Count)];
        tileGround = Instantiate(tileGroundPrefab, transform);

        int fullDungeonHeight = (dunGenManager.maxTileHeight - dunGenManager.minTileHeight) * cellSize; // tile levels * cell size

        tileGround.transform.position = tilePosition + new Vector3(0, -fullDungeonHeight / 2, 0);
        tileGround.transform.localScale = new Vector3(fullTileSize, fullDungeonHeight, fullTileSize);
        tileGround.name = "TileGround";

        if (fullDungeonHeight % 2 != 0) { tileGround.transform.position += new Vector3(0, -0.5f, 0); } // ugh math is annoying sometimes

        // << CREATE WALL PARENT >>
        wallParent = new GameObject("Wall Parent");
        wallParent.transform.parent = transform;

        // << CREATE CEILING >>
        if (dunGenManager.env_manager.spawnCeilings)
        {
            GameObject tileCeilingPrefab = dunGenManager.env_manager.ceilingPrefabs[Random.Range(0, dunGenManager.env_manager.ceilingPrefabs.Count)];
            tileCeiling = Instantiate(tileCeilingPrefab, transform);

            if (roomTile)
            {
                // position ceiling on top of generation
                tileCeiling.transform.position = tilePosition + new Vector3(0, (wallHeight * cellSize) + (cellSize / 2), 0); //height of cell, plus offset
                tileCeiling.transform.localScale = new Vector3(fullTileSize, cellSize, fullTileSize);
            }
            else
            {
                // position ceiling at the height of the wall so that it's surrounded by walls instead of on top 
                tileCeiling.transform.position = tilePosition + new Vector3(0, (wallHeight * cellSize) + (cellSize / 2) - cellSize, 0); //height of cell, plus offset
                tileCeiling.transform.localScale = new Vector3(fullTileSize - (2 * cellSize), cellSize, fullTileSize - (2 * cellSize));
            }

            tileCeiling.transform.position += new Vector3(0, -0.5f, 0); // trying to fix bug, dont want to mess up math so here we are

            tileCeiling.name = "TileCeiling";
            tileCeiling.transform.parent = transform;

            // add to sorce mesh filters to combine later
            sourceMeshFilters.Add(tileCeiling.GetComponentInChildren<MeshFilter>());
        }

        //Debug.Log("Grid Size: " + fullTileSize + " Cell Size: " + cellSize);



        StartGeneration(debug);
    }


    // ========================================== GENERATION ==============================================

    // <<<< MAIN GENERATE FUNCTION >>>>
    public void StartGeneration(bool debug = false)
    {
        DestroyAllCellParents();
        pathFinderScript.Reset();


        if (indivDebug)
        {
            tileNeighbors.Add(null);
            tileNeighbors.Add(null);
            tileNeighbors.Add(null);
            tileNeighbors.Add(null);
        }

        CreateCellParents();

        /*
        if (debug)
        {
            StartCoroutine(DebugGeneration(0.1f, 0.01f));
        }
        else
        {
            StartCoroutine(Generate());
        }*/

        StartCoroutine(Generate());

    }

    // <<<< GENERATION >>>>
    public IEnumerator Generate()
    {
        // set cell neighbors
        foreach (Cell cell in allCells) { cell.SetCellNeighbors(); }

        // determine which cells are edges of the grid
        DetermineEdges();

        // create entrance / exit pairs
        ChooseExits();

        // find paths from exits
        StartCoroutine(FindPaths());

        yield return new WaitUntil(() => debugPathAFound);
        yield return new WaitUntil(() => debugPathBFound);


        // expand exit doorways without messing with pathfinding
        ExpandExits();

        // turn on / off walls based on rooms
        SetRoomWalls();

        debugGenFinished = true;
        yield return new WaitUntil(() => dunGenManager.generationFinished);

        // create walls
        CreateOptimizedWalls();



        // create random obstacles in empty cells
        CreateRandomObstacles(dunGenManager.obstacleRandomness);


        // spawn models for each cell
        SpawnAllCellModels();

        // combine all cell meshes 
        CombineMeshes();

        // set all spawn points
        SetSpawns();





    }

    // <<<< SLOW VISUAL GENERATION >>>>
    public IEnumerator DebugGeneration(float delay, float pathfindingDelay)
    {
        // determine which cells are edges of the grid
        DetermineEdges();
        Debug.Log(">>>> found edges", this.gameObject);

        yield return new WaitForSeconds(delay);

        // create entrance / exit pairs
        ChooseExits();
        Debug.Log(">>>> exit count: " + exitCells.Count, this.gameObject);

        yield return new WaitForSeconds(delay);

        // << FIND PATH A >>
        // if 1 or 2 exits

        IEnumerator pathACoroutine = null;

        if (exitCells.Count == 1)
        {
            // get cell in center of tile
            Cell centerCell = GetCell(new Vector2(cellCoordMax / 2, cellCoordMax / 2));

            // find path from center to exit
            pathACoroutine = pathFinderScript.DebugFindRandomPath(allCells, centerCell, exitCells[0], pathfindingDelay, true, false);

            StartCoroutine(pathACoroutine);

        }
        else if (exitCells.Count >= 2)
        {

            pathACoroutine = pathFinderScript.DebugFindRandomPath(allCells, exitCells[0], exitCells[1], pathfindingDelay, true, false);

            StartCoroutine(pathACoroutine);
        }

        yield return new WaitUntil(() => debugPathAFound);
        StopCoroutine(pathACoroutine);

        // remove path from empty cells
        foreach (Cell cell in pathA)
        {
            if (emptyCells.Contains(cell)) { emptyCells.Remove(cell); }
        }

        Debug.Log(">>>> found pathA", this.gameObject);


        // << FIND PATH B >>
        // if multiple exits
        if (exitCells.Count > 2)
        {
            IEnumerator pathBCoroutine = null;

            if (exitCells.Count == 3)
            {              
                                                                                                                  // (0,2) because 2 is exclusive
                pathBCoroutine = pathFinderScript.DebugFindRandomPath(allCells, exitCells[2], exitCells[Random.Range(0, 2)], pathfindingDelay, false, true);

                StartCoroutine(pathBCoroutine);

            }
            else if (exitCells.Count == 4)
            {
                pathBCoroutine = pathFinderScript.DebugFindRandomPath(allCells, exitCells[2], exitCells[3], pathfindingDelay, false, true);

                StartCoroutine(pathBCoroutine);
            }

            yield return new WaitUntil(() => debugPathBFound);
            StopCoroutine(pathBCoroutine);
        }


        // remove path from empty cells
        foreach (Cell cell in pathB)
        {
            if (emptyCells.Contains(cell)) { emptyCells.Remove(cell); }
        }

        Debug.Log(">>>> found pathB", this.gameObject);

        yield return new WaitForSeconds(delay);

        CreateRandomObstacles(dunGenManager.obstacleRandomness);
        Debug.Log(">>>> created obstacles", this.gameObject);

        yield return new WaitForSeconds(delay);

        SpawnAllCellModels();
        Debug.Log(">>>> spawned cell models", this.gameObject);

        yield return new WaitForSeconds(delay);

        //CombineMeshes();
        Debug.Log(">>>> combined meshes", this.gameObject);

        // set all spawn points
        SetSpawns();
        Debug.Log(">>>> created spawn points", this.gameObject);


        debugGenFinished = true;
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
        emptyCells.Clear();

        exitCells.Clear();
    }


    // <<<< FIND CENTER POS OF EACH CELL AND CREATE PARENT >>>>
    public void CreateCellParents()
    {
        // << FIND CENTERS OF CELLS >>
        Vector3 botLeftCellCenter = tilePosition + new Vector3((float)-fullTileSize / 2, 0, (float)-fullTileSize / 2) + new Vector3((float)cellSize / 2, 0, (float)cellSize / 2);
        //Debug.Log("Top Left Cell Center: " + topLeftCellCenter);
        for (float y = 0; y < fullTileSize / cellSize; y += 1)
        {
            for (float x = 0; x < fullTileSize / cellSize; x += 1)
            {
                // get position of cell center
                Vector3 cellCenterPos = botLeftCellCenter + new Vector3(y * cellSize, 0, x * cellSize);

                // create cell , child of cell transform parent
                GameObject cell = Instantiate(cellPrefab, cellCenterPos, Quaternion.identity);
                cell.transform.parent = cellTransformParent.transform;

                // get cellParent script
                Cell cellParentScript = cell.GetComponent<Cell>();
                allCells.Add(cellParentScript); // add to all cells
                cellParentScript.coord = new Vector2(x, y); // set coord
                cell.name = "CellParent" + cellParentScript.coord.ToString(); // name cell 
                cellParentScript.tileGenManager = gameObject.GetComponent<TileGenerationManager>();
                cellParentScript.dunGenManager = cellParentScript.tileGenManager.dunGenManager;


            }
        }

        // set center cell of tile
        centerCell = GetCell(new Vector2(cellCoordMax / 2, cellCoordMax / 2));



    }


    // <<<< DETERMINE WHICH CELLS ARE EDGES >>>>
    public void DetermineEdges()
    {

        foreach (Cell cell in allCells)
        {
            bool isValidEdge = false;

            // if corner
            if (cell.coord == Vector2.zero || cell.coord == new Vector2(0, cellCoordMax)
                || cell.coord == new Vector2(cellCoordMax, 0) || cell.coord == new Vector2(cellCoordMax, cellCoordMax))
            {
                // set to wall, give edge type but don't put in list
                cell.cellType = 0;
                cell.isCorner = true;

                isValidEdge = true;
            }


            // get left edge
            if (cell.coord.y == 0)
            {
                cell.isLeftEdge = true;
                cell.isRightEdge = false;

                cell.cellType = 0;
                if (!cell.isCorner) { leftEdgeCells.Add(cell); } // if not corner add to list

                isValidEdge = true;

            }

            // get right edge
            if (cell.coord.y == cellCoordMax)
            {
                cell.isLeftEdge = false;
                cell.isRightEdge = true;

                cell.cellType = 0;
                if (!cell.isCorner) { rightEdgeCells.Add(cell); }

                isValidEdge = true;

            }

            // get top edge
            if (cell.coord.x == cellCoordMax)
            {
                cell.isTopEdge = true;
                cell.isBottomEdge = false;

                cell.cellType = 0;
                if (!cell.isCorner) { topEdgeCells.Add(cell); }

                isValidEdge = true;

            }

            // get bottom edge
            if (cell.coord.x == 0)
            {
                cell.isTopEdge = false;
                cell.isBottomEdge = true;

                cell.cellType = 0;
                if (!cell.isCorner) { bottomEdgeCells.Add(cell); }

                isValidEdge = true;

            }

            // else add to empty cells
            if (!isValidEdge)
            {
                emptyCells.Add(cell);
            }
        }
    }


    // <<<< CHOOSE EXITS & ENTRANCES FROM EDGE CELLS >>>>
    public void ChooseExits()
    {
        // if left exit
        if (needLeftExit)
        {
            // check if neighbor has chosen opposite exit already
            if (tileNeighbors[0] != null && tileNeighbors[0].rightExitIndex != -1) { leftExitIndex = tileNeighbors[0].rightExitIndex; }
            else
            {
                // randomly choose exit
                leftExitIndex = Random.Range(0, leftEdgeCells.Count);
            }

            // set cell to exit
            leftEdgeCells[leftExitIndex].cellType = 1;
            exitCells.Add(GetCell(new Vector2(leftExitIndex + 1, 0)));

        }

        // if right exit
        if (needRightExit)
        {
            // check if neighbor has chosen exit already
            if (tileNeighbors[1] != null && tileNeighbors[1].leftExitIndex != -1) { rightExitIndex = tileNeighbors[1].leftExitIndex; }
            else
            {
                // randomly choose exit
                rightExitIndex = Random.Range(0, rightEdgeCells.Count);
            }

            // set cell to exit
            rightEdgeCells[rightExitIndex].cellType = 1;
            exitCells.Add(GetCell(new Vector2(rightExitIndex + 1, cellCoordMax)));


        }

        // if top exit
        if (needTopExit)
        {
            // check if neighbor has chosen exit already
            if (tileNeighbors[2] != null && tileNeighbors[2].bottomExitIndex != -1) { topExitIndex = tileNeighbors[2].bottomExitIndex; }
            else
            {
                // randomly choose exit
                topExitIndex = Random.Range(0, topEdgeCells.Count);
            }

            // set cell to exit
            topEdgeCells[topExitIndex].cellType = 1;
            exitCells.Add(GetCell(new Vector2(cellCoordMax, topExitIndex + 1)));


        }

        // if bottom exit
        if (needBottomExit)
        {
            // check if neighbor has chosen exit already
            if (tileNeighbors[3] != null && tileNeighbors[3].topExitIndex != -1) { bottomExitIndex = tileNeighbors[3].topExitIndex; }
            else
            {
                // randomly choose exit
                bottomExitIndex = Random.Range(0, bottomEdgeCells.Count);
            }

            // set cell to exit
            bottomEdgeCells[bottomExitIndex].cellType = 1;
            exitCells.Add(GetCell(new Vector2(0, bottomExitIndex + 1)));


        }
    }


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
            if (emptyCells.Contains(cell)) { emptyCells.Remove(cell); }
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
                if (emptyCells.Contains(cell)) { emptyCells.Remove(cell); }
            }
        }
        else { debugPathBFound = true; }

    }


    // <<<< CREATE RANDOM OBSTACLES WITHIN GRID SPACE >>>>
    public void CreateRandomObstacles(float percentage = 0.2f)
    {
        // just in case input is over 100%
        if (percentage > 1) { percentage = 1; }

        List<Cell> noLongerEmptyCells = new List<Cell>();

        // randomly decide if empty cell should become obstacle
        foreach (Cell cell in emptyCells)
        {
            // if cell is inactive, remove from empty cells
            if (cell.cellType == -3)
            {
                noLongerEmptyCells.Add(cell);
            }

            else if (Random.Range((float)0,(float)1) < percentage)
            {
                cell.cellType = 0;
                noLongerEmptyCells.Add(cell);
            }
        }

        // remove each newly filled cell
        foreach (Cell cell in noLongerEmptyCells)
        {
            emptyCells.Remove(cell);
        }
    }

    // <<<< EXPAND EXITS TO PROPER SIZE >>>>
    public void ExpandExits()
    {

        int exitSize = dunGenManager.maxExitSize / 2;

        // for each side of exit
        for (int i = 0; i <= exitSize; i++)
        {
            // << LEFT EXIT >>
            if (needLeftExit) 
            {

                //inactive cell in front of init exit
                GetCell(new Vector2(leftExitIndex + 1, 1)).cellType = -3;

                // if exit extension is valid... (to the left)
                if (leftExitIndex - i >= 0 && leftEdgeCells[leftExitIndex - i] != null)
                {
                    leftEdgeCells[leftExitIndex - i].cellType = 1;

                    // if cell neighbor is valid
                    if (leftEdgeCells[leftExitIndex - i].cellNeighbors[1] != null)
                    {
                        leftEdgeCells[leftExitIndex - i].cellNeighbors[1].cellType = -3; // set cell in front of exit to inactive (right neighbor)
                    }
                }

                // if exit extension is valid... (to the right)
                if (leftExitIndex + i < leftEdgeCells.Count && leftEdgeCells[leftExitIndex + i] != null)
                {
                    leftEdgeCells[leftExitIndex + i].cellType = 1;

                    // if cell neighbor is valid
                    if (leftEdgeCells[leftExitIndex + i].cellNeighbors[1] != null)
                    {
                        leftEdgeCells[leftExitIndex + i].cellNeighbors[1].cellType = -3; // set cell in front of exit to inactive (right neighbor)
                    }
                }
            }

            
            // << RIGHT EXIT >>
            if (needRightExit) 
            {

                //inactive cell in front of init exit
                GetCell(new Vector2(rightExitIndex + 1, cellCoordMax - 1)).cellType = -3;

                // if exit extension is valid... (to the left)
                if (rightExitIndex - i >= 0 && rightEdgeCells[rightExitIndex - i] != null)
                {
                    rightEdgeCells[rightExitIndex - i].cellType = 1;
                    
                    // if cell neighbor is valid
                    if (rightEdgeCells[rightExitIndex - i].cellNeighbors[0] != null)
                    {
                        rightEdgeCells[rightExitIndex - i].cellNeighbors[0].cellType = -3; // set cell in front of exit to inactive (left neighbor)
                    }

                }

                // if exit extension is valid... (to the right)
                if (rightExitIndex + i < rightEdgeCells.Count && rightEdgeCells[rightExitIndex + i] != null)
                {
                    rightEdgeCells[rightExitIndex + i].cellType = 1;

                    // if cell neighbor is valid
                    if (rightEdgeCells[rightExitIndex + i].cellNeighbors[0] != null)
                    {
                        rightEdgeCells[rightExitIndex + i].cellNeighbors[0].cellType = -3; // set cell in front of exit to inactive (left neighbor)
                    }

                }

            }
            
            // << TOP EXIT >>
            if (needTopExit ) 
            {
                //inactive cell in front of init exit
                GetCell(new Vector2(cellCoordMax - 1, topExitIndex + 1)).cellType = -3;

                // if exit extension is valid... (to the left)
                if (topExitIndex - i >= 0 && topEdgeCells[topExitIndex - i] != null)
                {
                    topEdgeCells[topExitIndex - i].cellType = 1;

                    // if cell neighbor is valid
                    if (topEdgeCells[topExitIndex - i].cellNeighbors[3] != null)
                    {
                        topEdgeCells[topExitIndex - i].cellNeighbors[3].cellType = -3; // set cell in front of exit to inactive (bottom neighbor)
                    }
                }

                // if exit extension is valid... (to the right)
                if (topExitIndex + i < topEdgeCells.Count && topEdgeCells[topExitIndex + i] != null)
                {
                    topEdgeCells[topExitIndex + i].cellType = 1;

                    // if cell neighbor is valid
                    if (topEdgeCells[topExitIndex + i].cellNeighbors[3] != null)
                    {
                        topEdgeCells[topExitIndex + i].cellNeighbors[3].cellType = -3; // set cell in front of exit to inactive (bottom neighbor)
                    }
                }

            }
            
            // << BOTTOM EXIT >>
            if (needBottomExit) 
            {
                //inactive cell in front of init exit
                GetCell(new Vector2(1, bottomExitIndex + 1)).cellType = -3;

                // if exit extension is valid... (to the left)
                if (bottomExitIndex - i >= 0 && bottomEdgeCells[bottomExitIndex - i] != null)
                {

                    bottomEdgeCells[bottomExitIndex - i].cellType = 1;

                    // if cell neighbor is valid
                    if (bottomEdgeCells[bottomExitIndex - i].cellNeighbors[2] != null)
                    {
                        bottomEdgeCells[bottomExitIndex - i].cellNeighbors[2].cellType = -3; // set cell in front of exit to inactive (top neighbor)
                    }

                }

                // if exit extension is valid... (to the right)
                if (bottomExitIndex + i < bottomEdgeCells.Count && bottomEdgeCells[bottomExitIndex + i] != null)
                {

                    bottomEdgeCells[bottomExitIndex + i].cellType = 1;

                    // if cell neighbor is valid
                    if (bottomEdgeCells[bottomExitIndex + i].cellNeighbors[2] != null)
                    {
                        bottomEdgeCells[bottomExitIndex + i].cellNeighbors[2].cellType = -3; // set cell in front of exit to inactive (top neighbor)
                    }

                }
            }
            
        }





    }

    // <<<< CREATE OPTIMIZED WALLS >>>>
    public void CreateOptimizedWalls()
    {
        // << LEFT WALL >>
        if (!leftWallDisabled)
        {
            // if needs an exit
            if (needLeftExit)
            {

                // get lowest and highest exit points on wall edge
                int lowestExitIndex = -1;
                int highestExitIndex = -1;

                
                for (int i = 0; i < leftEdgeCells.Count; i++)
                {
                    if (leftEdgeCells[i].cellType == 1 && (lowestExitIndex == -1 || i < lowestExitIndex)) { lowestExitIndex = i; }
                    if (leftEdgeCells[i].cellType == 1 && (highestExitIndex == -1 || i > highestExitIndex)) { highestExitIndex = i; }
                }


                // **** LEFT WALL 1 ****
                if (lowestExitIndex != 0)
                {
                    GameObject leftwall_1 = ChooseRandomEdgeWall();
                    leftwall_1.transform.parent = wallParent.transform;
                    leftwall_1.name = "Left Wall 1";

                    leftwall_1.transform.position = CellPositionMidpoint(leftEdgeCells[0], leftEdgeCells[lowestExitIndex]);
                    leftwall_1.transform.position += new Vector3(0, (cellSize * wallHeight) / 2, -cellSize/2);

                    leftwall_1.transform.localScale = new Vector3(cellSize, cellSize * wallHeight, cellSize * (lowestExitIndex));

                    walls.Add(leftwall_1);
                }


                // **** LEFT WALL 2 ****
                if (highestExitIndex != leftEdgeCells.Count - 1)
                {
                    GameObject leftwall_2 = ChooseRandomEdgeWall();
                    leftwall_2.transform.parent = wallParent.transform;
                    leftwall_2.name = "Left Wall 2";

                    leftwall_2.transform.position = CellPositionMidpoint(leftEdgeCells[leftEdgeCells.Count - 1], leftEdgeCells[highestExitIndex]);
                    leftwall_2.transform.position += new Vector3(0, (cellSize * wallHeight) / 2, cellSize / 2);

                    leftwall_2.transform.localScale = new Vector3(cellSize, cellSize * wallHeight, cellSize * ((leftEdgeCells.Count - 1) - highestExitIndex));

                    walls.Add(leftwall_2);

                }
            }
            else
            {
                GameObject leftWall = ChooseRandomEdgeWall();
                leftWall.transform.parent = wallParent.transform;
                leftWall.name = "Left Wall";

                leftWall.transform.position = leftEdgeCells[0].transform.position - (leftEdgeCells[0].transform.position - leftEdgeCells[leftEdgeCells.Count - 1].transform.position) / 2;
                leftWall.transform.position += new Vector3(0, (cellSize * wallHeight) / 2, 0);


                leftWall.transform.localScale = new Vector3(cellSize, cellSize * wallHeight, cellSize * leftEdgeCells.Count); // + 2 to account for corners

                walls.Add(leftWall);

            }
        }

        // << RIGHT WALL >>
        if (!rightWallDisabled)
        {

            // if needs an exit
            if (needRightExit)
            {
                // get lowest and highest exit points on wall edge
                int lowestExitIndex = -1;
                int highestExitIndex = -1;


                for (int i = 0; i < rightEdgeCells.Count; i++)
                {
                    if (rightEdgeCells[i].cellType == 1 && (lowestExitIndex == -1 || i < lowestExitIndex)) { lowestExitIndex = i; }
                    if (rightEdgeCells[i].cellType == 1 && (highestExitIndex == -1 || i > highestExitIndex)) { highestExitIndex = i; }
                }

                // **** RIGHT WALL 1 ****
                if (lowestExitIndex != 0)
                {
                    GameObject rightwall_1 = ChooseRandomEdgeWall();
                    rightwall_1.transform.parent = wallParent.transform;
                    rightwall_1.name = "Right Wall 1";

                    rightwall_1.transform.position = CellPositionMidpoint(rightEdgeCells[0], rightEdgeCells[lowestExitIndex]);
                    rightwall_1.transform.position += new Vector3(0, (cellSize * wallHeight) / 2, -cellSize/2);


                    rightwall_1.transform.localScale = new Vector3(cellSize, cellSize * wallHeight, cellSize * (lowestExitIndex));


                    walls.Add(rightwall_1);

                }

                // **** LEFT WALL 2 ****
                if (highestExitIndex != rightEdgeCells.Count - 1)
                {
                    GameObject rightwall_2 = ChooseRandomEdgeWall();
                    rightwall_2.transform.parent = wallParent.transform;
                    rightwall_2.name = "Right Wall 2";

                    rightwall_2.transform.position = CellPositionMidpoint(rightEdgeCells[rightEdgeCells.Count - 1], rightEdgeCells[highestExitIndex]);
                    rightwall_2.transform.position += new Vector3(0, (cellSize * wallHeight) / 2, cellSize / 2);

                    rightwall_2.transform.localScale = new Vector3(cellSize, cellSize * wallHeight, cellSize * ((rightEdgeCells.Count - 1) - highestExitIndex));

                    walls.Add(rightwall_2);

                }

            }
            else
            {
                GameObject rightwall = ChooseRandomEdgeWall();
                rightwall.transform.parent = wallParent.transform;
                rightwall.name = "Right Wall";

                rightwall.transform.position = rightEdgeCells[0].transform.position - (rightEdgeCells[0].transform.position - rightEdgeCells[rightEdgeCells.Count - 1].transform.position) / 2;
                rightwall.transform.position += new Vector3(0, (cellSize * wallHeight) / 2, 0);


                rightwall.transform.localScale = new Vector3(cellSize, cellSize * wallHeight, cellSize * rightEdgeCells.Count); // + 2 to account for corners

                walls.Add(rightwall);

            }
        }

        // << TOP WALL >>
        if (!topWallDisabled)
        {
            // if needs an exit
            if (needTopExit)
            {
                // get lowest and highest exit points on wall edge
                int lowestExitIndex = -1;
                int highestExitIndex = -1;


                for (int i = 0; i < topEdgeCells.Count; i++)
                {
                    if (topEdgeCells[i].cellType == 1 && (lowestExitIndex == -1 || i < lowestExitIndex)) { lowestExitIndex = i; }
                    if (topEdgeCells[i].cellType == 1 && (highestExitIndex == -1 || i > highestExitIndex)) { highestExitIndex = i; }
                }

                // **** TOP WALL 1 ****
                if (lowestExitIndex != 0)
                {
                    GameObject topwall_1 = ChooseRandomEdgeWall();
                    topwall_1.transform.parent = wallParent.transform;
                    topwall_1.name = "Top Wall 1";

                    topwall_1.transform.position = CellPositionMidpoint(topEdgeCells[0], topEdgeCells[lowestExitIndex]);
                    topwall_1.transform.position += new Vector3(-cellSize / 2, (cellSize * wallHeight) / 2, 0);


                    topwall_1.transform.localScale = new Vector3(cellSize * lowestExitIndex, cellSize * wallHeight, cellSize);

                    walls.Add(topwall_1);

                }

                // **** TOP WALL 2 ****
                if (highestExitIndex != topEdgeCells.Count - 1)
                {
                    GameObject topwall_2 = ChooseRandomEdgeWall();
                    topwall_2.transform.parent = wallParent.transform;
                    topwall_2.name = "Top Wall 2";

                    topwall_2.transform.position = CellPositionMidpoint(topEdgeCells[topEdgeCells.Count - 1], topEdgeCells[highestExitIndex]);
                    topwall_2.transform.position += new Vector3(cellSize / 2, (cellSize * wallHeight) / 2, 0);

                    topwall_2.transform.localScale = new Vector3(cellSize * ((topEdgeCells.Count - 1) - highestExitIndex), cellSize * wallHeight, cellSize);

                    walls.Add(topwall_2);

                }
            }
            else
            {
                GameObject topwall = ChooseRandomEdgeWall();
                topwall.transform.parent = wallParent.transform;
                topwall.name = "Top Wall";

                topwall.transform.position = topEdgeCells[0].transform.position - (topEdgeCells[0].transform.position - topEdgeCells[topEdgeCells.Count - 1].transform.position) / 2;
                topwall.transform.position += new Vector3(0, (cellSize * wallHeight) / 2, 0);


                topwall.transform.localScale = new Vector3(cellSize * topEdgeCells.Count, cellSize * wallHeight, cellSize); // + 2 to account for corners

                walls.Add(topwall);

            }
        }

        // << BOTTOM WALL >>
        if (!bottomWallDisabled)
        {

            // if needs an exit
            if (needBottomExit)
            {
                // get lowest and highest exit points on wall edge
                int lowestExitIndex = -1;
                int highestExitIndex = -1;


                for (int i = 0; i < topEdgeCells.Count; i++)
                {
                    if (bottomEdgeCells[i].cellType == 1 && (lowestExitIndex == -1 || i < lowestExitIndex)) { lowestExitIndex = i; }
                    if (bottomEdgeCells[i].cellType == 1 && (highestExitIndex == -1 || i > highestExitIndex)) { highestExitIndex = i; }
                }

                // **** BOTTOM WALL 1 ****
                if (lowestExitIndex != 0)
                {
                    GameObject bottomwall_1 = ChooseRandomEdgeWall();
                    bottomwall_1.transform.parent = wallParent.transform;
                    bottomwall_1.name = "Bottom Wall 1";

                    bottomwall_1.transform.position = CellPositionMidpoint(bottomEdgeCells[0], bottomEdgeCells[lowestExitIndex]);
                    bottomwall_1.transform.position += new Vector3(-cellSize / 2, (cellSize * wallHeight) / 2, 0);


                    bottomwall_1.transform.localScale = new Vector3(cellSize * lowestExitIndex, cellSize * wallHeight, cellSize);

                    walls.Add(bottomwall_1);


                }

                // **** BOTTOM WALL 2 ****
                if (highestExitIndex != bottomEdgeCells.Count - 1)
                {
                    GameObject bottomwall_2 = ChooseRandomEdgeWall();
                    bottomwall_2.transform.parent = wallParent.transform;
                    bottomwall_2.name = "Bottom Wall 2";

                    bottomwall_2.transform.position = CellPositionMidpoint(bottomEdgeCells[bottomEdgeCells.Count - 1], bottomEdgeCells[highestExitIndex]);
                    bottomwall_2.transform.position += new Vector3(cellSize / 2, (cellSize * wallHeight) / 2, 0);

                    bottomwall_2.transform.localScale = new Vector3(cellSize * ((bottomEdgeCells.Count - 1) - highestExitIndex), cellSize * wallHeight, cellSize);

                    walls.Add(bottomwall_2);

                }
            }
            else
            {
                GameObject bottomwall = ChooseRandomEdgeWall();
                bottomwall.transform.parent = wallParent.transform;
                bottomwall.name = "Bottom Wall";

                bottomwall.transform.position = bottomEdgeCells[0].transform.position - (bottomEdgeCells[0].transform.position - bottomEdgeCells[bottomEdgeCells.Count - 1].transform.position) / 2;
                bottomwall.transform.position += new Vector3(0, (cellSize * wallHeight) / 2, 0);


                bottomwall.transform.localScale = new Vector3(cellSize * bottomEdgeCells.Count, cellSize * wallHeight, cellSize); // + 2 to account for corners
                
                walls.Add(bottomwall);

            }
        }
    
        // <<<< CORNERS >>>>
        if (!leftWallDisabled || !topWallDisabled)
        {
            GameObject topleftCorner = ChooseRandomEdgeWall();
            topleftCorner.transform.parent = wallParent.transform;
            topleftCorner.name = "Top Left Corner";

            topleftCorner.transform.position = leftEdgeCells[leftEdgeCells.Count - 1].transform.position + new Vector3(0, (cellSize * wallHeight) / 2, cellSize);
            topleftCorner.transform.localScale = new Vector3(cellSize, (cellSize * wallHeight), cellSize);

            walls.Add(topleftCorner);


        }

        if (!rightWallDisabled || !topWallDisabled)
        {
            GameObject topRightCorner = ChooseRandomEdgeWall();
            topRightCorner.transform.parent = wallParent.transform;
            topRightCorner.name = "Top Right Corner";

            topRightCorner.transform.position = rightEdgeCells[rightEdgeCells.Count - 1].transform.position + new Vector3(0, (cellSize * wallHeight) / 2, cellSize);
            topRightCorner.transform.localScale = new Vector3(cellSize, (cellSize * wallHeight), cellSize);

            walls.Add(topRightCorner);

        }

        if (!leftWallDisabled || !bottomWallDisabled)
        {
            GameObject botLeftCorner = ChooseRandomEdgeWall();
            botLeftCorner.transform.parent = wallParent.transform;
            botLeftCorner.name = "Bottom Left Corner";

            botLeftCorner.transform.position = leftEdgeCells[0].transform.position + new Vector3(0, (cellSize * wallHeight) / 2, -cellSize);
            botLeftCorner.transform.localScale = new Vector3(cellSize, (cellSize * wallHeight), cellSize);
            walls.Add(botLeftCorner);

        }

        if (!rightWallDisabled || !bottomWallDisabled)
        {
            GameObject botRightCorner = ChooseRandomEdgeWall();
            botRightCorner.transform.parent = wallParent.transform;
            botRightCorner.name = "Bottom Right Corner";

            botRightCorner.transform.position = rightEdgeCells[0].transform.position + new Vector3(0, (cellSize * wallHeight) / 2, -cellSize);
            botRightCorner.transform.localScale = new Vector3(cellSize, (cellSize * wallHeight), cellSize);
            walls.Add(botRightCorner);

        }

        // << ADJUST WALL HEIGHT >>
        wallParent.transform.position += new Vector3(0, -0.5f, 0);

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
        // left wall
        if (tileNeighbors[0] != null && tileNeighbors[0].roomTile) { tileNeighbors[0].rightWallDisabled = true; }

        // right wall
        if (tileNeighbors[1] != null && tileNeighbors[1].roomTile) {  tileNeighbors[1].leftWallDisabled = true; }

        // top wall
        if (tileNeighbors[2] != null && tileNeighbors[2].roomTile) {  tileNeighbors[2].bottomWallDisabled = true; }

        // bottom wall
        if (tileNeighbors[3] != null && tileNeighbors[3].roomTile) {  tileNeighbors[3].topWallDisabled = true; }

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
        //generatedMeshParent.transform.localPosition = new Vector3(tilePosition.x * -1, (-tilePosition.y * 2)+ 0.5f, tilePosition.z * -1); ;
        generatedMeshParent.transform.localPosition = new Vector3(tilePosition.x * -1, 0, tilePosition.z * -1); ;
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
        GameObject endModel = Instantiate(env_manager.endTileCenterSpawnObject, env_manager.endTileCenterSpawnObject.transform.localPosition + centerCell.transform.position, Quaternion.identity);
        endModel.transform.localScale = new Vector3(cellSize * centerCell.modelScale, cellSize * centerCell.modelScale, cellSize * centerCell.modelScale);
        endModel.transform.parent = centerCell.transform;

        Debug.Log("Spawned end model ", endModel);
    }





    //=========================================== HELPER FUNCTIONS =============================================

    public Cell GetCell(Vector2 coord)
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

        if (tileNeighbors[0] == null) { emptyNeighbors.Add(tileCoord + Vector2.left); }
        if (tileNeighbors[1] == null) { emptyNeighbors.Add(tileCoord + Vector2.right); }
        if (tileNeighbors[2] == null) { emptyNeighbors.Add(tileCoord + Vector2.up); }
        if (tileNeighbors[3] == null) { emptyNeighbors.Add(tileCoord + Vector2.down); }

        return emptyNeighbors;

    }

    // << SET NECESSARY EXITS BASED ON PATH NEIGHBORS >>
    public void SetNecessaryExitPoints()
    {
        // << SET PATH NEIGHBORS >>
        // if prev path tile not null and not in path neighbors list, add to list
        if (prevPathTile != null && !pathNeighbors.Contains(prevPathTile)) { pathNeighbors.Add(prevPathTile); }
        // if next path tile not null and not in path neighbors list, add to list
        if (nextPathTile != null && !pathNeighbors.Contains(nextPathTile)) { pathNeighbors.Add(nextPathTile); }
        // if branch node not null and not in path neighbors list, add to list
        if (branchNode != null && !pathNeighbors.Contains(branchNode)) { pathNeighbors.Add(branchNode); }

        // << GET NECCESSARY EXITS >>
        // if left neighbor not null AND left neighbor is in path neighbors
        if (tileNeighbors[0] != null && pathNeighbors.Contains(tileNeighbors[0])) { needLeftExit = true; } else { needLeftExit = false; }

        // if right neighbor not null AND right neighbor is in path neighbors
        if (tileNeighbors[1] != null && pathNeighbors.Contains(tileNeighbors[1])) { needRightExit = true; } else { needRightExit = false; }

        // if top neighbor not null AND top neighbor is in path neighbors
        if (tileNeighbors[2] != null && pathNeighbors.Contains(tileNeighbors[2])) { needTopExit = true; } else { needTopExit = false; }

        // if bottom neighbor not null AND bottom neighbor is in path neighbors
        if (tileNeighbors[3] != null && pathNeighbors.Contains(tileNeighbors[3])) { needBottomExit = true; } else { needBottomExit = false; }

        CreateExitCode();

        // print("set exit " + gridPoint.ToString() + " exit code: " + exitCode);
    }

    public void SetRandomExits()
    {
        // <<<< CREATE RANDOM EXITS BASED ON NEIGHBORS >>>>

        bool randomExitSet = false;

        // if neighbor is valid    AND   does not need exit already   AND    random value is valid                           allow exit
        if (tileNeighbors[0] != null && !needLeftExit && Random.Range((float)0, (float)1) < dunGenManager.exitRandomness) { 
            needLeftExit = true;
            tileNeighbors[0].needRightExit = true;
            randomExitSet = true; 
        }
        if (tileNeighbors[1] != null && !needRightExit && Random.Range((float)0, (float)1) < dunGenManager.exitRandomness) { 
            needRightExit = true;
            tileNeighbors[1].needLeftExit = true;
            randomExitSet = true; 
        }
        if (tileNeighbors[2] != null && !needTopExit && Random.Range((float)0, (float)1) < dunGenManager.exitRandomness) { 
            needTopExit = true;
            tileNeighbors[2].needBottomExit = true;
            randomExitSet = true; 
        }
        if (tileNeighbors[3] != null && !needBottomExit && Random.Range((float)0, (float)1) < dunGenManager.exitRandomness) { 
            needBottomExit = true;
            tileNeighbors[3].needTopExit = true;
            randomExitSet = true; 
        }

        CreateExitCode();

       // if (randomExitSet) { Debug.Log("Random Exit", gameObject); }

    }

    public void CreateExitCode()
    {
        // << CREATE EXIT CODE >>
        if (needLeftExit) { exitCode[0] = 1; }
        if (needRightExit) { exitCode[1] = 1; }
        if (needTopExit) { exitCode[2] = 1; }
        if (needBottomExit) { exitCode[3] = 1; }
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
