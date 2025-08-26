using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.Serialization;


public class OfflineGameFlowHandler:MonoBehaviour
{
    public Transform BoardParent, HomePoint;
    public LevelDataSO CurrentLevel;
    //public List<OfflinePlayerCurrentData> Players;
    public int TotalPlayersToSpawn=2;
     public SerializedDictionary<PlayerManager, OfflinePlayerCurrentData> Players;
     public PlayerManager p;
     Color[] playerColors = { Color.red, Color.blue, Color.green, Color.yellow };
    public void BootStrapLevel()
    {
        BootStrapBoard();
        BootStrapAllPlayers();
    }
[ContextMenu("AddQeustionsandAnswers")]
    public void AddQuestionsAndAnswersToPlayer(string question,string answer,bool IsCorrect)
    {
        if (Players.TryGetValue(this.p, out var data))
        {
            data.offlineStateDataData.QuestionsAndAnswers.Add(question, answer);
            if (IsCorrect)
            {
                data.offlineStateDataData.TotalCorrectAnswered++;
            }
            else
            {
                data.offlineStateDataData.TotalIncorrectAnswered++;
            }
        }
        else
        {
            Debug.LogWarning("Player not found in dictionary!");
        }
    }

    public void UpdateIndexAndMovementData(int NewIndex)
    {
        if (Players.TryGetValue(this.p, out var data))
        {
            data.offlineStateDataData.CurrentIndex = NewIndex;
            data.offlineStateDataData.MovesCounter++;
        }
    }
    
    public void MarkFinished(bool finished)
    {
        if (Players.TryGetValue(this.p, out var data))
        {
            data.offlineStateDataData.IsFinished = finished;
        }
    }

   
    private void BootStrapBoard()
    {
        Instantiate(CurrentLevel.Board.BoardPrefab, BoardParent);
        BoardLogicManager.Instance.SpawnAndGenerateTilesWithNumbers(CurrentLevel.Board.NumberToSpawnOnBoard,CurrentLevel.Board.BoardWidth,CurrentLevel.Board.BoardHeight);
    }
[ContextMenu("Create Players")]
    private void BootStrapAllPlayers()
    {
        
        Players.Clear();
        
        for (int i = 0; i < TotalPlayersToSpawn; i++)
        {
            GameObject Player = Instantiate(CurrentLevel.PlayerPrefab, HomePoint);
            Color AssignedColor = playerColors[i % playerColors.Length];
            Player.GetComponent<SpriteRenderer>().color = AssignedColor;
            //Player.name = "Player" + i;
            Players.Add(Player.GetComponent<PlayerManager>(),new OfflinePlayerCurrentData("Player"+i,0,0,0,0,false,PlayerType.Human,AssignedColor));
        }
        GameObject BotCPU = Instantiate(CurrentLevel.CPUPrefab, HomePoint);
        Players.Add(BotCPU.GetComponent<PlayerManager>(),new OfflinePlayerCurrentData("CPU",0,0,0,0,false,PlayerType.CPU,Color.white));
        

        // Example: spawning 2 humans and 1 CPU
        /*var player1 = new OfflinePlayerCurrentData(null, "Me", 0, 0, 0, 0, false, PlayerType.Human);
        var player2 = new OfflinePlayerCurrentData(null, "Friend", 0, 0, 0, 0, false, PlayerType.Human);
        var cpu = new OfflinePlayerCurrentData(null, "CPU Bot", 0, 0, 0, 0, false, PlayerType.CPU);*/

        /*Players.Add(null,player1);
        Players.Add(null,player2);
        Players.Add(null,cpu);*/
    }
    
}
[Serializable]
public class OfflineGameStateData
{
    
    public int CurrentIndex;
    public int MovesCounter;
    public int TotalCorrectAnswered, TotalIncorrectAnswered;
    public bool IsFinished;
    public SerializedDictionary<string, string> QuestionsAndAnswers ;

}
[Serializable]
public class OfflinePlayerCurrentData
{
    //public PlayerManager Player;
    public PlayerType  PlayerType;
    public string Name;
    public Color color;
    public OfflineGameStateData offlineStateDataData;
    public OfflinePlayerCurrentData( string playerName,
        int currentIndex, int movesCounter, int totalCorrectAnswered,
        int totalIncorrectAnswered, bool isFinished, PlayerType playerType,Color AssignedColor)
    {
        
        PlayerType = playerType;
        Name = playerName;
        color = AssignedColor;
        offlineStateDataData = new OfflineGameStateData
        {
            CurrentIndex = currentIndex,
            MovesCounter = movesCounter,
            TotalCorrectAnswered = totalCorrectAnswered,
            TotalIncorrectAnswered = totalIncorrectAnswered,
            IsFinished = isFinished,
           QuestionsAndAnswers =  new()
           
            
        };
    }
    
   
     
}
[Serializable]
public enum PlayerType
{
    CPU,Human
}
