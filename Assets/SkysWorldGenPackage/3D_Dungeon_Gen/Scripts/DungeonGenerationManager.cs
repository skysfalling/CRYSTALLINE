using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* THIS SCRIPT IS CREATED FOR 3D DUNGEON GENERATION
 *
 *  We start with an 'initial start tile' with a spawn in entrance for our player
 *
 *  From that start tile, the algortihm will randomly draw a <<MAIN PATH>> of n tile count,
 *  consisting of a total 'beginning' and 'end' of the single dungeon.
 *
 *  From the <<MAIN PATH>> , the algorithm will randomly select tiles to create a **BRANCH** from,
 *  of m tile count.
 *
 *  During all of this, the algorithm will make sure to log where each tile is placed and
 *  what exits are necessary.
 *
 *  After the <<MAIN PATH>> and its **BRANCHES** are created, the tiles will spawn with
 *  randomly spawned obstacles, items and enemies.
 *
 * */



public class DungeonGenerationManager : MonoBehaviour
{

    GameObject newTile;
    public List<TileGenerationManager> allTiles = new List<TileGenerationManager>();

    [HideInInspector]
    public NPC_Manager npc_manager;
    [HideInInspector]
    public Environment_Manager env_manager;


    public bool debug;
    public bool initOnSceneStart;
    public LineRenderer outlineLR;
    public LineRenderer mainPathLR;
    public Material mainPath_material;
    public Material branchPath_material;


    [Header("Generation Parameters")]
    public bool generationStarted;
    public bool generationFinished;
    [HideInInspector]
    public TileGenerationManager dungeonStartTile;
    [HideInInspector]
    public TileGenerationManager dungeonEndTile;
    public Vector2 startTileCoord = Vector2.zero;
    public Vector2 endTileCoord = new Vector2(5, 0);
    public Vector2 generationBoundaries = new Vector2(10,10); //max +/- (horz/vert) coordinate

    [Header("Layers")]
    public int groundLayer;

    [Header("Tile Parameters")]
    public int individualTileSize = 50;
    public int cellsPerTile = 10;
    public int cellSize;
    public GameObject tileCellParentPrefab;
    public GameObject tilePrefab;

    [Header("Paths")]
    public bool mainPathGenFinished;
    public int maxMainPathLength = 20;
    public List<TileGenerationManager> mainPath = new List<TileGenerationManager>();

    [Header("Branches")]
    public int maxBranchLength = 5;
    public List<List<TileGenerationManager>> branches = new List<List<TileGenerationManager>>(); // all branches

    [Header("Generation")]
    [Range(0,1)]
    public float obstacleSpawnWeight = 0.2f;

    [Header("Walls")]
    public Material wallMaterial;
    public int minWallHeight = 2;

    [Header("Tiles")]
    public int maxTileHeight = 7;
    public int minTileHeight = -3;

    [Space(10)]
    public int cur_tallestTileHeight;
    public int cur_lowestTileHeight;

    [Header("Exits")]
    [Range(0, 1)]
    public float exitRandomness = 0.4f;
    public int maxExitSize = 3;

    [Header("Rooms")]
    [Range(0,1)]
    public float roomSpawnWeight = 0.5f;
    public int roomCount = 0;
    public List<TileGenerationManager> roomStartTiles = new List<TileGenerationManager>();

    [Header("Height Adjustment")]
    [Range(0,1)]
    public float heightAdjustChance = 0.1f;
    [Range(-1, 1)]
    public float vertHeightFavor = 0.2f; // neg goes down, pos goes up


    private void Awake()
    {
        groundLayer = LayerMask.NameToLayer("Ground");
        npc_manager = GetComponent<NPC_Manager>();
        env_manager = GetComponent<Environment_Manager>();
    }

    private void Start()
    {
        //if (initOnSceneStart) { StartCoroutine(Generate(startTileCoord)); }

        // set individual cell size
        cellSize = (individualTileSize / cellsPerTile);

        Debug.Log("Cell Size :: " + cellSize);
    }

    private void Update()
    {
        DebugVisualizations(debug);
    }

    public void ClearGeneration()
    {
        foreach (TileGenerationManager tile in allTiles)
        {
            if (tile != null)
            {
                tile.DestroyAllCellParents();
                Destroy(tile.gameObject);
            }
        }

        allTiles.Clear();
        branches.Clear();
        mainPath.Clear();

        generationStarted = false;
        generationFinished = false;
    }

    // ======================================= DUNGEON GENERATION =================================================

    public IEnumerator Generate(Vector2 startTileCoord)
    {
        generationStarted = true;

        // << CREATE MAIN PATH >>
        dungeonStartTile = CreateNewTile(startTileCoord, transform, mainPath);
        dungeonStartTile.pathBeginning = true;
        dungeonStartTile.dungeonBeginning = true;
        dungeonStartTile.tileHeightLevel = 0;

        dungeonEndTile = CreateNewTile(endTileCoord, transform);
        dungeonEndTile.dungeonEnd = true;


        //DiscoverPath(dungeonStartTile, mainPath, maxMainPathLength, transform, true, -1);

        DiscoverPathBetweenPoints(dungeonStartTile, dungeonEndTile, mainPath, transform, true);

        if (mainPath.Count <= 1) { print("Main Path too small"); yield return null; } // check if path is abnormally small

        yield return new WaitUntil(() => mainPathGenFinished);

        // << RANDOMIZE MAIN PATH HEIGHT >>
        RandomizePathHeight(mainPath);

        // << CREATE ROOMS >>
        foreach (TileGenerationManager tile in roomStartTiles)
        {
            CreateRoomFromTile(tile);
        }

        /*
        // << CREATE MAIN PATH BRANCHES >>
        // get branch nodes
        List<TileGenerationManager> mainPathBranchNodes = PathCreateBranchNodes(mainPath);

        // from nodes , create new paths
        foreach (TileGenerationManager node in mainPathBranchNodes)
        {
            branches.Add(CreateBranchFromNode(node)); //create new branch
        }
        */

        // for each branch path, randomize height
        foreach (List<TileGenerationManager> branch in branches)
        {
            RandomizePathHeight(branch);
        }
        
        // << SET EXITS >>
        // create necessary exits in all tiles
        foreach (TileGenerationManager tile in allTiles)
        {
            tile.SetNecessaryExitPoints();
        }

        yield return new WaitForSeconds(0.5f);

        // << GENERATE 3D MODELS FOR EACH TILE >>
        StartCoroutine(StaggeredTileInit());

        yield return new WaitUntil(() => generationFinished);

        //set end tile
        mainPath[mainPath.Count - 1].SetMainPathEnd();

    }


    #region RANDOM PATH GENERATION ======================================
    // << CREATE PATH >> ** this recursively creates a randomized initial path from an initial start to a random end
    // << DISCOVER PATH FROM ORIGIN TILE >>
    public void DiscoverRandomPath(TileGenerationManager originTile, List<TileGenerationManager> path, int pathLength, Transform parent, bool mainPath = false, int horzFavor = 1)
    {
        //print("Start Discover Path");
        Vector2 spawnDirection = GetRandomSpawnDirection(originTile, horzFavor);

        // ** IF VALID SPAWN DIRECTION **
        if (spawnDirection != Vector2.zero)
        {
            TileGenerationManager newTile = CreateNewTile(originTile.coord + spawnDirection, parent, path); ;

            // << ROOM SPAWN CHANCE >>
            if (Random.Range((float)0, (float)1) < roomSpawnWeight)
            {
                newTile.roomTile = true;
                roomStartTiles.Add(newTile);
            }

            // set path neighbors
            newTile.prevPathTile = originTile;
            originTile.nextPathTile = newTile;

            // set height
            newTile.tileHeightLevel = originTile.tileHeightLevel;

            // ** CONTINUE RECURSION ** if path count is less than max
            if (path.Count < pathLength)
            {
                // if hasn't reached max path length, continue
                DiscoverRandomPath(newTile, path, pathLength, parent, mainPath, horzFavor);
            }
            else if (mainPath)
            {
                mainPathGenFinished = true;
            }
        }
        else if (mainPath)
        {
            mainPathGenFinished = true;
        }

        //Debug.Log(originTile.tileCoord + "spawn direction: " + spawnDirection);
    }

    // << GET RANDOM VALID SPAWN DIRECTION >>
    public Vector2 GetRandomSpawnDirection(TileGenerationManager originTile, int horzFavor = 1)
    {
        // << GET POSSIBLE DIRECTIONS FROM ORIGIN TILE >>
        string debugOptions = originTile.coord.ToString() + "MOVE OPTIONS: ";

        // [[ HORIZONTAL DIRECTIONS ]]
        // if neighbor not present in that direction... add to possible directions
        List<Vector2> horzSpawnDirections = new List<Vector2>();
        if (originTile.GetTileNeighbor(Vector2.left) == null && !TileOutOfBounds(originTile.coord + Vector2.left))
        { horzSpawnDirections.Add(Vector2.left); debugOptions += "left "; }

        if (originTile.GetTileNeighbor(Vector2.right) && !TileOutOfBounds(originTile.coord + Vector2.right))
        { horzSpawnDirections.Add(Vector2.right); debugOptions += "right "; }

        // [[ VERTICAL DIRECTIONS ]]
        // if neighbor not present in that direction... add to possible directions
        List<Vector2> vertSpawnDirections = new List<Vector2>();
        if (originTile.GetTileNeighbor(Vector2.up) && !TileOutOfBounds(originTile.coord + Vector2.up))
        { vertSpawnDirections.Add(Vector2.up); debugOptions += "up "; }

        if (originTile.GetTileNeighbor(Vector2.down) && !TileOutOfBounds(originTile.coord + Vector2.down))
        { vertSpawnDirections.Add(Vector2.down); debugOptions += "down "; }

        //print(debugOptions); //print move options

        // << RANDOM CHOICE >>
        Vector2 spawnDirection = Vector2.zero;
        float random = Random.Range(1, 10);
        //print("random: " + random);

        // spawn in random horizontal direction
        if (random <= (5 + horzFavor) && horzSpawnDirections.Count != 0) { spawnDirection = horzSpawnDirections[Random.Range(0, horzSpawnDirections.Count)]; }

        // spawn in random vertical direction
        else if (vertSpawnDirections.Count != 0) { spawnDirection = vertSpawnDirections[Random.Range(0, vertSpawnDirections.Count)]; }

        // if no vertical direction ... check if any horizontal direction
        else if (vertSpawnDirections.Count == 0 && horzSpawnDirections.Count != 0) { spawnDirection = horzSpawnDirections[Random.Range(0, horzSpawnDirections.Count)]; }

        // error checking
        //else { Debug.LogWarning("No Possible New Spawn Directions at " + originTile.tileCoord.ToString()); }

        return spawnDirection;
    }
    #endregion

    #region END POINT PATH GENERATION =============================
    // << DISCOVER PATH BETWEEN TWO END TILES >>
    public void DiscoverPathBetweenPoints(TileGenerationManager currentTile, TileGenerationManager endTile, List<TileGenerationManager> path, Transform parent, bool mainPath = false, int horzFavor = 1)
    {
        //print("Start Discover Path");
        Vector2 spawnDirection = GetClosestSpawnDirection(currentTile, endTile);

        // ** IF NOT END **
        if (currentTile.coord + spawnDirection != endTile.coord)
        {
            TileGenerationManager newTile = CreateNewTile(currentTile.coord + spawnDirection, parent, path);

            /*
            // << ROOM SPAWN CHANCE >>
            if (Random.Range((float)0, (float)1) < roomSpawnWeight)
            {
                newTile.roomTile = true;
                roomStartTiles.Add(newTile);
            }
            */

            // set path neighbors
            newTile.prevPathTile = currentTile;
            currentTile.nextPathTile = newTile;

            // set height
            newTile.tileHeightLevel = currentTile.tileHeightLevel;

            // ** CONTINUE RECURSION **

            // break infinite loop
            if (path.Count > (generationBoundaries.x * generationBoundaries.y)) { Debug.LogError("Infinite Loop Generation"); return; }

            // continue recursion
            DiscoverPathBetweenPoints(newTile, endTile, path, parent, mainPath, horzFavor);
            

            mainPathGenFinished = true;  
        }

        // ** IF END IS FOUND **
        else if (mainPath)
        {
            mainPathGenFinished = true;
        }

    }



    // << GET RANDOM VALID SPAWN DIRECTION >>
    public Vector2 GetClosestSpawnDirection(TileGenerationManager currentTile, TileGenerationManager endTile)
    {
        // << GET POSSIBLE DIRECTIONS FROM ORIGIN TILE >>

        // [[ ALL DIRECTIONS ]]
        // if neighbor not present in that direction... add to possible directions
        List<Vector2> spawnDirections = new List<Vector2>();

        if (currentTile.GetTileNeighbor(Vector2.left) == null && !TileOutOfBounds(currentTile.coord + Vector2.left))
        { spawnDirections.Add(Vector2.left);}

        if (currentTile.GetTileNeighbor(Vector2.right) == null && !TileOutOfBounds(currentTile.coord + Vector2.right))
        { spawnDirections.Add(Vector2.right);}

        if (currentTile.GetTileNeighbor(Vector2.up) == null && !TileOutOfBounds(currentTile.coord + Vector2.up))
        { spawnDirections.Add(Vector2.up);}

        if (currentTile.GetTileNeighbor(Vector2.down) == null && !TileOutOfBounds(currentTile.coord + Vector2.down))
        { spawnDirections.Add(Vector2.down);}


        // [[ CHECK WHICH NEIGHBOR IS CLOSEST TO EXIT ]]

        Vector2 closestToEnd = Vector2.zero;
        float smallestDistance = -1;

        foreach (Vector2 direction in spawnDirections)
        {
            // if direction is end, return end
            if (currentTile.coord + direction == endTile.coord)
            {
                Debug.Log("Found End Tile :: " + currentTile.coord + direction);
                return direction;
            }
            // else if direction not end
            else
            {
                // if default, set first value to smallest
                if (smallestDistance == -1) { closestToEnd = direction; smallestDistance = Vector2.Distance(direction, endTile.coord); }

                // else if the current coord is closer to end, 
                else if (Vector2.Distance(direction, endTile.coord) < smallestDistance) { closestToEnd = direction; smallestDistance = Vector2.Distance(direction, endTile.coord); }
            }
        }
        
        return closestToEnd;
    }

    #endregion



    #region BRANCHES =================================================

    // << CREATE RANDOM BRANCH NODES >> ** loops through given path and creates a branch from it
    public List<TileGenerationManager> PathCreateBranchNodes(List<TileGenerationManager> path, int spawnWeight = 3)
    {
        List<TileGenerationManager> branchNodes = new List<TileGenerationManager>();

        int tileIndex = 0; // init to check if first tile

        // for each tile in given path...
        foreach (TileGenerationManager tile in path)
        {
            // continue without branch node chance if entrance or exit of path
            if (tileIndex == 0 || tileIndex == path.Count - 1)
            {
                tileIndex++;
                continue;
            }

            List<Vector2> emptyNeighbors = tile.GetEmptyNeighbors(); // get tile empty neighbors
            int random = Random.Range(1, 10); // get random int

            // if cur tile has empty neighbors and random value is within weight...
            if (emptyNeighbors.Count > 0 && random <= spawnWeight)
            {
                // create branch
                Vector2 branchDir = emptyNeighbors[Random.Range(0, emptyNeighbors.Count - 1)]; // choose random valid direction

                TileGenerationManager newNode = CreateNewTile(branchDir, tile.transform); // create branch node
                newNode.inBranch = true; // set in branch
                newNode.prevPathTile = tile; // set prev path tile
                tile.branchNode = newNode; // set branch node of tile
                newNode.name = "branch node " + branchNodes.Count + ": " + newNode.coord.ToString();
                newNode.tileHeightLevel = tile.tileHeightLevel; // set to parent height level

                branchNodes.Add(newNode);

                string debug_out = "Branch created at " + branchDir + " neighbors: ";
                foreach (Vector2 point in emptyNeighbors) { debug_out += point.ToString() + " "; }
                //print(debug_out);
            }

            tileIndex++; // add to index counter
        }

        return branchNodes;
    }

    // << CREATE BRANCH FROM NODE >> ** Discovers path from start branch node
    public List<TileGenerationManager> CreateBranchFromNode(TileGenerationManager branchNode)
    {
        List<TileGenerationManager> branch = new List<TileGenerationManager> { branchNode }; // create branch list
        DiscoverRandomPath(branchNode, branch, maxBranchLength, branchNode.transform); // discover path from node

        // set each tile to inBranch
        foreach (TileGenerationManager tile in branch)
        {
            tile.inBranch = true;
        }

        return branch;
    }
    #endregion

    // ----------------------------------------- ROOM GENERATION -----------------------------------------------------
    // take the "start" of room and set a certain pattern of tiles around tile as a room tile
    public void CreateRoomFromTile(TileGenerationManager roomStart)
    {
        // << CREATE ROOM PARENT >>
        roomCount++; // up room count
        GameObject roomParent = new GameObject("Room Parent " + roomCount.ToString()); // create parent
        roomParent.AddComponent<RoomParent>();
        roomParent.transform.position = Vector3.zero; // set pos to zero
        roomParent.transform.parent = transform; // set parent to dun gen transform

        roomStart.transform.parent = roomParent.transform; // set room start parent to roomParent
        roomParent.GetComponent<RoomParent>().roomTiles.Add(roomStart);

        // << DECIDE WHICH DIRECTION TO START ROOM>>
        // ** validate neighbors
        List<Vector2> horzRoomStartNeighbors = new List<Vector2>();
        if (!TileOutOfBounds(roomStart.coord + Vector2.right)) { horzRoomStartNeighbors.Add(Vector2.right); }
        if (!TileOutOfBounds(roomStart.coord + Vector2.left)) { horzRoomStartNeighbors.Add(Vector2.left); }

        List<Vector2> vertRoomStartNeighbors = new List<Vector2>();
        if (!TileOutOfBounds(roomStart.coord + Vector2.up)) { vertRoomStartNeighbors.Add(Vector2.up); }
        if (!TileOutOfBounds(roomStart.coord + Vector2.down)) { vertRoomStartNeighbors.Add(Vector2.down); }

        // ** choose random room direction from choices
        Vector2 horzRoomDirection = new Vector2(-2, -2); // null value
        if (horzRoomStartNeighbors.Count > 0)
        {
            horzRoomDirection = horzRoomStartNeighbors[Random.Range(0, horzRoomStartNeighbors.Count)]; // random direction
        }

        Vector2 vertRoomDirection = new Vector2(-2, -2); // null value
        if (vertRoomStartNeighbors.Count > 0)
        {
            vertRoomDirection = vertRoomStartNeighbors[Random.Range(0, vertRoomStartNeighbors.Count)]; // random direction
        }

        // << DECIDE ROOM TYPE >>
        CreateRoomType(horzRoomDirection, vertRoomDirection, roomStart.coord, roomParent);

    }

    public void SetTileToRoomTile(Vector2 coord, GameObject parent)
    {
        TileGenerationManager tile = GetTile(coord);
        if (tile == null) { CreateNewTile(coord, parent.transform, null, true); }
        else { 
            tile.roomTile = true;
            tile.transform.parent = parent.transform;
            parent.GetComponent<RoomParent>().roomTiles.Add(tile);
        }
    }

    public void CreateRoomType(Vector2 horzDir, Vector2 vertDir, Vector2 roomStartCoord, GameObject roomParent)
    {
        int roomType = Random.Range(0, 1);

        // if directions are not null
        if (horzDir != new Vector2(-2, -2) && vertDir != new Vector2(-2, -2))
        {
            switch (roomType)
            {
                case 0:
                    // square room
                    SetTileToRoomTile(roomStartCoord + horzDir, roomParent);
                    SetTileToRoomTile(roomStartCoord + vertDir, roomParent);
                    SetTileToRoomTile(roomStartCoord + horzDir + vertDir, roomParent); // diag corner
                    break;
            }


            // line shape (horz or vert)

            // L shape

            // + shape
        }




    }


    // ----------------------------------------- HEIGHT GENERATION -----------------------------------------------------
    public void RandomizePathHeight(List<TileGenerationManager> path)
    {
        int curHeightLevel = path[0].tileHeightLevel;
        foreach(TileGenerationManager tile in path)
        {
            // if not start tile ...
            if (tile != path[0])
            {
                tile.tileHeightLevel = curHeightLevel;

                // set cur tallest / lowest heights
                if (curHeightLevel > cur_tallestTileHeight) { cur_tallestTileHeight = curHeightLevel; }
                if (curHeightLevel < cur_lowestTileHeight) { cur_lowestTileHeight = curHeightLevel; }


                // randomly choose if height changes , if not last tile
                if (Random.Range((float)0, (float)1) < heightAdjustChance && tile != path[path.Count - 1])
                {
                    tile.transitionTile = true;

                    // randomly choose which direction
                    float random = Random.Range((float)-1, (float)1) + vertHeightFavor;
                    if (curHeightLevel > minTileHeight && random <= 0) { curHeightLevel--; tile.transitionDirection = -1; } // height decrease
                    else if (curHeightLevel < maxTileHeight && random > 0 ) { curHeightLevel++; tile.transitionDirection = 1; } // height increase
                }
            }
        }
    }

    // ======================================== TILE MANAGEMENT ========================================================
    // << CREATE NEW TILE >> ** creates a new tile and adds to allTiles list
    public TileGenerationManager CreateNewTile(Vector2 tileCoord, Transform parent, List<TileGenerationManager> path = null, bool roomTile = false)
    {
        // << CREATE NEW TILE >>
        // create tile gameobject
        newTile = Instantiate(tilePrefab, Vector3.zero, Quaternion.identity);
        newTile.transform.position = transform.position + new Vector3(tileCoord.x * individualTileSize, 0, tileCoord.y * individualTileSize);

        // create tile gen script
        TileGenerationManager newTileGen = newTile.GetComponent<TileGenerationManager>();
        newTileGen.tilePosition = newTile.transform.position;
        newTileGen.coord = tileCoord;
        newTileGen.fullTileSize = individualTileSize;
        newTileGen.cellsPerTile = cellsPerTile;
        newTileGen.cellPrefab = tileCellParentPrefab;
        newTileGen.dunGenManager = gameObject.GetComponent<DungeonGenerationManager>();

        // set transform values
        newTile.transform.parent = parent;
        newTile.name = "Tile " + tileCoord.ToString();

        // set neighbors of new tile
        SetTileNeighbors(newTileGen);

        // add tile to path
        if (path != null)
        {
            if (path.Count == 0) { newTileGen.pathBeginning = true; } // set beginning tile
            path.Add(newTileGen);
        }

        // add tile to overall tile list
        allTiles.Add(newTileGen);

        // set room tile
        if (roomTile) 
        { 
            newTileGen.roomTile = true;
            parent.GetComponent<RoomParent>().roomTiles.Add(newTileGen);
        }

        return newTileGen;
    }

    public TileGenerationManager GetTile(Vector2 tileCoord)
    {
        foreach (TileGenerationManager tile in allTiles)
        {
            if (tile.coord == tileCoord) { return tile; }
        }

        return null;
    }

    // set neighbors of a tile
    public void SetTileNeighbors(TileGenerationManager tile)
    {
        Vector2 tilePoint = tile.coord;

        // reset neighbors
        tile.tileNeighbors.Clear();
        tile.tileNeighbors.Add(null);
        tile.tileNeighbors.Add(null);
        tile.tileNeighbors.Add(null);
        tile.tileNeighbors.Add(null);

        // check if dir is in allTiles
        foreach (TileGenerationManager checkTile in allTiles)
        {

            // << LEFT NEIGHBOR >>
            if (checkTile.coord == tile.coord + Vector2.left)
            {
                tile.tileNeighbors[0] = checkTile; // this tile's left neighbor is found tile "t"
                checkTile.tileNeighbors[1] = tile; // found tile t's right neighbor is then this tile
            }

            // << RIGHT NEIGHBOR >>
            else if (checkTile.coord == tile.coord + Vector2.right)
            {
                tile.tileNeighbors[1] = checkTile;
                checkTile.tileNeighbors[0] = tile;
            }

            // << TOP NEIGHBOR >>
            else if (checkTile.coord == tile.coord + Vector2.up)
            {
                tile.tileNeighbors[2] = checkTile;
                checkTile.tileNeighbors[3] = tile;
            }

            // << BOTTOM NEIGHBOR >>
            else if (checkTile.coord == tile.coord + Vector2.down)
            {
                tile.tileNeighbors[3] = checkTile;
                checkTile.tileNeighbors[2] = tile;
            }

        }
    }

    // check if tile is out of bounds
    public bool TileOutOfBounds(Vector2 tileCoord)
    {
        if (Mathf.Abs(tileCoord.x) >= generationBoundaries.x ||
            Mathf.Abs(tileCoord.y) >= generationBoundaries.y)
        {
            return true;
        }

        return false;
    }

    // initialize each tile one at a time
    public IEnumerator StaggeredTileInit()
    {
        // << GENERATE 3D MODELS FOR EACH TILE >>
        for (int i = 0; i < allTiles.Count; i++)
        {
            if (allTiles[i] != null){
                allTiles[i].TileInit();
                yield return new WaitUntil(() => allTiles[i].tileInitialized);
            }
        }

        generationFinished = true;
        //yield return null;
    }

    public bool runOnce = false;
    public List<GameObject> branchObjs;
    public void DebugVisualizations(bool enabled)
    {
        // turn main path on / off
        mainPathLR.gameObject.SetActive(enabled);
        if (!enabled) { runOnce = false; return; } // if not off, continue
        runOnce = true;

        float pathVertOffset = 10; // height offset of line
        
        // <<<< VISUALIZE MAIN PATH >>>>
        List<Vector3> mainPathDebugPoints = new List<Vector3>();
        foreach (TileGenerationManager tile in mainPath)
        {
            mainPathDebugPoints.Add(tile.transform.position + new Vector3(0, pathVertOffset, 0));
        }

        mainPathLR.positionCount = mainPathDebugPoints.Count;
        mainPathLR.SetPositions(mainPathDebugPoints.ToArray());


        /*

        // << CREATE LR FOR BRANCH PATH >>
        foreach (List<TileGenerationManager> branch in branches)
        {

            GameObject newBranchObject = new GameObject();
            newBranchObject.transform.parent = mainPathLR.transform;
            branchObjs.Add(newBranchObject);
            LineRenderer newBranchLR = newBranchObject.AddComponent<LineRenderer>();
            newBranchLR.material = branchPath_material;

            List<Vector3> points = new List<Vector3>();

            foreach (TileGenerationManager tile in branch)
            {
                points.Add(tile.transform.position + new Vector3(0, pathVertOffset, 0));
            }

            newBranchLR.SetPositions(points.ToArray());

        }
        */


    }

}
