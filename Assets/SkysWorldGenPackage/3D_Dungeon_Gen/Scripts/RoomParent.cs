using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomParent : MonoBehaviour
{
    public List<TileGenerationManager> roomTiles = new List<TileGenerationManager>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        // update roomtiles
        foreach (TileGenerationManager tile in roomTiles)
        {
            // set all tiles in a room to "start" tile height
            if (tile.tileHeightLevel != roomTiles[0].tileHeightLevel) { tile.tileHeightLevel = roomTiles[0].tileHeightLevel; }
        }
        
    }
}
