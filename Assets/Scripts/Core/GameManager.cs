using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Widgets;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [Header("References")]
    public static GameManager Instance;
    public LevelSettingsScriptableObject[] TotalLevelsList;

    [Header("Players State")]
    public List<PlayerManager> TotalPlayers = new();
    public readonly NetworkList<ulong> playerClientIds = new();
    private readonly NetworkVariable<int> currentPlayerIndex = new(0);

    [Header("Game State")]
    public int CurrentLevelIndex = 0;
    private bool gameStarted = false;
    private readonly List<PlayerManager> finishOrder = new();


    [Header("Quiz Questions State")]
    private List<QuizQuestionData> quizQuestionsList;
    private Dictionary<Difficulty, Vector2Int> stepsPerDifficulty;
    private QuizQuestionData currentQuizQuestion;
    private float currentTimeTaken;
    private Difficulty currentDifficulty;

    [Header("Board Data")]
    private BoardScriptableObect snakeAndLadderJoints;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        CacheQuizAndStepData();
    }

    private void Update()
    {
        if (IsServer && !gameStarted && Input.GetKeyDown(KeyCode.S))
        {
            SyncPlayerListClientRpc();
            StartGame();
        }
    }

    [ContextMenu("Init")]
    public void InitializeWidgetsService()
    {
        WidgetServiceInitialization.ServicesInitialized();
    }

    [ContextMenu("Start Scene")]
    public void LoadScene()
    {
        NetworkManager.SceneManager.LoadScene("Multiplayer_Level", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private void StartGame()
    {
        gameStarted = true;
        StartQuizTurnServerRpc();
    }

    private void CacheQuizAndStepData()
    {
        quizQuestionsList = TotalLevelsList[CurrentLevelIndex].LevelQuizSCO.questions;
        snakeAndLadderJoints = TotalLevelsList[CurrentLevelIndex].BoardJointsSCO;
        stepsPerDifficulty = TotalLevelsList[CurrentLevelIndex].DiceRollRangePerQuizDifficulty;
    }

    private int GenerateRandomQuestion()
    {
        int questionIndex = Random.Range(0, quizQuestionsList.Count);
        currentQuizQuestion = quizQuestionsList[questionIndex];
        return questionIndex;
    }

    private void SkipTurnIfPlayerFinished()
    {
        while (finishOrder.Contains(TotalPlayers[currentPlayerIndex.Value]))
        {
            currentPlayerIndex.Value = (currentPlayerIndex.Value + 1) % TotalPlayers.Count;
        }
    }

    private PlayerManager GetMyPlayerInstance()
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;
        foreach (var player in TotalPlayers)
        {
            if (player.OwnerClientId == localId)
                return player;
        }
        return null;
    }

    private void OnQuizAnswered(bool isCorrect, Difficulty difficulty, float timeTaken)
    {
        SubmitAnswerServerRpc(isCorrect, difficulty, timeTaken);
    }

    private IEnumerator HandlePostQuizMovement(PlayerManager player, bool isCorrect)
    {
        int currentTile = player.currentIndex;

        if (isCorrect)
        {
            foreach (var ladder in snakeAndLadderJoints.Ladders)
            {
                if (ladder.Key > currentTile && ladder.Key - currentTile <= 5)
                {
                    yield return player.MovePlayerTileByTile(ladder.Key - currentTile);
                    yield return player.MovePlayerDirectlyToTile(ladder.Value);
                    CheckWin(player);
                    yield return EndTurnAfterDelay();
                    yield break;
                }
            }
        }
        else
        {
            foreach (var snake in snakeAndLadderJoints.Snakes)
            {
                if (snake.Key >= currentTile && snake.Key - currentTile <= 5)
                {
                    yield return player.MovePlayerTileByTile(snake.Key - currentTile);
                    yield return player.MovePlayerDirectlyToTile(snake.Value);
                    CheckWin(player);
                    yield return EndTurnAfterDelay();
                    yield break;
                }
            }
        }

        if (isCorrect)
        {
            int steps = GetSteps(isCorrect, currentDifficulty, currentTimeTaken);
            yield return player.MovePlayerTileByTile(steps);
            CheckWin(player);
        }

        yield return EndTurnAfterDelay();
    }

    private int GetSteps(bool correct, Difficulty difficulty, float timeTaken)
    {
        if (!correct) return 0;

        if (!stepsPerDifficulty.TryGetValue(difficulty, out Vector2Int range))
        {
            Debug.LogWarning($"Missing step range for difficulty {difficulty}. Using default.");
            return 1;
        }

        int steps = Random.Range(range.x, range.y + 1);
        if (timeTaken < 5f) steps++;

        return steps;
    }

    private IEnumerator EndTurnAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        currentPlayerIndex.Value = (currentPlayerIndex.Value + 1) % TotalPlayers.Count;

        if (CheckAllPlayersFinished())
        {
            Debug.Log("All players finished!");
            for (int i = 0; i < finishOrder.Count; i++)
            {
                Debug.Log($"{i + 1} - {finishOrder[i].name}");
            }
        }
        else
        {
            StartQuizTurnServerRpc();
        }
    }
    private bool CheckAllPlayersFinished()
    {
        return finishOrder.Count == TotalPlayers.Count;
    }

    private void CheckWin(PlayerManager player)
    {
        if (player.currentIndex >= BoardManager.WinningTileIndex && !finishOrder.Contains(player))
        {
            player.SetCurrentIndex(BoardManager.WinningTileIndex);
            finishOrder.Add(player);
            Debug.Log($"🎉 {player.name} has finished!");
        }
    }

    #region NetworkCalls
    [ServerRpc(RequireOwnership = false)]
    private void StartQuizTurnServerRpc()
    {
        if (quizQuestionsList == null || quizQuestionsList.Count == 0) return;

        SkipTurnIfPlayerFinished();
        int questionIndex = GenerateRandomQuestion();

        PlayerManager currentPlayer = TotalPlayers[currentPlayerIndex.Value];
        ShowQuizClientRpc(currentPlayer.OwnerClientId, questionIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitAnswerServerRpc(bool isCorrect, Difficulty difficulty, float timeTaken)
    {
        currentTimeTaken = timeTaken;
        currentDifficulty = difficulty;

        PlayerManager player = TotalPlayers[currentPlayerIndex.Value];
        StartCoroutine(HandlePostQuizMovement(player, isCorrect));
    }

    [ClientRpc]
    private void ShowQuizClientRpc(ulong targetClientId, int questionIndex)
    {
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            PlayerManager currentPlayer = GetMyPlayerInstance();

            QuizManager.Instance.ShowQuiz(quizQuestionsList[questionIndex], currentPlayer, OnQuizAnswered);
        }
    }

    [ClientRpc]
    private void SyncPlayerListClientRpc()
    {
        TotalPlayers.Clear();
        PlayerManager[] allPlayers = FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);

        foreach (ulong id in playerClientIds)
        {
            foreach (var player in allPlayers)
            {
                if (player.OwnerClientId == id)
                {
                    TotalPlayers.Add(player);
                    break;
                }
            }
        }
        Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId}] Player list synced. Count: {TotalPlayers.Count}");
    }
    #endregion
}
