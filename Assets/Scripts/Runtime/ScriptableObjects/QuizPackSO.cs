using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Quiz Pack", menuName = "Shivance Games/Create Quiz Pack")]
public class QuizPackSO : ScriptableObject
{
    public List<QuizQuestionData> questions = new();
}

[System.Serializable]
public class QuizQuestionData
{
    [TextArea] public string question;
    public string[] options = new string[4];
    public int correctAnswerIndex;
    public QuestionsDifficulty questionsDifficulty;
    public bool isHintAllowed;
    public float timeLimit;
}

[System.Serializable]
public enum QuestionsDifficulty
{
    Easy,
    Medium,
    Hard
}