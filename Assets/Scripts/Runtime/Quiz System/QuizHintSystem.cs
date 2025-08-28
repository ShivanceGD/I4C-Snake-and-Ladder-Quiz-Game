/*
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

    public void RemoveWrongAnswersNew(int Count, int CorrectAnswerIndex,QuizUI quizUI,int TotalOptionsToRemove)
    {
        int OptionToBeRemoved;
        for (int i = 0; i < TotalOptionsToRemove; i++)
        {
            OptionToBeRemoved = Random.Range(0, Count);
            if (OptionToBeRemoved != CorrectAnswerIndex)
            {
                quizUI.RemoveOption(OptionToBeRemoved);;
            }

        }


    }
}
*/
using System;
using System.Collections.Generic;
using UnityEngine;

public class QuizHintSystem
{
    private System.Random random = new System.Random();

    /// <summary>
    /// Returns indices of wrong answers to hide.
    /// </summary>
    /// <param name="totalOptions">Total number of options in the question</param>
    /// <param name="correctIndex">Index of the correct option</param>
    /// <param name="removeCount">How many wrong options to remove</param>
    public List<int> GetHints(int totalOptions, int correctIndex, int removeCount)
    {
        var indices = new List<int>();
        for (int i = 0; i < totalOptions; i++)
        {
            if (i != correctIndex) // Only wrong answers are candidates
                indices.Add(i);
        }

        var removed = new List<int>();
        while (removed.Count < removeCount && indices.Count > 0)
        {
            int pick = random.Next(indices.Count);
            removed.Add(indices[pick]);
            indices.RemoveAt(pick);
        }

        return removed;
    }
}


