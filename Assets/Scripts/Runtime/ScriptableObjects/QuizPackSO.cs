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
    public CharacterData characterData;
}

[System.Serializable]
public enum QuestionsDifficulty
{
    Easy,
    Medium,
    Hard
}

[System.Serializable]
public class CharacterData
{
    public string characterName;
    public string characterInfo;
    public CharacterGender characterGender;
}

[System.Serializable]
public enum CharacterGender
{
    Male,
    Female,
    Anonymous
}