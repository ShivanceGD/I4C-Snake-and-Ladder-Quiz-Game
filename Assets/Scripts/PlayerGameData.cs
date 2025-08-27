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
    public PlayerGameData(string playerName, PlayerType playerType, Color assignedColor)
    {
        PlayerType = playerType;
        Name = playerName;
        Color = assignedColor;
        PlayerCurrentGameStateData = new PlayerGameStateData(); // Allocate once
    }

    public void ResetGameState()
    {
        PlayerCurrentGameStateData.CurrentIndex = 0;
        PlayerCurrentGameStateData.MovesCounter = 0;
        PlayerCurrentGameStateData.TotalCorrectAnswered = 0;
        PlayerCurrentGameStateData.TotalIncorrectAnswered = 0;
        PlayerCurrentGameStateData.IsFinished = false;
        PlayerCurrentGameStateData.QuestionsAndAnswers.Clear(); // ♻️ reuse instead of realloc
    }
    
    public void UpdatePlayersDataQuestionAndAnswer(PlayerGameData data, string Question, string Answer)
    {
        data.PlayerCurrentGameStateData.QuestionsAndAnswers[Question] = Answer;
    }

    public void UpdatePlayersDataCorrectOrIncorrectCounter(PlayerGameData data, bool isCorrect)
    {
        _ = isCorrect ? ++data.PlayerCurrentGameStateData.TotalCorrectAnswered : ++data.PlayerCurrentGameStateData.TotalIncorrectAnswered;
    }

    public void UpdatePlayersDataIndexData(PlayerGameData data, int newIndex)
    {
        data.PlayerCurrentGameStateData.CurrentIndex = newIndex;
    }

    public void UpdatePlayersDataMovesCounter(PlayerGameData data)
    {
        data.PlayerCurrentGameStateData.MovesCounter++;
    }

    public void UpdatePlayersDataMarkFinished(PlayerGameData data, bool finished)
    {
        data.PlayerCurrentGameStateData.IsFinished = finished;
    }

    public bool GetPlayerDataFinishedState(PlayerGameData data)
    {
        return data.PlayerCurrentGameStateData.IsFinished;
    }

    public int GetPlayerDataMoves(PlayerGameData data)
    {
        return data.PlayerCurrentGameStateData.MovesCounter;
    }

    public int GetPlayerDataTotalCorrectAnswered(PlayerGameData data)
    {
        return data.PlayerCurrentGameStateData.TotalCorrectAnswered;
    }

    public int GetPlayerDataTotalIncorrectAnswered(PlayerGameData data)
    {
        return data.PlayerCurrentGameStateData.TotalIncorrectAnswered;
    }

    public int GetPlayerDataCurrentIndex(PlayerGameData data)
    {
        return data.PlayerCurrentGameStateData.CurrentIndex;
    }

    public IEnumerable<KeyValuePair<string, string>> GetPlayerCurrentDataQuestionAndAnswers(PlayerGameData data)
    {
        foreach (var kvp in data.PlayerCurrentGameStateData.QuestionsAndAnswers)
        {
            yield return kvp;
        }
    }
}

[Serializable]
public class PlayerGameStateData
{
    public int CurrentIndex;
    public int MovesCounter;
    public int TotalCorrectAnswered, TotalIncorrectAnswered;
    public bool IsFinished;
   
    public SerializedDictionary<string, string> QuestionsAndAnswers = new(); // Allocated once and reused
}

[Serializable]
public enum PlayerType
{
    CPU, Human
}
