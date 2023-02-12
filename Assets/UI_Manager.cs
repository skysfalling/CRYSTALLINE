using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class UI_Manager : MonoBehaviour
{
    public GameManager gameManager;
    public WorldGenManager worldGenManager;

    [Header("TEXT")]
    public TextMeshProUGUI console;
    public int consoleLength = 20;
    private List<string> consoleLogList = new List<string>();

    [Space(10)]

    [Header("UI")]
    public Button startGame;
    public Toggle spawnCeilings;

    [Header("Tile Size")]
    public Slider tileSizeSlider;
    public TextMeshProUGUI tileSizeText;
    public Vector2 tileSizeRange = new Vector2(1, 20);

    [Header("Obstacle Spawn Weight")]
    public Slider obstacleSpawnWeightToggle;
    public TextMeshProUGUI obstacleSpawnWeightText;

    [Header("Room Spawn Weight")]
    public Slider roomSpawnWeightToggle;
    public TextMeshProUGUI roomSpawnWeightText;


    [Header("NPC Spawn Weight")]
    public Slider npcSpawnWeightToggle;
    public TextMeshProUGUI npcSpawnWeightText;




    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        worldGenManager = gameManager.GetComponentInChildren<WorldGenManager>();


        tileSizeSlider.value = worldGenManager.individualTileSize / tileSizeRange.y; // start percentage for slider
        tileSizeSlider.onValueChanged.AddListener(UpdateTileSize); // update value

        obstacleSpawnWeightToggle.value = worldGenManager.obstacleSpawnWeight; // start percentage for slider
        obstacleSpawnWeightToggle.onValueChanged.AddListener(UpdateObstacleSpawnWeight); // update value

        roomSpawnWeightToggle.value = worldGenManager.roomSpawnWeight; // start percentage for slider
        roomSpawnWeightToggle.onValueChanged.AddListener(UpdateRoomSpawnWeight); // update value

        npcSpawnWeightToggle.value = worldGenManager.npcSpawnWeight; // start percentage for slider
        npcSpawnWeightToggle.onValueChanged.AddListener(UpdateNPCSpawnWeight); // update value

        spawnCeilings.onValueChanged.AddListener(UpdateSpawnCeilings);

    }

    // Update is called once per frame
    void Update()
    {
        tileSizeText.text = "Tile Size: " + worldGenManager.individualTileSize;
        obstacleSpawnWeightText.text = "Obstacle Spawn Weight: " + worldGenManager.obstacleSpawnWeight;
        roomSpawnWeightText.text = "Room Spawn Weight: " + worldGenManager.roomSpawnWeight;
        npcSpawnWeightText.text = "NPC Spawn Weight: " + worldGenManager.npcSpawnWeight;

    }

    public void UpdateTileSize(float value)
    {
        int tileSizeValue = Mathf.FloorToInt(value * tileSizeRange.y);

        if (tileSizeValue < tileSizeRange.x) { tileSizeValue = (int)tileSizeRange.x; } 

        tileSizeSlider.value = value;
        worldGenManager.individualTileSize = tileSizeValue;
    }

    public void UpdateObstacleSpawnWeight(float value)
    {
        worldGenManager.obstacleSpawnWeight = value;
    }

    public void UpdateRoomSpawnWeight(float value)
    {
        worldGenManager.roomSpawnWeight = value;
    }

    public void UpdateNPCSpawnWeight(float value)
    {
        worldGenManager.npcSpawnWeight = value;
    }

    public void UpdateSpawnCeilings(bool enabled)
    {
        worldGenManager.spawnCeilings = enabled;
    }



    #region IN GAME CONSOLE ===================================
    void OnEnable()
    {
        Application.logMessageReceived += LogCallback;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= LogCallback;
    }

    void LogCallback(string logString, string stackTrace, LogType type)
    {
        consoleLogList.Add(logString);

        if (consoleLogList.Count > consoleLength)
        {
            consoleLogList.RemoveAt(0);
        }

        console.text = "";
        foreach (string log in consoleLogList)
        {
            console.text += log + "\r\n";
        }
    }
    #endregion
}
