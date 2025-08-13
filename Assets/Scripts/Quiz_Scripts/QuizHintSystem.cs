using System.Collections.Generic;
using UnityEngine;

public class QuizHintSystem : MonoBehaviour
{
    public void RemoveWrongAnswers(int count, QuizQuestionData question, QuizUI ui)
    {
        if (ui == null || question == null) return;
        var indices = new List<int>();
        for (int i = 0; i < question.options.Length; i++) indices.Add(i);

        int removed = 0;
        var rnd = new System.Random();
        while (removed < count && indices.Count > 0)
        {
            int pick = rnd.Next(indices.Count);
            int idx = indices[pick];
            indices.RemoveAt(pick);
            if (idx != question.correctAnswerIndex)
            {
                ui.RemoveOption(idx);
                removed++;
            }
        }
    }
}
