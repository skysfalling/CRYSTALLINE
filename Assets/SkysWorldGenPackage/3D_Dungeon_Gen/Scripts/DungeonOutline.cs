using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DungeonOutline : MonoBehaviour
{
    DungeonGenerationManager dunGenManager;
    LineRenderer lr;

    private void Start()
    {
        dunGenManager = GetComponentInParent<DungeonGenerationManager>();
        lr = GetComponent<LineRenderer>();

    }

    // Update is called once per frame
    void Update()
    {
        int individualTileSize = dunGenManager.individualTileSize;
        Vector2 generationBoundaries = dunGenManager.generationBoundaries;

        // <<<< GENERATION SPACE OUTLINE >>>>
        List<Vector3> outlinePoints = new List<Vector3>();

        // << GET VERTEX POINTS OF GROUND >>
        float vertOffset = -0.25f;
        Vector3 topLeftVertex = new Vector3(-(individualTileSize * generationBoundaries.x) + transform.position.x,
            vertOffset + transform.position.y,
            ((individualTileSize * generationBoundaries.y)) + transform.position.z);
        Vector3 botLeftVertex = new Vector3(-(individualTileSize * generationBoundaries.x) + transform.position.x,
            vertOffset + transform.position.y,
            (-(individualTileSize * generationBoundaries.y)) + transform.position.z);
        Vector3 topRightVertex = new Vector3((individualTileSize * generationBoundaries.x) + transform.position.x,
            vertOffset + transform.position.y,
            ((individualTileSize * generationBoundaries.y)) + transform.position.z);
        Vector3 botRightVertex = new Vector3((individualTileSize * generationBoundaries.x) + transform.position.x,
            vertOffset + transform.position.y,
            (-(individualTileSize * generationBoundaries.y)) + transform.position.z);


        // overall outline
        outlinePoints.Add(topLeftVertex);
        outlinePoints.Add(botLeftVertex);
        outlinePoints.Add(botRightVertex);
        outlinePoints.Add(topRightVertex);
        outlinePoints.Add(topLeftVertex);


        if (!dunGenManager.generationStarted)
        {
            // tile size demo
            outlinePoints.Add(topLeftVertex + new Vector3(individualTileSize, 0, 0));
            outlinePoints.Add(topLeftVertex + new Vector3(individualTileSize, 0, -individualTileSize));
            outlinePoints.Add(topLeftVertex + new Vector3(0, 0, -individualTileSize));
        }

        lr.positionCount = outlinePoints.Count;
        lr.SetPositions(outlinePoints.ToArray());

    }
}
