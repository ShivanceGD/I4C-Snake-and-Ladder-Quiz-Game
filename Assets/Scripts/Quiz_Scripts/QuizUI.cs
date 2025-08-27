using NUnit.Framework;
using System;
using System.Collections.Generic;
using TMPro;
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
    public Button[] optionButtons;
    public Button optionButtonPrefab;
    public Transform optionButtonTransform;
    public Sprite defaultButtonSprite;
    public TMP_Text Difficulty_text;

    [Header("Character Panel UI")]
    public Image Character_Icon;
    public Image CharacterBackground_Icon;
    public TMP_Text CharacterName_text;
    public TMP_Text CharacterInfo_text;
    [Header("Misc")]
    [SerializeField] Color Easy;
    [SerializeField] Color Medium;
    [SerializeField] Color Hard;

    [Header("Characters Reference")]
    public CharacterSCO chars;
    public event Action<int> OnOptionSelected;
    public event Action OnHintRequested;
    

    private void Start()
    {
        if (hintButton) hintButton.onClick.AddListener(() => OnHintRequested?.Invoke());
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int idx = i;
            var b = optionButtons[i];
            if (b) b.onClick.AddListener(() => OnOptionSelected?.Invoke(idx));
        }
        
    }

    
    //
    public void ShowQuizPannelWithDetails(string question, string[] answers, float timeLimit, Difficulty difficulty)
    {
        if (quizPanel) quizPanel.SetActive(true);
        if (questionText) questionText.text = question;
        for (int i = 0; i < answers.Length; i++)
        {
            int idx = i;
            Button button = Instantiate(optionButtonPrefab, optionButtonTransform);
            button.GetComponentInChildren<TMP_Text>().text = answers[idx];
            button.onClick.AddListener(() => OnOptionSelected?.Invoke(idx));
        }
    }
    public void ShowQuestion(QuizQuestionData q)
    {
        if (quizPanel) quizPanel.SetActive(true);
        if (questionText) questionText.text = q.question;
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < q.options.Length) { optionButtons[i].gameObject.SetActive(true); var t = optionButtons[i].GetComponentInChildren<TMP_Text>(); if (t) t.text = q.options[i]; }
            else optionButtons[i].gameObject.SetActive(false);
        }
        ResetOptionSprites();
    }
    public void ShowCharacter(QuizQuestionData q)
    {
        

        CharacterBackground_Icon.color = RandomInfo<Color>(chars.BGColor);
        if(q.isAnonymous)
        {
            CharacterName_text.text = chars.Anonymous_Name;
            CharacterInfo_text.text = chars.Anonymous_Name;
            Character_Icon.sprite = chars.Anonymous_icon;
        }
        else
        {
           
            if(!q.isMale)
            {
                CharacterName_text.text = q.CharName;
                Character_Icon.sprite = RandomInfo<Sprite>(chars.FemaleCharacters_Icons);
            }
            else
            {
                CharacterName_text.text = q.CharName;
                Character_Icon.sprite = RandomInfo<Sprite>(chars.MaleCharacters_Icons);
            }
            CharacterInfo_text.text = q.CharInfo;
        }
        if(q.difficulty == Difficulty.Easy)
        {
            Difficulty_text.text = "Easy";
            Difficulty_text.color = Easy;
        }
        else if(q.difficulty == Difficulty.Medium)
        {
            Difficulty_text.text = "Medium";
            Difficulty_text.color = Medium;
        }
        else if(q.difficulty == Difficulty.Hard)
        {
            Difficulty_text.text = "Hard";
            Difficulty_text.color = Hard;
        }
    }

    public void ResetOptionSprites()
    {
        foreach (var btn in optionButtons)
        {
            if (btn != null && btn.image != null)
                btn.image.sprite = defaultButtonSprite;
        }
    }
    public void UpdateTimerDisplay(float t) { if (timerText) timerText.text = $"{t:F1}s"; }
    public void SetHintButtonState(bool active) { if (hintButton) hintButton.interactable = active; }
    public void RemoveOption(int index) { if (index >= 0 && index < optionButtons.Length) optionButtons[index].gameObject.SetActive(false); }
    public void HideQuizPannel() { if (quizPanel) quizPanel.SetActive(false); }
    public T RandomInfo<T>(List<T> list) { return list[UnityEngine.Random.Range(0, list.Count)]; }
    

}
