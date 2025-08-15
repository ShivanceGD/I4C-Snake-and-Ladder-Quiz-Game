using NUnit.Framework;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizUI : MonoBehaviour
{
    [Header("Quiz Panel UI")]
    public GameObject quizPanel;
    public TMP_Text questionText;
    public TMP_Text timerText;
    public Button hintButton;
    public Button[] optionButtons;

    [Header("Character Panel UI")]
    public Image Character_Icon;
    public Image CharacterBackground_Icon;
    public TMP_Text CharacterName_text;
    public TMP_Text CharacterInfo_text;

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

    public void ShowQuestion(QuizQuestionData q)
    {
        if (quizPanel) quizPanel.SetActive(true);
        if (questionText) questionText.text = q.question;
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < q.options.Length) { optionButtons[i].gameObject.SetActive(true); var t = optionButtons[i].GetComponentInChildren<TMP_Text>(); if (t) t.text = q.options[i]; }
            else optionButtons[i].gameObject.SetActive(false);
        }
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
            int gender = UnityEngine.Random.Range(0, 2);
            if(gender==0)
            {
                CharacterName_text.text = RandomInfo<string>(chars.FemaleCharacters_Names);
                Character_Icon.sprite = RandomInfo<Sprite>(chars.FemaleCharacters_Icons);
            }
            else
            {
                CharacterName_text.text = RandomInfo<string>(chars.MaleCharacters_Names);
                Character_Icon.sprite = RandomInfo<Sprite>(chars.MaleCharacters_Icons);
            }
            CharacterInfo_text.text = RandomInfo<string>(chars.JobDescription);
        }
    }

    public void UpdateTimerDisplay(float t) { if (timerText) timerText.text = $"Time: {t:F1}s"; }
    public void SetHintButtonState(bool active) { if (hintButton) hintButton.interactable = active; }
    public void RemoveOption(int index) { if (index >= 0 && index < optionButtons.Length) optionButtons[index].gameObject.SetActive(false); }
    public void HideQuizPannel() { if (quizPanel) quizPanel.SetActive(false); }
    public T RandomInfo<T>(List<T> list) { return list[UnityEngine.Random.Range(0, list.Count)]; }
    

}
