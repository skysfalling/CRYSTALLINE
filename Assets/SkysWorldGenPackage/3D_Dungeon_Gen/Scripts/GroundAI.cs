using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class GroundAI : MonoBehaviour
{
    public enum State { Roam, Idle };

    public TileGenerationManager curTile;
    Rigidbody rb;

    [Header("Debug")]
    public bool debug;
    public Material db_targetNormalMat;


    public Transform roamTarget;
    public LayerMask npcLayer;
    public LayerMask groundLayer;

    public Transform player;


    public float speed = 3;
    float speedValue; // actual speed used
    //public float runFromPlayerRadius = 5;
    //public float safeRadius = 7;
    public float findTargetRadius = 20;

    public float distFromTarget;



    [Header("Data")]
    public List<Cell> cellsInRange = new List<Cell>();


    [Header("States")]
    public State curState = State.Idle;

    [Header("Roam")]
    public float minRoamDelay = 1;
    public float maxRoamDelay = 3;

    [Header("Idle")]
    public float minIdleDelay = 1;
    public float maxIdleDelay = 3;

    // Start is called before the first frame update
    void Start()
    {
        //player = GameObject.FindGameObjectWithTag("Player").transform;
        rb = GetComponent<Rigidbody>();

        speedValue = speed;

        StartCoroutine(StateUpdate());
        StartCoroutine(Roam());
    }

    // Update is called once per frame
    void Update()
    {

        // <<<< MOVE AI TOWARD TARGET >>>>
        if (roamTarget != null)
        {
            // get distance
            distFromTarget = Vector3.Distance(transform.position, roamTarget.position);

            // get speed and move toward target
            speed = speedValue;
            rb.MovePosition(Vector3.Lerp(transform.position, new Vector3(roamTarget.position.x, transform.position.y, roamTarget.position.z), speed * Time.deltaTime));

            // look at move direction
            transform.LookAt(Vector3.Lerp(transform.position, new Vector3(roamTarget.position.x, transform.position.y, roamTarget.position.z), speed * Time.deltaTime));
        
        }

        // get cur tile 
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out hit, Mathf.Infinity))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                // if ai is on new tile
                if (hit.collider.GetComponentInParent<TileGenerationManager>() != curTile)
                {
                    curTile = hit.collider.GetComponentInParent<TileGenerationManager>();
                    roamTarget = null; // cancel out target on old tile
                }
            }

            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.down) * 10, Color.yellow);
        }

    }


    // constant state update
    public IEnumerator StateUpdate()
    {
        while (true)
        {
            // get cells in range
            GetCellsInRange(findTargetRadius);

            // IDLE -> ROAM
            // is idle and close to target
            if (curState == State.Idle)
            {
                yield return new WaitForSeconds(Random.Range(minIdleDelay, maxIdleDelay)); // random wait
                curState = State.Roam;
            }

            // ROAM -> IDLE
            else if (curState == State.Roam && distFromTarget < 1)
            {
                curState = State.Idle;
            }


            yield return new WaitForSeconds(0.1f); // slow down while loop 
        }
    }

    public IEnumerator Roam()
    {
        // always run
        while (true)
        {
            // only change target if roam is true
            if (curState == State.Roam)
            {
                if (cellsInRange.Count > 1)
                {
                    roamTarget = cellsInRange[Random.Range(0, cellsInRange.Count)].transform;
                }
                yield return new WaitForSeconds(Random.Range(minRoamDelay, maxRoamDelay));
            }

            yield return new WaitForSeconds(0.1f); // slow down while loop 
        }
    }

    public void GetCellsInRange(float range)
    {
        cellsInRange.Clear(); // reset list

        // foreach all   cell in tile
        foreach (Cell cell in curTile.allCells) { 
            // if cell is in range and not in list already
            if (Vector3.Distance(transform.position, cell.transform.position) <= range && !cellsInRange.Contains(cell))
            {
                cellsInRange.Add(cell);
                if (debug)
                {
                    cell.SetDebugCube(true);
                }
            }

            // if cell is not in range and list contains cell
            else if (Vector3.Distance(transform.position, cell.transform.position) > range)
            {
                if (cellsInRange.Contains(cell))
                {
                    cellsInRange.Remove(cell);
                }

                if (debug)
                {
                    cell.SetDebugCube(false);
                }
            }
        }

        foreach (Cell cell in cellsInRange)
        {
            cell.TempChangeDebugMat(db_targetNormalMat);
        }
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, findTargetRadius);
    }
}
