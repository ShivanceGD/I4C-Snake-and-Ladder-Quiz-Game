using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewQuizLevel", menuName = "Quiz/Create Level Quiz")]
public class QuizScriptableObject : ScriptableObject
{
    public List<QuizQuestionData> questions = new();
}