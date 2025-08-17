using UnityEngine;

[System.Serializable]
public class QuizQuestionData
{
    [TextArea] public string question;
    public string[] options = new string[4];
    public int correctAnswerIndex;
    public Difficulty difficulty;
    public bool isHintAllowed;
    public float timeLimit;
    public bool isAnonymous;
    public string CharName;
    public string CharInfo;
    public bool isMale;
}

public enum Difficulty
{
    Easy,
    Medium,
    Hard
}
