using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * This script's goal is to randomly create a path from one cell to another
 * using weights based on each cell's distance to the end cell.
 * 
 * 1. Get the start and end cells
 * 
 * 2. Start pathfinding loop
 *      - set current "path head"
 *      - set the weights of the adjacent cells
 *      - add adjacent cells to visited list
 *      - choose best weight
 *          + if two or more cells are within a specified 
 *              range of each other, choose next head 
 *              randomly.
 * 
 * 3. Continue loop until path head is end cell. 
 * 
 */

public class RandomPathFinder : MonoBehaviour
{
    TileGenerationManager tileGenManager;
    public bool foundPath;
    public float pathRandomness = 0.4f;
    public int iterations = 0;
    public List<Cell> visitedCells = new List<Cell>();
    public List<Cell> validAdjacentHeadCells = new List<Cell>(); // i want to see the adjacent cells



    private void Awake()
    {
        tileGenManager = GetComponent<TileGenerationManager>();
    }

    public void Reset()
    {
        visitedCells.Clear();
        validAdjacentHeadCells.Clear();
        foundPath = false;
        iterations = 0;
    }


    public List<Cell> FindRandomPath(List<Cell> allCells, Cell start, Cell end, bool pathA = false, bool pathB = false)
    {
        List<Cell> path = new List<Cell>();

        // start path with start cell
        Cell pathHead = start;
        path.Add(pathHead);

        // <<<< LOOP >>>>
        while(!foundPath && iterations < allCells.Count)
        {
            validAdjacentHeadCells = FindChildrenOfPathHead(pathHead, end);
            // if path not found yet
            if (!foundPath)
            {
                pathHead = ChooseNextPathHeadBasedOnWeight(path, pathHead);
            }

            iterations++; // add to iterations
        }

        // reset all lists
        Reset();



        if (pathA) { tileGenManager.debugPathAFound = true;}
        else if (pathB) { tileGenManager.debugPathBFound = true;}

        return path;
    }

    public IEnumerator DebugFindRandomPath(List<Cell> allCells, Cell start, Cell end, float delay = 0.01f, bool debugPathA = false, bool debugPathB = false)
    {
        Debug.Log("START FIND RANDOM PATH - allCells count " + allCells.Count + " start: " + start.coord + " end:" + end.coord);

        List<Cell> path = new List<Cell>();

        // start path with start cell
        Cell pathHead = start;
        path.Add(pathHead);

        // <<<< LOOP >>>>
        while (!foundPath && iterations < allCells.Count)
        {
            // << PART 1 >>
            validAdjacentHeadCells = FindChildrenOfPathHead(pathHead, end);
            //Debug.Log("children of " + pathHead + " -> " + validAdjacentHeadCells[0]);

            yield return new WaitForSeconds(delay);

            // if path not found yet
            if (!foundPath)
            {
                // << PART 2 >>
                pathHead = ChooseNextPathHeadBasedOnWeight(path, pathHead);
                //Debug.Log("new path head : " + pathHead);

                yield return new WaitForSeconds(delay);
            }

            iterations++; // add to iterations
        }

        if (foundPath) { Debug.Log("Found Path from " + start.coord + " to " + end.coord + " in " + iterations + " iterations"); }
        else { Debug.LogError("FAILED Path from " + start.coord + " to " + end.coord + " in " + iterations + " iterations"); }

        // reset all lists
        Reset();

        if (debugPathA) { tileGenManager.pathA = path; tileGenManager.debugPathAFound = true; }
        else if (debugPathB) { tileGenManager.pathB = path; tileGenManager.debugPathBFound = true; }

    }




    // =========================================== PATH FINDER STEPS ==============================================


    // <<<< STEP 1 - FIND CHILDREN OF PATH HEAD >>>>
    private List<Cell> FindChildrenOfPathHead(Cell pathHead, Cell end)
    {
        // <<<< GET VALID ADJACENT CELLS >>>>
        // find adjacent cells to path head
        List<Cell> invalidAdjacentCells = new List<Cell>();
        List<Cell> adjacentCellsOut = GetAdjacentCells(pathHead);
        foreach (Cell cell in adjacentCellsOut)
        {
            if (cell == end)
            {
                foundPath = true;
                break;
            } // found exit


            // if adjacent cell not in visited cells, add AND check validity of cell (is it empty?)
            if (cell.cellType != 0)
            {
                // if cell isn't already visited
                if (!visitedCells.Contains(cell))
                {
                    visitedCells.Add(cell);
                }

                cell.pathWeight = Vector3.Distance(cell.transform.position, end.transform.position);

                // if cell is not already part of the path
                if (cell.cellType != 2)
                {
                    cell.cellType = -2; // sets gizmos to green to visualize that this cell has been visited
                }
            }
            else
            {
                invalidAdjacentCells.Add(cell); // add to invalid cells to remove later
            }
        }

        // remove invalid cells
        foreach (Cell cell in invalidAdjacentCells)
        {
            if (adjacentCellsOut.Contains(cell))
            {
                adjacentCellsOut.Remove(cell);
            }
        }
        invalidAdjacentCells.Clear();

        return adjacentCellsOut;
    }

    // <<<< STEP 2 - CHOOSE NEXT HEAD BASED ON WEIGHT >>>>
    private Cell ChooseNextPathHeadBasedOnWeight(List<Cell> path, Cell pathHead)
    {
        // find best weight OR lottery pick <3
        Cell bestCell = null;
        foreach (Cell cell in validAdjacentHeadCells)
        {
            // SPIN THE WHEEL , COME RIGHT UP AND GIVE IT YOUR ALL
            // randomly decide if the "best cell" will be random
            if (Random.Range((float)0, (float)1) < pathRandomness)
            {
                //choose random cell
                bestCell = validAdjacentHeadCells[Random.Range(0, validAdjacentHeadCells.Count - 1)];
            }
            // choose cell with best weight
            else if (bestCell == null || cell.pathWeight < bestCell.pathWeight)
            {
                bestCell = cell;
            }
        }

        // set best cell to path head if not already part of path, otherwise try again
        if (!path.Contains(bestCell))
        {
            path.Add(bestCell);
        }

        bestCell.cellType = 2; // set to a part of the path
        pathHead = bestCell;

        return pathHead;
        
    }





    // =========================================== HELPER FUNCTIONS =============================================
    public void PrintGrid(List<List<int>> grid)
    {
        // print to console for testing
        string test_out = "\n";
        for (int i = 0; i < grid.Count; i++)
        {
            //test_out += i.ToString() + ": ";

            for (int j = 0; j < grid[i].Count; j++)
            {
                if (grid[i][j] == -1)
                {
                    test_out += " _ ";
                }
                else
                {
                    test_out += " " + grid[i][j];
                }

            }

            test_out += "\n";

        }

        Debug.Log(test_out);
    }


    private List<Cell> GetAdjacentCells(Cell n)
    {
        List<Cell> temp = new List<Cell>();

        int row = (int)n.coord.x;
        int col = (int)n.coord.y;


        if (row + 1 <= tileGenManager.cellCoordMax)
        {
            temp.Add(tileGenManager.GetCell(new Vector2(row + 1, col)));
        }
        if (row - 1 >= 0)
        {
            temp.Add(tileGenManager.GetCell(new Vector2(row - 1, col)));
        }
        if (col - 1 >= 0)
        {
            temp.Add(tileGenManager.GetCell(new Vector2(row, col - 1)));
        }
        if (col + 1 <= tileGenManager.cellCoordMax)
        {
            temp.Add(tileGenManager.GetCell(new Vector2(row, col + 1)));
        }

        return temp;
    }
}
