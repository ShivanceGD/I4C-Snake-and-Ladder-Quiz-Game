using System;
using Unity.Collections;
using Unity.Netcode;
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
[Serializable]
public struct QuizQuestionSummary : INetworkSerializable
{
    public FixedString512Bytes Question;
    public FixedString128Bytes CorrectAnswer;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Question);
        serializer.SerializeValue(ref CorrectAnswer);
    }
}

public enum Difficulty
{
    Easy,
    Medium,
    Hard
}
