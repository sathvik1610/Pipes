using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using DG.Tweening;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private LevelCollection levelCollection;
    [SerializeField] private Pipe cellPrefab;
    [SerializeField] private int currentLevelIndex;
    // [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI levelIndicatorText;

    [SerializeField] public TextMeshProUGUI levelCompleteText;
    [SerializeField] public TextMeshProUGUI level1CompleteText;
    [SerializeField] public TextMeshProUGUI level2CompleteText;
    [SerializeField] private GameObject gameCompletionPanel;
    [SerializeField] private TextMeshProUGUI gameCompletionText;
    [SerializeField] private Image backgroundOverlay;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    private bool hasGameFinished;
    private Pipe[,] pipes;
    private List<Pipe> startPipes;

    private void Awake()
    {
        levelCompleteText.gameObject.SetActive(false);
        level1CompleteText.gameObject.SetActive(false);
        level2CompleteText.gameObject.SetActive(false);
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        hasGameFinished = false;
        levelCompleteText.gameObject.SetActive(false);

        if (levelCollection == null || levelCollection.levels.Count == 0)
        {
            Debug.LogError("Level Collection is missing or empty!");
            return;
        }

        // Start game immediately
        SpawnLevel();
        gameCompletionPanel.SetActive(false);
        restartButton.onClick.AddListener(RestartGame);
        quitButton.onClick.AddListener(QuitGame);

    }
    // private void StartGame()
    // {
    //     SpawnLevel();
    //     startButton.gameObject.SetActive(false); // Hide start button after game starts
    // }

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

        Transform levelParent = new GameObject("Level").transform;
        LevelData levelData = levelCollection.levels[currentLevelIndex];
        pipes = new Pipe[levelData.Row, levelData.Column];
        levelIndicatorText.text = $"Level {currentLevelIndex + 1}/{levelCollection.levels.Count}";

        startPipes = new List<Pipe>();

        for (int i = 0; i < levelData.Row; i++)
        {
            for (int j = 0; j < levelData.Column; j++)
            {
                Vector2 spawnPos = new Vector2(j + 0.5f, i + 0.5f);
                Pipe tempPipe = Instantiate(cellPrefab, spawnPos, Quaternion.identity, levelParent);
                tempPipe.Init(levelData.Data[i * levelData.Column + j]);
                pipes[i, j] = tempPipe;

                if (tempPipe.PipeType == 1)
                {
                    startPipes.Add(tempPipe);
                }
            }
        }

        AdjustCamera(levelData);
        StartCoroutine(ShowHint());
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
        if (hasGameFinished) return;

        LevelData levelData = levelCollection.levels[currentLevelIndex];
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        int row = Mathf.FloorToInt(mousePos.y);
        int col = Mathf.FloorToInt(mousePos.x);

        if (row < 0 || col < 0 || row >= levelData.Row || col >= levelData.Column) return;

        if (Input.GetMouseButtonDown(0))
        {
            pipes[row, col].UpdateInput();
            StartCoroutine(ShowHint());
        }
    }

    private IEnumerator ShowHint()
    {
        yield return new WaitForSeconds(0.1f);
        CheckFill();
        CheckWin();
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

        Queue<Pipe> checkQueue = new Queue<Pipe>();
        HashSet<Pipe> finishedPipes = new HashSet<Pipe>(startPipes);

        foreach (var pipe in startPipes)
        {
            checkQueue.Enqueue(pipe);
        }

        while (checkQueue.Count > 0)
        {
            Pipe pipe = checkQueue.Dequeue();
            List<Pipe> connectedPipes = pipe.ConnectedPipes();
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

    private void CheckWin()
    {
        LevelData levelData = levelCollection.levels[currentLevelIndex];
        bool allDestinationsFilled = true;

        foreach (var pipe in pipes)
        {
            // Check only destination pipes (assuming PipeType 2 is destination)
            if (pipe.PipeType == 2 && !pipe.IsFilled)
            {
                allDestinationsFilled = false;
                break;
            }
        }

        if (allDestinationsFilled)
        {
            hasGameFinished = true;
            StartCoroutine(GameFinished());
        }
    }

    private IEnumerator GameFinished()
    {
        switch (currentLevelIndex)
        {
            case 0:
                level1CompleteText.gameObject.SetActive(true);
                yield return new WaitForSeconds(2f);
                level1CompleteText.gameObject.SetActive(false);
                break;
            case 1:
                level2CompleteText.gameObject.SetActive(true);
                yield return new WaitForSeconds(2f);
                level2CompleteText.gameObject.SetActive(false);
                break;
            default:
                if (currentLevelIndex >= levelCollection.levels.Count - 1)
                {
                    StartCoroutine(ShowGameCompletionPanel());
                    yield break;
                }

                break;
        }

        currentLevelIndex++;
        if (currentLevelIndex < levelCollection.levels.Count)
        {
            SpawnLevel();
            hasGameFinished = false;
        }
        else
        {
            Debug.Log("Congratulations! All levels completed!");
        }
    }
    private IEnumerator ShowGameCompletionPanel()
    {
        // Disable game elements
        foreach (var pipe in pipes)
        {
            if (pipe != null)
                pipe.gameObject.SetActive(false);
        }

        // Activate and animate background overlay
        backgroundOverlay.gameObject.SetActive(true);
        backgroundOverlay.DOFade(1f, 0.5f);

        // Activate completion panel with scaling animation
        gameCompletionPanel.SetActive(true);
        gameCompletionPanel.transform.localScale = Vector3.zero;
        gameCompletionPanel.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);

        // Animate text
        gameCompletionText.transform.localScale = Vector3.zero;
        gameCompletionText.transform.DOScale(1f, 0.5f).SetDelay(0.3f).SetEase(Ease.OutBack);
        restartButton.gameObject.SetActive(true);
        quitButton.gameObject.SetActive(true);

        // Animate buttons
        restartButton.transform.localScale = Vector3.zero;
        quitButton.transform.localScale = Vector3.zero;
        
        restartButton.transform.DOScale(1f, 0.5f).SetDelay(0.5f).SetEase(Ease.OutBack);
        quitButton.transform.DOScale(1f, 0.5f).SetDelay(0.6f).SetEase(Ease.OutBack);

        yield return null;

        // Optional: Add a restart or quit button functionality here
    }
    private void RestartGame()
    {
        // Reset game state
        hasGameFinished = false;
        gameCompletionPanel.SetActive(false);
        backgroundOverlay.gameObject.SetActive(false);
        backgroundOverlay.color = new Color(backgroundOverlay.color.r, backgroundOverlay.color.g, backgroundOverlay.color.b, 0);

        // Reset level
        currentLevelIndex = 0;
        SpawnLevel();
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}