using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;
using Random = UnityEngine.Random;

public class QuizManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject quizPanel;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Button hintButton;

    [Header("Hint Settings")]
    [SerializeField] private Difficulty difficultyLevelForNoHints;
    [SerializeField] private int wrongAnswersToRemoveForHint = 2;

    private QuizQuestionData currentQuestion;
    private Action<bool, Difficulty, float> onQuizComplete;
    private Coroutine timerCoroutine;
    private bool isQuizActive = false;
    private bool hintUsed = false;
    private Players currentPlayer;

    private void Awake()
    {
        SetupListeners();
        quizPanel.SetActive(false);
    }

    private void SetupListeners()
    {
        foreach (Button btn in optionButtons)
            btn.onClick.AddListener(() => OnOptionSelected(btn));

        hintButton.onClick.AddListener(UseHint);
    }

    public void ShowQuiz(QuizQuestionData questionData, Players player, Action<bool, Difficulty, float> onComplete)
    {
        currentQuestion = questionData;
        currentPlayer = player;
        onQuizComplete = onComplete;
        isQuizActive = true;
        hintUsed = false;

        DisplayQuestionUI();
        StartTimer(currentQuestion.timeLimit);
    }

    private void DisplayQuestionUI()
    {
        quizPanel.SetActive(true);
        questionText.text = currentQuestion.question;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].gameObject.SetActive(true);
            optionButtons[i].interactable = true;
            optionButtons[i].GetComponentInChildren<TMP_Text>().text = currentQuestion.options[i];
        }

        bool canUseHint = currentQuestion.isHintAllowed
                          && currentQuestion.difficulty != difficultyLevelForNoHints
                          && currentPlayer.CanUseHint();

        hintButton.gameObject.SetActive(true); // Always visible
        hintButton.interactable = canUseHint;  // Only interactable if eligible
    }


    private void StartTimer(float timeLimit)
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        timerCoroutine = StartCoroutine(QuizTimer(timeLimit));
    }

    private IEnumerator QuizTimer(float timeLimit)
    {
        float timer = 0f;
        while (timer < timeLimit && isQuizActive)
        {
            timer += Time.deltaTime;
            timerText.text = $"Time: {timer:F1}s";
            yield return null;
        }

        if (isQuizActive)
        {
            EndQuiz(false, timer);
        }
    }

    private void OnOptionSelected(Button selectedButton)
    {
        if (!isQuizActive) return;

        int selectedIndex = Array.FindIndex(optionButtons, btn => btn == selectedButton);
        bool isCorrect = selectedIndex == currentQuestion.correctAnswerIndex;

        EndQuiz(isCorrect, float.Parse(timerText.text.Replace("Time: ", "").Replace("s", "")));
    }

    private void UseHint()
    {
        if (!currentQuestion.isHintAllowed || hintUsed || !currentPlayer.CanUseHint()) return;

        int removed = 0;
        int attempts = 0;

        while (removed < wrongAnswersToRemoveForHint && attempts < 100)
        {
            int randIndex = Random.Range(0, optionButtons.Length);
            if (randIndex != currentQuestion.correctAnswerIndex && optionButtons[randIndex].gameObject.activeSelf)
            {
                optionButtons[randIndex].gameObject.SetActive(false);
                removed++;
            }
            attempts++;
        }

        currentPlayer.UseHint();
        hintUsed = true;
        hintButton.interactable = false;
    }

    private void EndQuiz(bool isCorrect, float timeTaken)
    {
        isQuizActive = false;
        quizPanel.SetActive(false);
        onQuizComplete?.Invoke(isCorrect, currentQuestion.difficulty, timeTaken);
    }
}