using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using AYellowpaper.SerializedCollections;
using TMPro;
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
    public BoardLogicManager boardManager;
    [Header("LeaderBoard References")]
    [SerializeField]private GameObject LeaderBoard;
    [SerializeField]private GameObject RankPrefab;
    [SerializeField]private Transform RankingTransform;

    [Header("Summary References")] [SerializeField]
    private GameObject SummaryPrefab;
    private Transform SummaryTransform;
    
    // central player-state store
    public SerializedDictionary<Player, PlayerGameData> AllPlayers = new();

    [Header("Settings")]
    public int TotalPlayersToSpawn = 1;

    [Header("Player Colors (assigned in order)")]
    public Color[] PlayerColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };

    private async void Start()
    {
        await BootstrapLevel();
        BootStrapAllPlayers();
    }
    public void StartTurn()
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
        GameObject.FindGameObjectWithTag("StartGameButton").GetComponent<Button>().onClick.AddListener(StartTurn);
        
        // 1) Board
        if (CurrentLevelData.Board?.BoardPrefab != null)
        {
            Instantiate(CurrentLevelData.Board.BoardPrefab, boardParent);
            if(boardManager == null) boardManager = FindFirstObjectByType<BoardLogicManager>();
            await Task.Yield();
            
            BoardLogicManager.Instance.GenerateTilesPositionWithNumbers(CurrentLevelData.Board.NumberPrefabToSpawnOnBoard, CurrentLevelData.Board.BoardWidth, CurrentLevelData.Board.BoardHeight);
        }
        else Debug.LogWarning("[OfflineFlowManager] Board prefab missing in levelData.");

        FindingManagersInScene();

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
    
    private void FindingManagersInScene()
    {
        if (movementManager == null) movementManager = FindFirstObjectByType<MovementManager>();
        if (quizManager == null) quizManager = FindFirstObjectByType<QuizManager>();
        if (offlineTurnHandler == null) offlineTurnHandler = FindFirstObjectByType<OfflineTurnLogic>();
        
    }
    
    private void SpawnCPUPlayerOffline(int paletteLen)
    {
        if (CurrentLevelData.CPUPrefab != null)
        {
            var bot = Instantiate(CurrentLevelData.CPUPrefab);
            bot.transform.position = boardManager.playerHouseLocation.position;
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
            obj.transform.position = boardManager.playerHouseLocation.position;
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
        bool correct = Random.value > 0.2;
        float t = Random.Range(2f, 8f);
        QuestionsDifficulty d = QuestionsDifficulty.Easy;
        movementManager.ProcessPostQuizMovement(cpu, correct, d, t, CurrentLevelData.Board, CurrentLevelData.DiceRollRangePerQuizDifficulty, (mres) =>
            {
                UpdatePlayerDataAfterMovement(cpu, null, new QuizResult { IsCorrect = correct, SelectedIndex = -1, TimeTaken = t }, mres);
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
        LeaderBoard.SetActive(true);
        Debug.Log("[OfflineFlowManager] === Leaderboard ===");
        var ranking = AllPlayers.OrderByDescending(kv => kv.Value.PlayerCurrentGameStateData.CurrentIndex).ThenByDescending(kv => kv.Value.PlayerCurrentGameStateData.TotalCorrectAnswered).ToList();
        int rank = 1;
        foreach (var kv in ranking)
        {
            SetLeaderBoardRankings(rank, kv);
            Debug.Log($"{rank}. {kv.Value.Name} - tile:{kv.Value.PlayerCurrentGameStateData.CurrentIndex} correct:{kv.Value.PlayerCurrentGameStateData.TotalCorrectAnswered}");
            rank++;
        }
    }

    private void SetLeaderBoardRankings(int rank, KeyValuePair<Player, PlayerGameData> kv)
    {
        GameObject ranks = Instantiate(RankPrefab, RankingTransform);
        ranks.transform.GetChild(4).GetChild(3).GetComponent<Image>().color = kv.Value.Color;
        ranks.transform.GetChild(0).GetComponent<TMP_Text>().text = kv.Value.Name;
        ranks.transform.GetChild(1).GetComponentInChildren<TMP_Text>().text = rank.ToString();
        ranks.transform.GetChild(2).GetComponent<TMP_Text>().text = kv.Value.PlayerCurrentGameStateData.TotalCorrectAnswered.ToString();
        ranks.transform.GetChild(3).GetComponent<TMP_Text>().text = kv.Value.PlayerCurrentGameStateData.CurrentIndex.ToString();
    }

    private void SetSummaryData(KeyValuePair<Player, PlayerGameData> kv)
    {
        int index = 1;
        foreach (var qa in kv.Value.PlayerCurrentGameStateData.QuestionsAndAnswers)
        {
            GameObject summary = Instantiate(SummaryPrefab, SummaryTransform);
            summary.transform.GetChild(0).GetComponent<TMP_Text>().text = index.ToString();
            summary.transform.GetChild(1).GetComponent<TMP_Text>().text = qa.Key;
            summary.transform.GetChild(2).GetComponent<TMP_Text>().text = qa.Value;
            index++;

        }
        //summary.transform.GetChild(0).GetComponent<TMP_Text>().text = kv.Value.PlayerCurrentGameStateData.QuestionsAndAnswers.Keys;
    }
    public void SwitchScene(string SceneName)
    {
        LoadingSceneManager.Instance.LoadScene(SceneName);
        LoadingSceneManager.Instance.SetLoadingScreenMessage("Loading Menu");
    }
    public void PauseAndResumeGame(float f)
    {
        Time.timeScale = f;
    }
}
