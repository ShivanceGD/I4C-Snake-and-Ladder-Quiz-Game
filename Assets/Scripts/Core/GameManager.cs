using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public GameObject PlayerPrefab;
    public LevelSettingsScriptableObject[] Levels;
    public QuizManager quizManager;

    [Header("Player Settings")]
    [SerializeField] private int numberOfPlayers = 2;
    [SerializeField] private Players[] TotalPlayers;

    [Header("Game State")]
    public int CurrentLevelIndex = 0;
    private int currentPlayerIndex = 0;
    private bool canPlayTurn = true;

    private List<QuizQuestionData> quizList;
    private Dictionary<Difficulty, Vector2Int> difficultyStepMap;
    private BoardScriptableObect snakeAndLadderJoints;

    private QuizQuestionData currentQuestion;
    private float currentTimeTaken;
    private Difficulty currentDifficulty;

    // 🏁 WIN STATE
    private List<Players> finishOrder = new List<Players>();
    private int winningTileIndex => BoardManager.tilePositions.Count - 1;

    private void Start()
    {
        InitPlayers();
        CacheQuizData();
        CacheStepData();
        StartQuizTurn();
    }

    private void InitPlayers()
    {
        TotalPlayers = new Players[numberOfPlayers];

        for (int i = 0; i < numberOfPlayers; i++)
        {
            GameObject playerObj = Instantiate(PlayerPrefab);
            playerObj.name = $"Player{i + 1}";
            TotalPlayers[i] = playerObj.GetComponent<Players>();
        }
    }

    private void CacheQuizData()
    {
        quizList = Levels[CurrentLevelIndex].LevelQuizSCO.questions;
        snakeAndLadderJoints = Levels[CurrentLevelIndex].BoardJointsSCO;
    }

    private void CacheStepData()
    {
        difficultyStepMap = Levels[CurrentLevelIndex].DiceRollRangePerQuizDifficulty;
    }

    private void StartQuizTurn()
    {
        if (!canPlayTurn) return;

        if (quizList == null || quizList.Count == 0)
        {
            Debug.LogWarning("No quiz data found for current level.");
            return;
        }

        // ⏭️ Skip if player already finished
        while (finishOrder.Contains(TotalPlayers[currentPlayerIndex]))
        {
            currentPlayerIndex = (currentPlayerIndex + 1) % TotalPlayers.Length;
        }

        int randomIndex = Random.Range(0, quizList.Count);
        currentQuestion = quizList[randomIndex];
        Players currentPlayer = TotalPlayers[currentPlayerIndex];

        quizManager.ShowQuiz(currentQuestion, currentPlayer, OnQuizAnswered);
    }

    private void OnQuizAnswered(bool isCorrect, Difficulty difficulty, float timeTaken)
    {
        currentTimeTaken = timeTaken;
        currentDifficulty = difficulty;

        Players player = TotalPlayers[currentPlayerIndex];
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

        currentPlayerIndex = (currentPlayerIndex + 1) % TotalPlayers.Length;

        if (finishOrder.Count == numberOfPlayers)
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
            player.SetCurrentIndex(winningTileIndex); // ✅ Fix: update using setter
            finishOrder.Add(player);
            Debug.Log($"🎉 {player.name} has finished!");
        }
    }
}
