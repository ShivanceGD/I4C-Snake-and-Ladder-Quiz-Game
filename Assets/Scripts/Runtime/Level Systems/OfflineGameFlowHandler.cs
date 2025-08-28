using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using AYellowpaper.SerializedCollections;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class OfflineFlowManager : MonoBehaviour
{
    [Header("References (assign in inspector)")]
    public LevelDataSO CurrentLevelData;
    public Transform boardParent;
    public Transform PlayerSpawnLocation;
    public MovementManager movementManager;
    public QuizManager quizManager;
    public OfflineTurnLogic offlineTurnHandler;
    public GameObject HowToPlayPanelPrefab;
    public Transform CanvasTransform;
    private GameObject HowToPlayPanel;
    

    // central player-state store
    public SerializedDictionary<Player, PlayerGameData> AllPlayers = new SerializedDictionary<Player, PlayerGameData>();

    [Header("Settings")]
    public int TotalPlayersToSpawn = 1;

    [Header("Player Colors (assigned in order)")]
    public Color[] PlayerColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };
    private void FindingManagersInScene()
    {
        if (movementManager == null) movementManager = FindFirstObjectByType<MovementManager>();
        if (quizManager == null) quizManager = FindFirstObjectByType<QuizManager>();
        if (offlineTurnHandler == null) offlineTurnHandler = FindFirstObjectByType<OfflineTurnLogic>();
    }

    private async void Start()
    {
        try
        {
            await BootstrapLevel();
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    [ContextMenu("StartLevel")]
    public void StartLevel()
    {
        Destroy(HowToPlayPanel);
        StartCoroutine(StartFirstTurnNextFrame());
    }
    
    private IEnumerator StartFirstTurnNextFrame()
    {
        // allow one frame for everything to settle (tile positions etc.)
        yield return null;
        StartTurns();
    }

    private async Task BootstrapLevel()
    {
        HowToPlayPanel = Instantiate(HowToPlayPanelPrefab,CanvasTransform);
        GameObject.FindGameObjectWithTag("StartGameButton").GetComponent<Button>().onClick.AddListener(StartLevel);
        FindingManagersInScene();
        // 1) Board
        if (CurrentLevelData.Board?.BoardPrefab != null)
        {
            
            Instantiate(CurrentLevelData.Board.BoardPrefab, boardParent);
            await Task.Yield();
            
            BoardLogicManager.Instance.GenerateTilesPositionWithNumbers(CurrentLevelData.Board.NumberPrefabToSpawnOnBoard, CurrentLevelData.Board.BoardWidth, CurrentLevelData.Board.BoardHeight);
        }
        else Debug.LogWarning("[OfflineFlowManager] Board prefab missing in levelData.");

        // 2) Spawn Players
        BootStrapAllPlayers();

        // 3) Load quiz questions into QuizManager
        quizManager.LoadQuestions(CurrentLevelData.LevelQuizSCO);
    }

    private void BootStrapAllPlayers()
    {
        // reset existing
        foreach (var kv in AllPlayers) kv.Value.ResetGameState();
        AllPlayers.Clear();

        int paletteLen = PlayerColors is { Length: > 0 } ? PlayerColors.Length : 0;

        // spawn human players
        SpawnHumanPlayerOffline(paletteLen);

        // spawn CPU
        SpawnCPUPlayerOffline(paletteLen);

        // register turn order
        var list = AllPlayers.Keys.ToList();
        offlineTurnHandler.RegisterPlayers(list);
        Debug.Log($"[OfflineFlowManager] Bootstrapped {AllPlayers.Count} players.");
    }

    private void SpawnCPUPlayerOffline(int paletteLen)
    {
        if (CurrentLevelData.CPUPrefab != null)
        {
            var bot = Instantiate(CurrentLevelData.CPUPrefab, PlayerSpawnLocation);
            bot.name = "CPU";
            var cpu = bot.GetComponent<Player>();
            Color cpuColor = paletteLen > 0 ? PlayerColors[TotalPlayersToSpawn % paletteLen] : cpu.Color;
            cpu.ApplyColor(cpuColor);
            cpu.Color = cpuColor;
            var cdata = new PlayerGameData("CPU", PlayerType.CPU, cpuColor);
            AllPlayers.Add(cpu, cdata);
            //cpu.transform.position = BoardLogicManager.GetTilePosition(0);
        }
    }

    private void SpawnHumanPlayerOffline(int paletteLen)
    {
        for (int i = 0; i < TotalPlayersToSpawn; i++)
        {
            var obj = Instantiate(CurrentLevelData.PlayerPrefab);
            obj.transform.position = PlayerSpawnLocation.position;
            obj.name = $"Player{i + 1}";
            var player = obj.GetComponent<Player>();
            Color assigned = paletteLen > 0 ? PlayerColors[i % paletteLen] : player.Color;
            player.ApplyColor(assigned); // apply color visually
            player.Color = assigned;
            var data = new PlayerGameData(player.PlayerName, PlayerType.Human, assigned);
            AllPlayers.Add(player, data);
            //player.transform.position = BoardLogicManager.GetTilePosition(0);
        }
    }

    private void StartTurns()
    {
        if (AllPlayers.Count == 0)
        {
            Debug.LogWarning("[OfflineFlowManager] No players found to start turns.");
            return;
        }

        Player current = offlineTurnHandler.GetCurrentPlayer();
        if (current == null) { Debug.LogWarning("[OfflineFlowManager] No current player determined."); return; }

        StartTurnForPlayer(current);
    }

    private void StartTurnForPlayer(Player p)
    {
        Debug.Log($"[OfflineFlowManager] StartTurn -> {p?.name}");
        if (p == null) return;

        // If CPU, auto-resolve
        if (p.IsCpu)
        {
            StartCoroutine(CpuSequence(p));
            return;
        }

        // Human: ask quiz manager for question
        int idx = quizManager.GetRandomQuestionIndex();
        var q = quizManager.GetQuestionByIndex(idx);
        bool canUseHint = q != null && q.isHintAllowed && AllPlayers.TryGetValue(p, out var pd) && !pd.PlayerCurrentGameStateData.IsFinished;

        quizManager.ShowQuizForPlayer(q, p, canUseHint, () =>
        {
            // hint used: if you track hint counts, do it here (no counter in PlayerGameData by default)
        }, (qr) =>
        {
            Debug.Log($"[OfflineFlowManager] Quiz result for {p.name}: correct={qr.IsCorrect} time={qr.TimeTaken} sel={qr.SelectedIndex}");
            movementManager.ProcessPostQuizMovement(p, qr.IsCorrect, q != null ? q.questionsDifficulty : QuestionsDifficulty.Easy, qr.TimeTaken, CurrentLevelData.Board, CurrentLevelData.DiceRollRangePerQuizDifficulty, (mres) =>
            {
                Debug.Log($"[OfflineFlowManager] MovementResult for {p.name}: finalIndex={mres.FinalTileIndex} reason={mres.Reason}");
                UpdatePlayerDataAfterMovement(p, q, qr, mres);

                if (mres.Finished)
                {
                    Debug.Log($"[OfflineFlowManager] {p.name} finished the game!");
                    // mark finished in data and remove from turn order
                    if (AllPlayers.TryGetValue(p, out var d)) d.UpdatePlayersDataMarkFinished(true);
                    offlineTurnHandler.RemovePlayerFromTurn(p);
                }

                // Check game end
                if (CheckForGameEnd())
                {
                    ShowLeaderboard();
                    return;
                }

                // continue to next player
                offlineTurnHandler.EndTurn();
                Player nxt = offlineTurnHandler.GetCurrentPlayer();
                if (nxt != null) StartTurnForPlayer(nxt);
                else Debug.Log("[OfflineFlowManager] No next player available.");
            });
        });
    }

    private IEnumerator CpuSequence(Player cpu)
    {
        yield return new WaitForSeconds(0.5f);
        bool correct = Random.value > 0.35f;
        float t = Random.Range(2f, 8f);
        QuestionsDifficulty d = QuestionsDifficulty.Easy;
        movementManager.ProcessPostQuizMovement(cpu, correct, d, t, CurrentLevelData.Board,
            CurrentLevelData.DiceRollRangePerQuizDifficulty, (mres) =>
            {
                UpdatePlayerDataAfterMovement(cpu, null,
                    new QuizResult { IsCorrect = correct, SelectedIndex = -1, TimeTaken = t }, mres);
                if (mres.Finished)
                {
                    Debug.Log($"[OfflineFlowManager] {cpu.name} finished.");
                    if (AllPlayers.TryGetValue(cpu, out var cd)) cd.UpdatePlayersDataMarkFinished(true);
                    offlineTurnHandler.RemovePlayerFromTurn(cpu);
                }

                if (CheckForGameEnd())
                {
                    ShowLeaderboard();
                    return;
                }

                offlineTurnHandler.EndTurn();
                Player nxt = offlineTurnHandler.GetCurrentPlayer();
                if (nxt != null) StartTurnForPlayer(nxt);
            });
    }

    private void UpdatePlayerDataAfterMovement(Player p, QuizQuestionData question, QuizResult quizResult, MovementResult movementResult)
    {
        if (!AllPlayers.TryGetValue(p, out var data)) return;

        if (question != null)
        {
            string answer = (quizResult.SelectedIndex >= 0 && quizResult.SelectedIndex < question.options.Length) ? question.options[quizResult.SelectedIndex] : "";
            data.UpdatePlayersDataQuestionAndAnswer(question.question, answer);
            data.UpdatePlayersDataCorrectOrIncorrectCounter(quizResult.IsCorrect);
        }

        if (movementResult.FinalTileIndex >= 0)
        {
            data.UpdatePlayersDataIndexData(movementResult.FinalTileIndex);
        }

        data.UpdatePlayersDataMovesCounter();
        if (movementResult.Finished) data.UpdatePlayersDataMarkFinished(true);
    }

    private bool CheckForGameEnd()
    {
        // Game ends when remaining players who are NOT finished are <= 1
        int notFinished = AllPlayers.Values.Count(x => !x.PlayerCurrentGameStateData.IsFinished);
        if (notFinished <= 1)
        {
            Debug.Log($"[OfflineFlowManager] Game end condition met. Not finished count: {notFinished}");
            return true;
        }
        return false;
    }

    private void ShowLeaderboard()
    {
        Debug.Log("[OfflineFlowManager] === Leaderboard ===");
        var ranking = AllPlayers.OrderByDescending(kv => kv.Value.PlayerCurrentGameStateData.CurrentIndex)
                                .ThenByDescending(kv => kv.Value.PlayerCurrentGameStateData.TotalCorrectAnswered)
                                .ToList();
        int rank = 1;
        foreach (var kv in ranking)
        {
            Debug.Log($"{rank}. {kv.Value.Name} - tile:{kv.Value.PlayerCurrentGameStateData.CurrentIndex} correct:{kv.Value.PlayerCurrentGameStateData.TotalCorrectAnswered}");
            rank++;
        }
    }
}
