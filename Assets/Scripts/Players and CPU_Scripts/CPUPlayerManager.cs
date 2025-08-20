using UnityEngine;

public class CPUPlayerManager : PlayerManager
{
    [Range(0f, 1f)]
    public float correctAnswerChance = 0.7f;

    // called by QuizManager when showing a quiz for this CPU player
    public void AnswerQuizAutomatically(QuizQuestionData q)
    {
        bool isCorrect = Random.value <= correctAnswerChance;
        float timeTaken = Random.Range(1f, q.timeLimit);
        // In singleplayer no Netcode: directly notify QuizManager.OnQuizCompleted
        QuizManager.Instance.OnQuizCompleted?.Invoke(this, isCorrect, q.difficulty, timeTaken);
        // If multiplayer, the server should simulate CPU actions server-side (spawn CPU on server).
    }
}
