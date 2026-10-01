using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.U2D.IK;
using UnityEngine.UI;

public class QuizUI : MonoBehaviour
{
    [Header("Quiz Panel UI")]
    public GameObject quizPanel;
    public TMP_Text questionText;
    public TMP_Text timerText;
    public Button hintButton;
    public List<Button> optionButtons;
    public Sprite defaultButtonSprite;
    public TMP_Text Difficulty_text;

    [Header("Character Panel UI")]
    public Image Character_Icon;
    public Image CharacterBackground_Icon;
    public TMP_Text CharacterName_text;
    public TMP_Text CharacterInfo_text;
    [Header("Misc")]
    [SerializeField] Color EasyColor;
    [SerializeField] Color MediumColor;
    [SerializeField] Color HardColor;

    [Header("Characters Reference")]
    public CharacterSCO chars;
    
    private Action<int> onOptionSelected;
    private Action onHintAction;
    
    
    private void Start()
    {
        if (hintButton) hintButton.onClick.AddListener(() => onHintAction?.Invoke());

        for (int i = 0; i < optionButtons.Count; i++)
        {
            int idx = i;
            var b = optionButtons[i];
            if (b) b.onClick.AddListener(() => onOptionSelected?.Invoke(idx));
        }
    }
    
    public void SetOptionAction(Action<int> action)
    {
        onOptionSelected = action;
    }
    public void SetHintAction(Action action)
    {
        onHintAction = action;
    }
    
    public void ShowQuizPannelWithDetails(string question, string[] answers, float timeLimit, QuestionsDifficulty questionsDifficulty)
    {
        if (quizPanel) quizPanel.SetActive(true);
        if (questionText) questionText.text = question;
        for (int i = 0; i < optionButtons.Count; i++)
        {
            if (i < answers.Length) 
            { 
                optionButtons[i].gameObject.SetActive(true); 
                var t = optionButtons[i].GetComponentInChildren<TMP_Text>(); 
                if (t) t.text = answers[i]; 
            }
            else optionButtons[i].gameObject.SetActive(false);
        }
        //SetDifficultyColorAndText(questionsDifficulty);
    }
    public void ShowQuestion(QuizQuestionData q)
    {
        if (quizPanel) quizPanel.SetActive(true);
        if (questionText) questionText.text = q.question;
        for (int i = 0; i < optionButtons.Count; i++)
        {
            if (i < q.options.Length) { optionButtons[i].gameObject.SetActive(true); var t = optionButtons[i].GetComponentInChildren<TMP_Text>(); if (t) t.text = q.options[i]; }
            else optionButtons[i].gameObject.SetActive(false);
        }
        SetDifficultyColorAndText(q.questionsDifficulty);
    }
        //ResetOptionSprites();
    
    public void ShowCharacter(QuizQuestionData q)
    {
        CharacterBackground_Icon.color = RandomInfo<Color>(chars.BGColor);
        if(q.characterData.characterGender == CharacterGender.Anonymous)
        {
            Character_Icon.sprite = chars.Anonymous_icon;
        }
        else
        {
            if(q.characterData.characterGender == CharacterGender.Male)
            {
                Character_Icon.sprite = RandomInfo<Sprite>(chars.MaleCharacters_Icons);
            }
            else
            {
                Character_Icon.sprite = RandomInfo<Sprite>(chars.FemaleCharacters_Icons);
            }
           
        }
        CharacterName_text.text = q.characterData.characterName;
        CharacterInfo_text.text = q.characterData.characterInfo;
    }

    public void ResetOptionSprites()
    {
        foreach (var btn in optionButtons)
        {
            if (btn != null && btn.image != null)
                btn.image.sprite = defaultButtonSprite;
        }
    }
    public void UpdateTimerDisplay(float t) { if (timerText) timerText.text = $"Timer: {t:F1}s"; }
    public void SetHintButtonState(bool active) { if (hintButton) hintButton.interactable = active; }
    public void RemoveOption(int index) { if (index >= 0 && index < optionButtons.Count) optionButtons[index].gameObject.SetActive(false); }
    public void HideQuizPannel() { if (quizPanel) quizPanel.SetActive(false); }
    public T RandomInfo<T>(List<T> list) { return list[UnityEngine.Random.Range(0, list.Count)]; }

    private void SetDifficultyColorAndText(QuestionsDifficulty diff)
    {
        switch (diff)
        {
            case QuestionsDifficulty.Easy:
            {
                Difficulty_text.color = EasyColor;
                Difficulty_text.text = "Easy";
                break;
            }
            case QuestionsDifficulty.Medium:
            {
                Difficulty_text.color = MediumColor;
                Difficulty_text.text = "Medium";
                break;
            }
            case QuestionsDifficulty.Hard:
            {
                Difficulty_text.color = HardColor;
                Difficulty_text.text = "Hard";
                break;
            }
            default:
            {
                Difficulty_text.color = Color.white;
                break;
            }
        }
    }

}
