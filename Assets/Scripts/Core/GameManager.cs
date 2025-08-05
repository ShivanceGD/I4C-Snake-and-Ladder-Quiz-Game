using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [Header("References")]
    public static GameManager Instance;
    public LevelSettingsScriptableObject[] TotalLevelsList;
    public QuizManager QuizManager;

    [Header("Players State")]
    public List<Players> TotalPlayers = new List<Players>();
    private NetworkList<ulong> playerClientIds = new NetworkList<ulong>();
    private NetworkVariable<int> currentPlayerIndex = new NetworkVariable<int>(0);

    [Header("Game State")]
    public int CurrentLevelIndex = 0;
    private bool gameStarted = false;
    private List<Players> finishOrder = new List<Players>();
    private int winningTileIndex => BoardManager.tilePositions.Count - 1;

    [Header("Quiz Questions State")]
    private List<QuizQuestionData> quizQuestionsList;
    private Dictionary<Difficulty, Vector2Int> difficultyStepMap;
    private QuizQuestionData currentQuizQuestion;
    private float currentTimeTaken;
    private Difficulty currentDifficulty;

    [Header("Board Data")]
    private BoardScriptableObect snakeAndLadderJoints;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        CacheQuizData();
        CacheStepData();
    }

    private void Update()
    {
        if (IsServer && !gameStarted && Input.GetKeyDown(KeyCode.S))
        {
            SyncPlayerListClientRpc(); // Sync player list before game starts
            StartGame();
        }
    }

    private void StartGame()
    {
        gameStarted = true;
        StartQuizTurnServerRpc();
    }

    private void CacheQuizData()
    {
        quizQuestionsList = TotalLevelsList[CurrentLevelIndex].LevelQuizSCO.questions;
        snakeAndLadderJoints = TotalLevelsList[CurrentLevelIndex].BoardJointsSCO;
    }

    private void CacheStepData()
    {
        difficultyStepMap = TotalLevelsList[CurrentLevelIndex].DiceRollRangePerQuizDifficulty;
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartQuizTurnServerRpc()
    {
        StartQuizTurn();
    }

    private void StartQuizTurn()
    {
        if (quizQuestionsList == null || quizQuestionsList.Count == 0)
        {
            Debug.LogWarning("No quiz data found for current level.");
            return;
        }

        while (finishOrder.Contains(TotalPlayers[currentPlayerIndex.Value]))
        {
            currentPlayerIndex.Value = (currentPlayerIndex.Value + 1) % TotalPlayers.Count;
        }

        int randomIndex = Random.Range(0, quizQuestionsList.Count);
        currentQuizQuestion = quizQuestionsList[randomIndex];

        Players currentPlayer = TotalPlayers[currentPlayerIndex.Value];
        ShowQuizClientRpc(randomIndex, currentPlayer.OwnerClientId);
    }

    [ClientRpc]
    private void ShowQuizClientRpc(int questionIndex, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            var question = TotalLevelsList[CurrentLevelIndex].LevelQuizSCO.questions[questionIndex];
            Players currentPlayer = GetMyPlayerInstance();
            QuizManager.ShowQuiz(question, currentPlayer, OnQuizAnswered);
        }
    }

    private Players GetMyPlayerInstance()
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

    [ServerRpc(RequireOwnership = false)]
    private void SubmitAnswerServerRpc(bool isCorrect, Difficulty difficulty, float timeTaken)
    {
        currentTimeTaken = timeTaken;
        currentDifficulty = difficulty;

        Players player = TotalPlayers[currentPlayerIndex.Value];
        StartCoroutine(HandlePostQuizMovement(player, isCorrect));
    }

    private IEnumerator HandlePostQuizMovement(Players player, bool isCorrect)
    {
        int currentTile = player.CurrentIndex;

        if (isCorrect)
        {
            foreach (var ladder in snakeAndLadderJoints.Ladders)
            {
                if (ladder.Key > currentTile && ladder.Key - currentTile <= 5)
                {
                    yield return player.MovePlayerToDestinationTile(ladder.Key - currentTile);
                    yield return player.MovePlayerToExactIndex(ladder.Value);
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
                if (snake.Key > currentTile && snake.Key - currentTile <= 5)
                {
                    yield return player.MovePlayerToDestinationTile(snake.Key - currentTile);
                    yield return player.MovePlayerToExactIndex(snake.Value);
                    CheckWin(player);
                    yield return EndTurnAfterDelay();
                    yield break;
                }
            }
        }

        if (isCorrect)
        {
            int steps = GetSteps(isCorrect, currentDifficulty, currentTimeTaken);
            yield return player.MovePlayerToDestinationTile(steps);
            CheckWin(player);
        }

        yield return EndTurnAfterDelay();
    }

    private int GetSteps(bool correct, Difficulty difficulty, float timeTaken)
    {
        if (!correct) return 0;

        if (!difficultyStepMap.TryGetValue(difficulty, out Vector2Int range))
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

        if (finishOrder.Count == TotalPlayers.Count)
        {
            Debug.Log("🏁 All players finished!");
            for (int i = 0; i < finishOrder.Count; i++)
            {
                Debug.Log($"{i + 1} - {finishOrder[i].name}");
            }
        }
        else
        {
            StartQuizTurn();
        }
    }

    private void CheckWin(Players player)
    {
        if (player.CurrentIndex >= winningTileIndex && !finishOrder.Contains(player))
        {
            player.SetCurrentIndex(winningTileIndex);
            finishOrder.Add(player);
            Debug.Log($"🎉 {player.name} has finished!");
        }
    }

    public void RegisterPlayer(Players player)
    {
        if (!TotalPlayers.Contains(player))
        {
            TotalPlayers.Add(player);

            if (IsServer)
            {
                playerClientIds.Add(player.OwnerClientId);
            }
        }
    }

    [ClientRpc]
    private void SyncPlayerListClientRpc()
    {
        TotalPlayers.Clear();
        Players[] allPlayers = FindObjectsByType<Players>(FindObjectsSortMode.None);


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
}
