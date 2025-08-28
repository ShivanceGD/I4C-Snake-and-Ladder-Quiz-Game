using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

[Serializable]
public class PlayerGameData
{
    public PlayerType PlayerType;
    public string Name;
    public Color Color;
    public PlayerGameStateData PlayerCurrentGameStateData;

    public PlayerGameData() { PlayerCurrentGameStateData = new PlayerGameStateData(); }

    public PlayerGameData(string playerName, PlayerType playerType, Color assignedColor)
    {
        PlayerType = playerType;
        Name = playerName;
        Color = assignedColor;
        PlayerCurrentGameStateData = new PlayerGameStateData();
    }

    public void ResetGameState()
    {
        PlayerCurrentGameStateData.CurrentIndex = 0;
        PlayerCurrentGameStateData.MovesCounter = 0;
        PlayerCurrentGameStateData.TotalCorrectAnswered = 0;
        PlayerCurrentGameStateData.TotalIncorrectAnswered = 0;
        PlayerCurrentGameStateData.IsFinished = false;
        PlayerCurrentGameStateData.QuestionsAndAnswers.Clear();
    }

    public void UpdatePlayersDataQuestionAndAnswer(string Question, string Answer) => PlayerCurrentGameStateData.QuestionsAndAnswers[Question] = Answer;
    public void UpdatePlayersDataCorrectOrIncorrectCounter(bool isCorrect) { if (isCorrect) PlayerCurrentGameStateData.TotalCorrectAnswered++; else PlayerCurrentGameStateData.TotalIncorrectAnswered++; }
    public void UpdatePlayersDataIndexData(int newIndex) => PlayerCurrentGameStateData.CurrentIndex = newIndex;
    public void UpdatePlayersDataMovesCounter() => PlayerCurrentGameStateData.MovesCounter++;
    public void UpdatePlayersDataMarkFinished(bool finished) => PlayerCurrentGameStateData.IsFinished = finished;
    public bool GetPlayerDataFinishedState() => PlayerCurrentGameStateData.IsFinished;
}

[Serializable]
public class PlayerGameStateData
{
    public int CurrentIndex;
    public int MovesCounter;
    public int TotalCorrectAnswered;
    public int TotalIncorrectAnswered;
    public bool IsFinished;
    public SerializedDictionary<string, string> QuestionsAndAnswers = new();
}

[Serializable]
public enum PlayerType
{
    CPU, Human
}
