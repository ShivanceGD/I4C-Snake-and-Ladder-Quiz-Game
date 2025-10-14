using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using AYellowpaper.SerializedCollections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class OfflineFlowManager : MonoBehaviour,IFlowManager
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
    public QuizPackSO CurrentQuizPack;

    [Header("LeaderBoard References")]
    [SerializeField] private GameObject LeaderBoard;
    [SerializeField] private GameObject RankPrefab;
    [SerializeField] private Transform RankingTransform;

    [Header("Summary References")]
    [SerializeField] private GameObject SummaryPrefab;
    [SerializeField] private Transform SummaryTransform;

    // central player-state store
    public SerializedDictionary<Player, PlayerGameData> AllPlayers = new();

    [Header("Settings")]
    public int TotalPlayersToSpawn = 1;

    [Header("Player Colors (assigned in order)")]
    public Color[] PlayerColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };

    [Header("Player HUD References")]
    [SerializeField] private GameObject PlayerHudItem;
    [SerializeField] private Transform PlayerHudItemPanelTransform;
    [SerializeField] private TMP_Text InfoText;
    [SerializeField] private List<string> LadderTexts;
    [SerializeField] private List<string> SnakeTexts;
    
    [Header("GameOver Panel (Only Story Mode)")]
    [SerializeField] private GameObject GameOverPanel;
    [SerializeField] private List<GameObject> StarsInPanel;
    [SerializeField] private TMP_Text GameOverText;
    [SerializeField] private Button RetryButton;
    [SerializeField] private Button MenuButton;

    // store HUDs per player (not in PlayerData, just cached here)
    private Dictionary<Player, GameObject> playerHuds = new();
    private Player CurrentPlayer;
    //[SerializeField] private int MovesTaken;
    
    private async void Start()
    {
        GetLevelData();
        await BootstrapLevel();
        BootStrapAllPlayers();
    }

    private void GetLevelData()
    {
        TotalPlayersToSpawn = GameModeManager.Instance.NumberOfPlayersToBeSpawned;
        CurrentQuizPack = GameModeManager.Instance.QuizPack;
        if(GameModeManager.Instance.level != null) CurrentLevelData = GameModeManager.Instance.level;
    }

    public void StartTurn()
    {
        Destroy(HowToPlayPanel);
        StartCoroutine(StartFirstTurnNextFrame());
    }

    private IEnumerator StartFirstTurnNextFrame()
    {
        yield return null; // allow one frame for setup
        StartTurns();
    }

    private async Task BootstrapLevel()
    {
        HowToPlayPanel = Instantiate(HowToPlayPanelPrefab, CanvasTransform);
        GameObject.FindGameObjectWithTag("StartGameButton")
            .GetComponent<Button>().onClick.AddListener(StartTurn);
        GameObject.FindGameObjectWithTag("StartGameCloseButton").SetActive(false);

        // 1) Board
        if (CurrentLevelData.Board?.BoardPrefab != null)
        {
            Instantiate(CurrentLevelData.Board.BoardPrefab, boardParent);
            if (boardManager == null) boardManager = FindFirstObjectByType<BoardLogicManager>();
            await Task.Yield();

            BoardLogicManager.Instance.GenerateTilesPositionWithNumbers(
                CurrentLevelData.Board.NumberPrefabToSpawnOnBoard,
                CurrentLevelData.Board.BoardWidth,
                CurrentLevelData.Board.BoardHeight);
        }
        else Debug.LogWarning("[OfflineFlowManager] Board prefab missing in levelData.");

        FindingManagersInScene();

        // 3) Load quiz questions into QuizManager
        quizManager.LoadQuestions(CurrentQuizPack);
    }

    private void BootStrapAllPlayers()
    {
        // reset existing
        foreach (var kv in AllPlayers) kv.Value.ResetGameState();
        AllPlayers.Clear();
        playerHuds.Clear();

        int paletteLen = PlayerColors is { Length: > 0 } ? PlayerColors.Length : 0;

        // spawn human players
        SpawnHumanPlayerOffline(paletteLen);

        // spawn CPU if < 4
        if (TotalPlayersToSpawn < 4)
        {
            SpawnCPUPlayerOffline(paletteLen);
        }

        // register turn order
        var list = AllPlayers.Keys.ToList();
        offlineTurnHandler.RegisterPlayers(list);

        // create HUDs for all players
        InstantiatePlayerHuds();

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
            player.ApplyColor(assigned);
            player.Color = assigned;
            var data = new PlayerGameData(player.PlayerName, PlayerType.Human, assigned);
            AllPlayers.Add(player, data);
        }
    }

    // HUD instantiation logic
    private void InstantiatePlayerHuds()
    {
        int i = 1;
        playerHuds.Clear();
        foreach (var kv in AllPlayers)
        {
            Player p = kv.Key;
            GameObject hud = Instantiate(PlayerHudItem, PlayerHudItemPanelTransform);
            hud.name = $"{p.name}_HUD";

            // set player name text
            hud.transform.GetComponentInChildren<TMP_Text>().text = i.ToString();
            hud.transform.GetChild(3).GetComponent<TMP_Text>().text = p.IsCpu ? "CPU" : p.PlayerName;
            // set player color if UI has Image
            
            hud.transform.GetChild(2).GetComponentInChildren<Image>().color = kv.Value.Color;

            // ensure TurnIndicator starts off
            hud.transform.Find("TurnIndicator").gameObject.SetActive(false);

            playerHuds.Add(p, hud);
            i++;
        }
    }

    private void UpdateTurnIndicators(Player current)
    {
        CurrentPlayer = current;
        foreach (var kv in playerHuds)
        {
            Transform indicator = kv.Value.transform.Find("TurnIndicator");
            if (indicator != null)
                indicator.gameObject.SetActive(kv.Key == current);
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
        if (current == null)
        {
            Debug.LogWarning("[OfflineFlowManager] No current player determined.");
            return;
        }

        UpdateTurnIndicators(current);
        StartTurnForPlayer(current);
    }

    private void StartTurnForPlayer(Player p)
    {
        Debug.Log($"[OfflineFlowManager] StartTurn -> {p?.name}");
        if (p == null) return;

        UpdateTurnIndicators(p);

        if (p.IsCpu)
        {
            StartCoroutine(CpuSequence(p));
            return;
        }

        // Human: ask quiz manager for question
        int idx = quizManager.GetRandomQuestionIndex();
        var q = quizManager.GetQuestionByIndex(idx);
        bool canUseHint = q != null && q.isHintAllowed &&
                          AllPlayers.TryGetValue(p, out var pd) &&
                          !pd.PlayerCurrentGameStateData.IsFinished;
    
        quizManager.ShowQuizForPlayer(q, p, canUseHint, () => { }, (qr) =>
        {
            movementManager.ProcessPostQuizMovement(
                p, qr.IsCorrect,
                q != null ? q.questionsDifficulty : QuestionsDifficulty.Easy,
                qr.TimeTaken, CurrentLevelData.Board,
                CurrentLevelData.DiceRollRangePerQuizDifficulty, (mres) =>
                {
                    UpdatePlayerDataAfterMovement(p, q, qr, mres);
                    p.MovesTaken++;
                    if (mres.Finished)
                    {
                        if (AllPlayers.TryGetValue(p, out var d)) d.UpdatePlayersDataMarkFinished(true);
                        offlineTurnHandler.RemovePlayerFromTurn(p);
                    }

                    if (CheckForGameEnd())
                    {
                        ShowLeaderboard();
                        if (CurrentLevelData.GameMode == GameMode.StoryMode)
                        {
                            UpdateStarRating(p);
                        }
                        return;
                    }
                    offlineTurnHandler.EndTurn();
                    Player nxt = offlineTurnHandler.GetCurrentPlayer();
                    if (nxt != null) StartTurnForPlayer(nxt);
                });
        });
    }

    private void UpdateStarRating(Player p)
    {
        //Calculate stars and show
        int newStars = GetStarRating(p.MovesTaken, CurrentLevelData.TotalAvailableMoves, 3);
        Debug.Log("new Stars " + newStars);
        if (newStars > CurrentLevelData.LevelStars)
        {
            Debug.Log("Stars Updated");
            CurrentLevelData.LevelStars = newStars;
            Debug.Log(CurrentLevelData.LevelStars);
        }
        GameModeManager.Instance.SaveGameData();
    }

    private IEnumerator CpuSequence(Player cpu)
    {
        yield return new WaitForSeconds(0.5f);
        bool correct = Random.value > 0.2f;
        float t = Random.Range(2f, 8f);
        QuestionsDifficulty d = QuestionsDifficulty.Easy;

        movementManager.ProcessPostQuizMovement(cpu, correct, d, t,
            CurrentLevelData.Board, CurrentLevelData.DiceRollRangePerQuizDifficulty, (mres) =>
            {
                UpdatePlayerDataAfterMovement(cpu, null,
                    new QuizResult { IsCorrect = correct, SelectedIndex = -1, TimeTaken = t }, mres);

                if (mres.Finished)
                {
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
            string answer = (quizResult.SelectedIndex >= 0 && quizResult.SelectedIndex < question.options.Length)
                ? question.options[quizResult.SelectedIndex]
                : "";
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
        int notFinished = AllPlayers.Values.Count(x => !x.PlayerCurrentGameStateData.IsFinished);
        return notFinished <= 1;
    }

    private void ShowLeaderboard()
    {
        LeaderBoard.SetActive(true);
        Button ExitButton = LeaderBoard.transform.Find("Exit_Button").GetComponent<Button>();
        ExitButton.onClick.RemoveAllListeners();
        var ranking = AllPlayers
            .OrderByDescending(kv => kv.Value.PlayerCurrentGameStateData.CurrentIndex)
            .ThenByDescending(kv => kv.Value.PlayerCurrentGameStateData.TotalCorrectAnswered)
            .ToList();

        int rank = 1;
        foreach (var kv in ranking)
        {
            SetLeaderBoardRankings(rank, kv);
            SetSummaryData(kv);
            rank++;
        }
        if (CurrentLevelData.GameMode == GameMode.StoryMode)
        {
            ExitButton.onClick.AddListener(()=>
            {
                LeaderBoard.SetActive(false);
                ShowGameOverPanel();
            });
            // Winner is the first player in the ranking
            Player winner = ranking.First().Key;

            bool playerWon = !winner.IsCpu; // assume Human win if not CPU
            SetGameOverPanel(playerWon);
        }
        else
        {
            ExitButton.onClick.AddListener(()=>SwitchScene("New_Menu"));
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
        int index = 0;
        foreach (var qa in kv.Value.PlayerCurrentGameStateData.QuestionsAndAnswers)
        {
            GameObject summary = Instantiate(SummaryPrefab, SummaryTransform);
            summary.transform.GetChild(1).GetComponentInChildren<TMP_Text>().text = qa.Value;
            summary.transform.GetChild(2).GetComponent<TMP_Text>().text = (index + 1).ToString();
            summary.transform.GetChild(3).GetComponent<TMP_Text>().text = qa.Key;
            index++;
        }
    }

    private void ShowGameOverPanel()
    {
        GameOverPanel.SetActive(true);
    }
    private void SetGameOverPanel(bool playerWon)
    {
        //GameOverPanel.SetActive(true);

        if (playerWon)
        {
            GameOverText.text = "<color=blue>Level Completed!</color>   ";

            // Calculate stars
            int stars = CurrentLevelData.LevelStars;
            Debug.Log(stars);
            for (int i = 0; i < stars; i++)
            {
                Debug.Log("Star Active");
                StarsInPanel[i].SetActive(true);
            }

            // Show Next Level + Menu
            RetryButton.gameObject.SetActive(false);
            MenuButton.gameObject.SetActive(true);
        }
        else
        {
            GameOverText.text = "<color=red>Level Failed!</color>";
            
        
            // Hide stars on failure
            foreach (var star in StarsInPanel)
                star.SetActive(false);

            // Show Retry + Menu
            RetryButton.gameObject.SetActive(true);
            MenuButton.gameObject.SetActive(true);

            RetryButton.onClick.RemoveAllListeners();
            RetryButton.onClick.AddListener(() =>
            {
                LoadingSceneManager.Instance.LoadofflineScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                //SwitchScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            });

            MenuButton.onClick.RemoveAllListeners();
            MenuButton.onClick.AddListener(() =>
            {
                SwitchScene("MainMenu"); // replace with your menu scene
            });
        }
    }
    
    public void SwitchScene(string SceneName)
    {
        LoadingSceneManager.Instance.LoadofflineScene(SceneName);
        LoadingSceneManager.Instance.SetLoadingScreenMessage("Loading Menu");
    }

    public void PauseAndResumeGame(float f)
    {
        Time.timeScale = f;
    }
    /// <summary>
    /// Updates HUD with a combined string: Player Name/Your Turn + steps + snake/ladder info
    /// </summary>
    public void UpdateHUD( int stepsMoved = 0, bool snake = false, bool ladder = false)
    {
        string baseText = $"{CurrentPlayer.PlayerName}'s Turn ";

        // Add details
        string details = "";
        if (stepsMoved > 0) details += $"\n Moved {stepsMoved} steps.";
        if (ladder) details += $"\n <color=green>{LadderTexts[Random.Range(0,LadderTexts.Count)]}</color>";
        if (snake) details += $"\n <color=red>{SnakeTexts[Random.Range(0,SnakeTexts.Count)]}</color>";

        // Final HUD text
        InfoText.text = baseText + details; 
    }
    private int GetStarRating(int movesTaken, int maxMoves, int maxStars)
    {
        if (movesTaken <= 0)
            return maxStars;

        int bandSize = Mathf.CeilToInt(maxMoves / (float)maxStars);
        int band = (movesTaken - 1) / bandSize;
        int stars = maxStars - band;
        int stas = Mathf.Clamp(stars, 1, maxStars);
        Debug.Log(stas);
        return stas;
    }
}