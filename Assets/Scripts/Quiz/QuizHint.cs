using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(QuizManager))]
public class QuizHint : MonoBehaviour
{
    public void UseHint(int wrongAnswersToRemove, QuizQuestionData currentQuestion, PlayerManager currentPlayer,Button[] optionButtons, bool canUseHint, Action onHintUsed)
    {
        if (!canUseHint) return;

        int removed = 0;
        int attempts = 0;

        while (removed < wrongAnswersToRemove && attempts < 100)
        {
            int randIndex = UnityEngine.Random.Range(0, optionButtons.Length);
            if (randIndex != currentQuestion.correctAnswerIndex && optionButtons[randIndex].gameObject.activeSelf)
            {
                optionButtons[randIndex].gameObject.SetActive(false);
                removed++;
            }
            attempts++;
        }

        currentPlayer.UseHint();
        onHintUsed?.Invoke();
    }
}
