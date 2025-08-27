using System;
using System.Linq;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.Events;

public class OfflineGameFlowHandler : MonoBehaviour
{
    public SerializedDictionary<Player, PlayerGameData> AllPlayers;
    public Transform BoardParent, HomePoint;
    public LevelDataSO CurrentLevel;
    public int TotalPlayersToSpawn = 2;
    private GlobalColourManager colorManager;
    //public int CurrentTurnIndex=0;
    public event Action<PlayerGameData> OnTurnStartedAction; 
    
    private void Awake()
    {
        Color[] playerColors = { Color.red, Color.blue, Color.green, Color.yellow };
        colorManager = new GlobalColourManager(playerColors);
    }

    public void BootStrapLevel()
    {
        BootStrapBoard();
        BootStrapAllPlayers();
    }

    public void StartOfflineGame()
    {
        //Start Turn -> 0 index then ++ got whose index turn is it
        //Ask quiz from him (if player) Take moves (if cpu) -> Movement manager to move
        //Again ask for turn and cycle repeats
        if (AllPlayers.Count != 0 && !IsGameOver())
        {
            OnTurnStartedAction.Invoke(AllPlayers.ElementAt(TurnHandler.Instance.GetCurrentTurnIndex()).Value);
        }
        
        
    }

    #region Spawn/Register Players

    [ContextMenu("Create Players")]
    private void BootStrapAllPlayers()
    {
        // Instead of destroying all PlayerGameData, reuse existing ones if possible
        foreach (var kvp in AllPlayers)
        {
            kvp.Value.ResetGameState();
        }

        AllPlayers.Clear();
        SpawnAndRegisterPlayersOffline();
        SpawnAndRegisterCPUOffline();
        colorManager.Reset();
    }
    private void SpawnAndRegisterCPUOffline()
    {
        GameObject botCPU = Instantiate(CurrentLevel.CPUPrefab, HomePoint);
        botCPU.transform.name = "CPU";
        var player = botCPU.GetComponent<Player>();
        AllPlayers.Add(player, new PlayerGameData("CPU", PlayerType.CPU, Color.white));
    }
    private void SpawnAndRegisterPlayersOffline()
    {
        for (int i = 0; i < TotalPlayersToSpawn; i++)
        {
            GameObject playerObj = Instantiate(CurrentLevel.PlayerPrefab, HomePoint);
            playerObj.transform.name = "Player" + (i + 1);
            Color assignedColor = colorManager.GetUniqueColor();
            playerObj.GetComponent<SpriteRenderer>().color = assignedColor;

            var player = playerObj.GetComponent<Player>();
            AllPlayers.Add(player, new PlayerGameData("Player" + (i +1), PlayerType.Human, assignedColor));
        }
    }

    #endregion

    #region Spawning GameBoard and Numbers
    private void BootStrapBoard()
    {
        Instantiate(CurrentLevel.Board.BoardPrefab, BoardParent);
        BoardLogicManager.Instance.GenerateTilesPositionWithNumbers(CurrentLevel.Board.NumberToSpawnOnBoard, CurrentLevel.Board.BoardWidth, CurrentLevel.Board.BoardHeight
        );
    }
    #endregion
    
    //Todo
    public void UpdatePlayersAllGameData(Player player, string question = null, string answer = null, bool? isCorrect = null, int? newIndex = null, bool? finished = false, bool incrementMove = false)
    {
        if (AllPlayers.TryGetValue(player, out var data))
        {
            data.UpdatePlayersDataQuestionAndAnswer( data, question, answer);
            data.UpdatePlayersDataCorrectOrIncorrectCounter(data, isCorrect.Value);
            if(newIndex!=null){ data.UpdatePlayersDataIndexData(data,newIndex.Value); }
            if(incrementMove) { data.UpdatePlayersDataMovesCounter(data);}
            if(finished == true ) { data.UpdatePlayersDataMarkFinished(data, true);}
            
            //call all
        }
    }
    private bool IsGameOver()
    {
        int activePlayers = 0;

        foreach (var kvp in AllPlayers)
        {
            PlayerGameData data = kvp.Value;

            // If this player is not finished, they are still active
            if (!data.GetPlayerDataFinishedState(data))
            {
                activePlayers++;
            }
        }

        // Game over when <= 1 active player remains
        return activePlayers <= 1;
    }
#region Event Wiring
    public void EventWiring()
    {
            OnTurnStartedAction += (PlayerGameData) => HandleStartTurn(AllPlayers.ElementAt(TurnHandler.Instance.GetCurrentTurnIndex()).Value);
    }

    public void HandleStartTurn(PlayerGameData player)
    {
        if (player == null)
        {
            Debug.LogError("No Player Found To Start Turn");
            return;
        }

        if (player.PlayerType == PlayerType.Human)
        {
            //Start Quiz
        }
        else if (player.PlayerType == PlayerType.CPU)
        {
            //Start Random Movement
        }
        else Debug.LogError("No Player Type Found");
        
    }
#endregion
}

