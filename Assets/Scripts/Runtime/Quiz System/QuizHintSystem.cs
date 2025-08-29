using System.Collections.Generic;
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


