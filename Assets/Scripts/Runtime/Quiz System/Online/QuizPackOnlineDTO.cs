using System;
using System.Collections.Generic;

[Serializable]
public class QuizPackOnlineDTO
{
    public string packId;
    public string displayName;
    public string categoryName;
    public string description;
    public string version;
    public string updatedAtIso;
    public List<QuestionOnlineDTO> questions = new();
}

[Serializable]
public class QuestionOnlineDTO
{
    public string question;
    public string[] options = new string[4];
    public int correctAnswerIndex;
    public QuestionsDifficulty questionsDifficulty;
    public bool isHintAllowed;
    public float timeLimit;
    public CharacterOnlineDTO characterData = new();
}

[Serializable]
public class CharacterOnlineDTO
{
    public string characterName;
    public string characterInfo;
    public CharacterGender characterGender;
}
