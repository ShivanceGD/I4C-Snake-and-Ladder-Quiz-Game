using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuizManager : MonoBehaviour
{
    public static QuizManager Instance;

    [SerializeField] private QuizUI quizUI;
    [SerializeField] private QuizTimer quizTimer;
    private QuizHintSystem hintSystem = new QuizHintSystem();

    private List<QuizQuestionData> questions = new List<QuizQuestionData>();
    private int lastQuestionIndex = -1;

    private Coroutine timeoutCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void LoadQuestions(QuizPackSO quizSCO)
    {
        questions = quizSCO?.questions ?? new List<QuizQuestionData>();
    }

    public void ShowQuizForPlayer(
        QuizQuestionData q,
        Player player,
        bool canUseHint,
        Action onUseHint,
        Action<QuizResult> onComplete)
    {
        if (q == null)
        {
            Debug.LogError("[QuizManager] null question");
            onComplete?.Invoke(new QuizResult { IsCorrect = false, SelectedIndex = -1, TimeTaken = 0f });
            return;
        }

        if (quizUI == null)
        {
            Debug.LogError("[QuizManager] quizUI missing");
            onComplete?.Invoke(new QuizResult { IsCorrect = false, SelectedIndex = -1, TimeTaken = 0f });
            return;
        }

        // add to player's local history
        player.QuestionsList.Add(q);

        // show UI
        quizUI.ShowQuestion(q);
        quizUI.ShowCharacter(q);

        // ---- HINT INTEGRATION ----
        quizUI.SetHintButtonState(canUseHint);
        if (canUseHint)
        {
            quizUI.SetHintAction(() =>
            {
                onUseHint?.Invoke();
                quizUI.SetHintButtonState(false);

                // remove 2 random wrong answers (50/50 style)
                int removeCount = Mathf.Min(2, q.options.Length - 1);
                var toRemove = hintSystem.GetHints(q.options.Length, q.correctAnswerIndex, removeCount);
                foreach (var idx in toRemove)
                    quizUI.RemoveOption(idx);
            });
        }
        else
        {
            quizUI.SetHintAction(null);
        }
        // --------------------------

        // local handler for answer
        void LocalHandler(int idx)
        {
            Cleanup();
            float t = (quizTimer != null) ? quizTimer.ElapsedTime : 0f;
            var res = new QuizResult
            {
                IsCorrect = idx == q.correctAnswerIndex,
                SelectedIndex = idx,
                TimeTaken = t
            };
            onComplete?.Invoke(res);
        }

        // timeout fallback
        IEnumerator TimeoutWatcher(float wait, Action onTimeout)
        {
            yield return new WaitForSeconds(wait + 0.05f);
            if (quizUI != null && quizUI.quizPanel != null && quizUI.quizPanel.activeSelf)
            {
                Cleanup();
                onTimeout?.Invoke();
            }
        }

        // assign option handler
        quizUI.SetOptionAction(LocalHandler);

        // start timer
        if (quizTimer != null) quizTimer.StartTimer(q.timeLimit, quizUI.UpdateTimerDisplay);

        // start fallback watcher
        timeoutCoroutine = StartCoroutine(TimeoutWatcher(q.timeLimit, () =>
        {
            var res = new QuizResult
            {
                IsCorrect = false,
                SelectedIndex = -1,
                TimeTaken = q.timeLimit
            };
            onComplete?.Invoke(res);
        }));

        Debug.Log($"[QuizManager] Shown question for {player.name}: \"{q.question}\"");
    }

    private void Cleanup()
    {
        // stop timeout watcher
        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
            timeoutCoroutine = null;
        }

        // stop timer
        if (quizTimer != null) quizTimer.StopTimer();

        // reset UI
        if (quizUI != null)
        {
            quizUI.HideQuizPannel();
            quizUI.SetOptionAction(null);
            quizUI.SetHintAction(null);
        }
    }

    public int GetRandomQuestionIndex()
    {
        if (questions == null || questions.Count == 0) return -1;
        int newIdx;
        do { newIdx = UnityEngine.Random.Range(0, questions.Count); }
        while (questions.Count > 1 && newIdx == lastQuestionIndex);
        lastQuestionIndex = newIdx;
        return newIdx;
    }

    public QuizQuestionData GetQuestionByIndex(int idx)
    {
        if (questions == null || idx < 0 || idx >= questions.Count) return null;
        return questions[idx];
    }
    
}

public struct QuizResult
{
    public bool IsCorrect;
    public float TimeTaken;
    public int SelectedIndex;
}

public struct MovementResult
{
    public Player Player;
    public int StepsTaken;
    public int FinalTileIndex;
    public bool Finished;
    public bool UsedSnakeOrLadder;
    public string Reason; // "Ladder","Snake","Normal"
}
