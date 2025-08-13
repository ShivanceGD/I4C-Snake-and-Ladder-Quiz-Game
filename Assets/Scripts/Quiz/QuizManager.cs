using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class QuizManager : MonoBehaviour
{
    public static QuizManager Instance;

    [Header("References")]
    [SerializeField] private QuizTimer quizTimer;
    [SerializeField] private QuizHint quizHint;
    [SerializeField] private  CharacterSCO character;

    [Header("QuizUI")]
    [SerializeField] private GameObject quizPanel;
    [SerializeField] private TMP_Text questionText;
    public TMP_Text timerText;
    [SerializeField] private Button hintButton;
    [SerializeField] private Button[] optionButtons;
    [Header("CharacterUI")]
    [SerializeField] private Image Character_Image;
    [SerializeField] private Image CharacterBackground_Image;
    [SerializeField] private TMP_Text CharacterName_text;
    [SerializeField] private TMP_Text CharacterInfo_text;

    [Header("Hint Settings")]
    [SerializeField] private Difficulty noHintDifficulty;
    [SerializeField] private int wrongAnswersToRemove = 2;

    private QuizQuestionData currentQuestion;
    public PlayerManager currentPlayer;
    private Action<bool, Difficulty, float> onQuizComplete;

    private bool isQuizActive = false;
    private bool hintUsed = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        if (quizTimer == null) quizTimer = GetComponent<QuizTimer>();
        if (quizHint == null) quizHint = GetComponent<QuizHint>();

        quizPanel.SetActive(false);
        SetupUIListeners();
    }

    #region UI Setup & Input Handling
    private void SetupUIListeners()
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            optionButtons[i].onClick.AddListener(() => HandleOptionSelected(index));
        }

        hintButton.onClick.AddListener(Hint);
    }

    private void SetupQuestionUI()
    {
        questionText.text = currentQuestion.question;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].gameObject.SetActive(true);
            TMP_Text btnText = optionButtons[i].GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.text = currentQuestion.options[i];
            }
        }
    }
    private void SetupCharacterUI()
    {
        CharacterBackground_Image.color = RandomCharacterInfo<Color>(character.BGColor);
        
        if(currentQuestion.isAnonymous)
        {
            Character_Image.sprite = character.Anonymous_icon;
            CharacterName_text.text = character.Anonymous_Name;
            CharacterInfo_text.text = character.Anonymous_Name;
        }
        else
        {
            int gender = UnityEngine.Random.Range(0, 2);
            if (gender == 0)
            {
                Character_Image.sprite = RandomCharacterInfo<Sprite>(character.FemaleCharacters_Icons);
                CharacterName_text.text = RandomCharacterInfo<string>(character.FemaleCharacters_Names);
        }
            else
            {
                Character_Image.sprite = RandomCharacterInfo<Sprite>(character.MaleCharacters_Icons);
                CharacterName_text.text = RandomCharacterInfo<string>(character.MaleCharacters_Names);
            }
            CharacterInfo_text.text = RandomCharacterInfo<string>(character.JobDescription);
        }
        
        
    }

    private void HandleOptionSelected(int selectedIndex)
    {
        if (!isQuizActive || currentQuestion == null)
            return;

        bool isCorrect = selectedIndex == currentQuestion.correctAnswerIndex;
        EndQuiz(isCorrect, quizTimer.ElapsedTime);
    }
    #endregion

    #region Quiz Lifecycle
    public void ShowQuiz(QuizQuestionData question, PlayerManager player, Action<bool, Difficulty, float> onComplete)
    {
        currentQuestion = question;
        currentPlayer = player;
        onQuizComplete = onComplete;

        isQuizActive = true;
        hintUsed = false;

        quizPanel.SetActive(true);
        SetupQuestionUI();
        SetupCharacterUI();
        SetupHintAvailability();
        StartTimer(currentQuestion.timeLimit);
    }

    private void SetupHintAvailability()
    {
        hintButton.interactable = CanUseHint();
    }

    public bool CanUseHint()
    {
        return !hintUsed && currentQuestion.difficulty != noHintDifficulty && currentPlayer != null && currentPlayer.HasHints();
    }

    public void OnHintUsed()
    {
        hintUsed = true;
        hintButton.interactable = false;
    }

    private void OnTimerFinished(float elapsedTime)
    {
        if (isQuizActive)
        {
            EndQuiz(false, elapsedTime);
        }
    }

    private void EndQuiz(bool isCorrect, float timeTaken)
    {
        isQuizActive = false;
        quizTimer.StopTimer();
        quizPanel.SetActive(false);

        onQuizComplete?.Invoke(isCorrect, currentQuestion.difficulty, timeTaken);
    }
    #endregion

    #region Delegated Calls
    private void StartTimer(float timeLimit)
    {
        quizTimer.OnTimeUp.RemoveAllListeners();
        quizTimer.OnTimeUp.AddListener(OnTimerFinished);
        quizTimer.StartTimerWithLimit(timeLimit);
    }

    private void Hint()
    {
        quizHint.UseHint(wrongAnswersToRemove, currentQuestion, currentPlayer, optionButtons, CanUseHint(), OnHintUsed);
    }
    #endregion
    #region Generic Function
    private  T RandomCharacterInfo<T>(List<T> list)
    {
        int num = UnityEngine.Random.Range(0, list.Count);
        return list[num];
    }
    #endregion

}
