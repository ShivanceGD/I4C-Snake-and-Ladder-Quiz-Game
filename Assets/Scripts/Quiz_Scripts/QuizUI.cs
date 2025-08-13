using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizUI : MonoBehaviour
{
    public GameObject quizPanel;
    public TMP_Text questionText;
    public TMP_Text timerText;
    public Button hintButton;
    public Button[] optionButtons;

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

    public void UpdateTimerDisplay(float t) { if (timerText) timerText.text = $"Time: {t:F1}s"; }
    public void SetHintButtonState(bool active) { if (hintButton) hintButton.interactable = active; }
    public void RemoveOption(int index) { if (index >= 0 && index < optionButtons.Length) optionButtons[index].gameObject.SetActive(false); }
    public void HideQuizPannel() { if (quizPanel) quizPanel.SetActive(false); }
}
