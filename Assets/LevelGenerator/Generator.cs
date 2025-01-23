using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Generator : MonoBehaviour
{
    public static Generator Instance;

    [SerializeField] private LevelCollection levelCollection;
    [SerializeField] private SpawnCell cellPrefab;
    [SerializeField] private int row, col;
    [SerializeField] private int currentLevelIndex;

    private SpawnCell[,] pipes;
    private List<SpawnCell> startPipes;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Keep this object across scenes
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
            return;
        }

        if (levelCollection == null)
        {
            Debug.LogError("Level Collection reference is missing!");
            return;
        }
        if (row <= 0 || col <= 0)
        {
            Debug.LogError("Invalid row or column values!");
            return;
        }
        if (currentLevelIndex >= levelCollection.levels.Count)
        {
            CreateNewLevel();
        }
        CreateLevelData();
        SpawnLevel();
    }

    private void CreateNewLevel()
    {
        LevelData newLevel = ScriptableObject.CreateInstance<LevelData>();
        newLevel.Row = row;
        newLevel.Column = col;
        newLevel.Data = new List<int>(new int[row * col]); 

        levelCollection.levels.Add(newLevel);
        EditorUtility.SetDirty(levelCollection);
    }

    private void CreateLevelData()
    {
        LevelData levelData = levelCollection.levels[currentLevelIndex];
        if (levelData.Column == col && levelData.Row == row) return;

        levelData.Row = row;
        levelData.Column = col;
        levelData.Data = new List<int>(new int[row * col]); 

        EditorUtility.SetDirty(levelData);
    }

    private void SpawnLevel()
    {
        // Clear existing pipes if any
        if (pipes != null)
        {
            foreach (var pipe in pipes)
            {
                if (pipe != null)
                    Destroy(pipe.gameObject);
            }
        }

        LevelData levelData = levelCollection.levels[currentLevelIndex];
        pipes = new SpawnCell[levelData.Row, levelData.Column];

        for (int i = 0; i < levelData.Row; i++)
        {
            for (int j = 0; j < levelData.Column; j++)
            {
                Vector2 spawnPos = new Vector2(j + 0.5f, i + 0.5f);
                SpawnCell tempPipe = Instantiate(cellPrefab, spawnPos, Quaternion.identity);
                tempPipe.Init(0); // Initialize with empty pipe
                pipes[i, j] = tempPipe;
            }
        }

        AdjustCamera(levelData);
    }

    private void AdjustCamera(LevelData levelData)
    {
        if (Camera.main != null)
        {
            Camera.main.orthographicSize = Mathf.Max(levelData.Row, levelData.Column) + 2f;
            Vector3 cameraPos = new Vector3(levelData.Column * 0.5f, levelData.Row * 0.5f, Camera.main.transform.position.z);
            Camera.main.transform.position = cameraPos;
        }
        else
        {
            Debug.LogError("Main camera not found!");
        }
    }

    private void Update()
    {
        LevelData levelData = levelCollection.levels[currentLevelIndex];
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        int rowIndex = Mathf.FloorToInt(mousePos.y);
        int colIndex = Mathf.FloorToInt(mousePos.x);

        if (rowIndex < 0 || colIndex < 0 || rowIndex >= levelData.Row || colIndex >= levelData.Column) return;

        if (Input.GetMouseButtonDown(0))
        {
            pipes[rowIndex, colIndex].UpdateInput();
        }

        HandleKeyboardInput(rowIndex, colIndex);
    }

    private void HandleKeyboardInput(int rowIndex, int colIndex)
    {
        if (Input.GetKeyDown(KeyCode.Z)) pipes[rowIndex, colIndex].Init(0);
        if (Input.GetKeyDown(KeyCode.X)) pipes[rowIndex, colIndex].Init(1);
        if (Input.GetKeyDown(KeyCode.C)) pipes[rowIndex, colIndex].Init(2);
        if (Input.GetKeyDown(KeyCode.V)) pipes[rowIndex, colIndex].Init(3);
        if (Input.GetKeyDown(KeyCode.A)) pipes[rowIndex, colIndex].Init(4);
        if (Input.GetKeyDown(KeyCode.S)) pipes[rowIndex, colIndex].Init(5);
        if (Input.GetKeyDown(KeyCode.D)) pipes[rowIndex, colIndex].Init(6);
    }

    private void CheckFill()
    {
        LevelData levelData = levelCollection.levels[currentLevelIndex];
        foreach (var pipe in pipes)
        {
            if (pipe.PipeType != 0)
            {
                pipe.IsFilled = false;
            }
        }

        Queue<SpawnCell> checkQueue = new Queue<SpawnCell>();
        HashSet<SpawnCell> finishedPipes = new HashSet<SpawnCell>();
        foreach (var pipe in startPipes)
        {
            checkQueue.Enqueue(pipe);
        }

        while (checkQueue.Count > 0)
        {
            SpawnCell pipe = checkQueue.Dequeue();
            List<SpawnCell> connectedPipes = pipe.ConnectedPipes();
            foreach (var connectedPipe in connectedPipes)
            {
                if (!finishedPipes.Contains(connectedPipe))
                {
                    finishedPipes.Add(connectedPipe);
                    checkQueue.Enqueue(connectedPipe);
                }
            }
        }

        foreach (var filled in finishedPipes)
        {
            filled.IsFilled = true;
        }

        foreach (var pipe in pipes)
        {
            pipe.UpdateFilled();
        }
    }

    private void ResetStartPipe()
    {
        startPipes = new List<SpawnCell>();
        LevelData levelData = levelCollection.levels[currentLevelIndex];

        for (int i = 0; i < levelData.Row; i++)
        {
            for (int j = 0; j < levelData.Column; j++)
            {
                if (pipes[i, j].PipeType == 1)
                {
                    startPipes.Add(pipes[i, j]);
                }
            }
        }
    }

    private void OnApplicationQuit()
    {
        // Save the current level when the editor is closed
        SaveData();
        Debug.Log("Level design saved."); // Display a message
    }

    private void SaveData()
    {
        LevelData levelData = levelCollection.levels[currentLevelIndex];
        for (int i = 0; i < levelData.Row; i++)
        {
            for (int j = 0; j < levelData.Column; j++)
            {
                levelData.Data[i * levelData.Column + j] = pipes[i, j].PipeData;
            }
        }

        EditorUtility.SetDirty(levelData);
        EditorUtility.SetDirty(levelCollection);
    }
}