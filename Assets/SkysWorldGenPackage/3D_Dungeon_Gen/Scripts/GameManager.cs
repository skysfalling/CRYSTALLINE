using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public DungeonGenerationManager dunGenManager;
    public bool initGameStart;

    [Header("First Person")]
    public GameObject fp_player;
    public GameObject cameraHolder;
    PlayerManager playerManager; // holds child objs of player
    MoveCam moveCam; // moves the camera with the player
    PlayerCam playerCam; // roatates camera with the player

    [Header("VR")]
    public VRRigManager vr_rig;

    [Header("Scene Management")]
    public int dungeonSceneBuildIndex; 

    // Start is called before the first frame update
    void Start()
    {

        // FIRST PERSON SETUP
        if (fp_player != null)
            playerManager = fp_player.GetComponent<PlayerManager>();

            if (cameraHolder != null)
            {
                moveCam = cameraHolder.GetComponent<MoveCam>();
                playerCam = cameraHolder.GetComponentInChildren<PlayerCam>();

                moveCam.camPos = playerManager.camPos.transform;
                playerCam.orientation = playerManager.orientation.transform;
            }
         

        // VR SETUP
        if (vr_rig != null)
            vr_rig.ResetPhysicsHandPosition();


        if (initGameStart)
            StartCoroutine(GameStart());

    }


    IEnumerator GameStart()
    {
        StartCoroutine(dunGenManager.Generate(Vector2.zero));
        yield return new WaitUntil(() => dunGenManager.dungeonStartTile.playerSpawn != null);


        SpawnPlayerInDungeon();
    }

    public void SpawnPlayerInDungeon()
    {

        if (fp_player != null)
        {
            fp_player.transform.position = dunGenManager.dungeonStartTile.GetComponent<TileGenerationManager>().playerSpawn.position + new Vector3(0, 2, 0);

        }
        else if (vr_rig != null)
        {
            Debug.Log("Spawn VR Player");

            vr_rig.transform.localPosition = dunGenManager.dungeonStartTile.GetComponent<TileGenerationManager>().playerSpawn.position + new Vector3(0, 2, 0);
            vr_rig.ResetPhysicsHandPosition();
        }
    }


    public void LoadNewDungeon()
    {
        SceneManager.LoadScene(dungeonSceneBuildIndex);
    }

}
