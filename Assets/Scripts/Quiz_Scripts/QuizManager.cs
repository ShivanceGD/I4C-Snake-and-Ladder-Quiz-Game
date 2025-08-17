using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    public static QuizManager Instance;

    public GameObject starGamePannel;
    public Button startGame;

    [Header("Systems")]
    [SerializeField] private QuizUI quizUI;
    [SerializeField] private QuizTimer quizTimer;
    [SerializeField] private QuizHintSystem hintSystem;

    [Header("Settings")]
    [SerializeField] private Difficulty noHintDifficulty = Difficulty.Easy;
    [SerializeField] private int wrongAnswersToRemove = 2;

    // Raised on the SERVER to drive movement/turn advance (GameManager subscribes)
    public Action<PlayerManager, bool, Difficulty, float> OnQuizCompleted;
    private int lastQuestionIndex;

    // Cached from LevelManager
    private List<QuizQuestionData> questions = new();

    // Per-quiz state (local to the client whose turn it is)
    private QuizQuestionData currentQuestion;
    private PlayerManager currentPlayer;
    private bool hintUsed;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (quizUI != null)
        {
            quizUI.OnOptionSelected += HandleOptionSelected;
            quizUI.OnHintRequested += HandleHintRequest;
        }

        if (quizTimer != null)
        {
            // Time up = treat as incorrect
            quizTimer.OnTimeUp.AddListener(_ => EndQuiz(false));
        }
    }

    #region Public API used by your setup

    public void CacheQuestionsFromLevel()
    {
        var lvl = LevelManager.Instance?.CurrentLevel;
        if (lvl == null)
        {
            Debug.LogError("QuizManager: LevelManager.CurrentLevel is null.");
            return;
        }
        questions = lvl.LevelQuizSCO?.questions ?? new List<QuizQuestionData>();
        if (questions.Count == 0) Debug.LogWarning("QuizManager: No questions found in current level.");
    }

    public void ShowQuizFromServerIndex(int questionIndex, PlayerManager localPlayer)
    {
        if (questions == null || questions.Count == 0) CacheQuestionsFromLevel();
        if (questions == null || questionIndex < 0 || questionIndex >= questions.Count)
        {
            Debug.LogError($"QuizManager: Invalid question index {questionIndex}.");
            return;
        }

        var q = questions[questionIndex];
        ShowQuiz(q, localPlayer);
    }

    public void ShowQuiz(QuizQuestionData question, PlayerManager player)
    {
        if (question == null || player == null)
        {
            Debug.LogError("QuizManager.ShowQuiz: question/player is null.");
            return;
        }

        currentQuestion = question;
        currentPlayer = player;
        hintUsed = false;

        quizUI.ShowQuestion(question);
        quizUI.ShowCharacter(question);
        quizUI.SetHintButtonState(CanUseHint());
        quizTimer.StartTimer(question.timeLimit, quizUI.UpdateTimerDisplay);
    }

    #endregion

    #region UI/Hint handlers

    public bool CanUseHint() =>
        !hintUsed &&
        currentQuestion != null &&
        currentQuestion.difficulty != noHintDifficulty &&
        currentPlayer != null &&
        currentPlayer.HasHints();

    public void UseHint()
    {
        if (!CanUseHint()) return;

        hintSystem.RemoveWrongAnswers(wrongAnswersToRemove, currentQuestion, quizUI);

        hintUsed = true;
        currentPlayer.UseHint();
        quizUI.SetHintButtonState(false);
    }

    private void HandleOptionSelected(int selectedIndex)
    {
        if (currentQuestion == null) return;
        bool isCorrect = selectedIndex == currentQuestion.correctAnswerIndex;
        EndQuiz(isCorrect);
    }

    private void HandleHintRequest() => UseHint();

    #endregion

    #region Finish + report to server

    private void EndQuiz(bool isCorrect)
    {
        // Stop local UI
        if (quizTimer != null) quizTimer.StopTimer();
        if (quizUI != null) quizUI.HideQuizPannel();

        float timeTaken = quizTimer != null ? quizTimer.ElapsedTime : 0f;

        // Multiplayer path: send result to server so movement/turn advance happens server-side
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !IsOffline())
        {
            ulong localId = NetworkManager.Singleton.LocalClientId;

            var netFlow = GameManager.Instance != null ? GameManager.Instance.netFlow : null;
            if (netFlow != null)
            {
                netFlow.SendQuizResultServerRpc(localId, isCorrect,
                    currentQuestion != null ? currentQuestion.difficulty : Difficulty.Easy,
                    timeTaken);
                return;
            }
        }

        // Fallback (offline or no netFlow): invoke event locally
        OnQuizCompleted?.Invoke(currentPlayer, isCorrect,
            currentQuestion != null ? currentQuestion.difficulty : Difficulty.Easy,
            timeTaken);
    }

    private bool IsOffline()
    {
        return GameManager.Instance != null && GameManager.Instance.isOfflineMode;
    }

    #endregion

    #region Helpers

    /*public int GetRandomQuestionIndex()
    {
        if (questions == null || questions.Count == 0) CacheQuestionsFromLevel();
        if (questions == null || questions.Count == 0) return -1;
        return UnityEngine.Random.Range(0, questions.Count);
    }*/
    public int GetRandomQuestionIndex()
    {
        if (questions == null || questions.Count == 0) CacheQuestionsFromLevel();
        if (questions == null || questions.Count == 0) return -1;

        int newIndex;
        do
        {
            newIndex = UnityEngine.Random.Range(0, questions.Count);
        } while (questions.Count > 1 && newIndex == lastQuestionIndex);

        lastQuestionIndex = newIndex;
        return newIndex;
    }

    public QuizQuestionData GetQuestionByIndex(int idx)
    {
        if (questions == null || questions.Count == 0) CacheQuestionsFromLevel();
        if (questions == null || idx < 0 || idx >= questions.Count) return null;
        return questions[idx];
    }

    #endregion
}
