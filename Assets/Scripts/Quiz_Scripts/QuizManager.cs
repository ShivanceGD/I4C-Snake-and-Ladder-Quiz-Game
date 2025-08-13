using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class QuizManager : MonoBehaviour
{
    public static QuizManager Instance;

    [Header("Systems")]
    [SerializeField] private QuizUI quizUI;
    [SerializeField] private QuizTimer quizTimer;
    [SerializeField] private QuizHintSystem hintSystem;

    [Header("Settings")]
    [SerializeField] private Difficulty noHintDifficulty = Difficulty.Easy;
    [SerializeField] private int wrongAnswersToRemove = 2;

    // Raised on the SERVER to drive movement/turn advance (GameManager subscribes)
    public Action<PlayerManager, bool, Difficulty, float> OnQuizCompleted;

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

    /// <summary>
    /// Called by NetworkFlowManager on the target client to open the quiz UI
    /// with a specific question index and the local player.
    /// </summary>
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

    /// <summary>
    /// Optional direct-show method (also used internally).
    /// </summary>
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

        // Your original HintSystem signature uses its own serialized QuizUI
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
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
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

    #endregion

    #region Helpers (optional but handy)

    public int GetRandomQuestionIndex()
    {
        if (questions == null || questions.Count == 0) CacheQuestionsFromLevel();
        if (questions == null || questions.Count == 0) return -1;
        return UnityEngine.Random.Range(0, questions.Count);
    }

    public QuizQuestionData GetQuestionByIndex(int idx)
    {
        if (questions == null || questions.Count == 0) CacheQuestionsFromLevel();
        if (questions == null || idx < 0 || idx >= questions.Count) return null;
        return questions[idx];
    }

    #endregion
}
