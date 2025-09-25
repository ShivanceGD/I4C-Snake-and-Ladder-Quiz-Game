/*using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AYellowpaper.SerializedCollections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MultiplayerFlowManager : NetworkBehaviour, IFlowManager
{
    [Header("References (assign in inspector)")]
    public LevelDataSO CurrentLevelData;

    public Transform boardParent;
    public Transform PlayerSpawnLocation;
    public MovementManager movementManager;
    public QuizManager quizManager;
    public OnlineTurnLogic onlineTurnHandler;
    public GameObject HowToPlayPanelPrefab;
    public Transform CanvasTransform;
    private GameObject HowToPlayPanel;
    public BoardLogicManager BoardLogicManager;
    public BoardDataSO currentBoardData;
    public QuizPackSO currentQuizPack;

    [Header("LeaderBoard References")] [SerializeField]
    private GameObject LeaderBoard;

    [SerializeField] private GameObject RankPrefab;
    [SerializeField] private Transform RankingTransform;

    [Header("Summary References")] [SerializeField]
    private GameObject SummaryPrefab;

    [SerializeField] private Transform SummaryTransform;

    [Header("Hud References")] [SerializeField]
    private GameObject PlayerHudItem;

    [SerializeField] private Transform PlayerHudItemPanelTransform;

    [SerializeField] private TMP_Text Info_Text;

    // centralized data (server-authoritative)
    public SerializedDictionary<Player, PlayerGameData> AllPlayers = new();
    [SerializeField] private Dictionary<Player, GameObject> playerHuds = new();
    private Player CurrentPlayer;
    [Header("Colors")] public Color[] PlayerColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };

    [SerializeField] private List<string> LadderTexts;
    [SerializeField] private List<string> SnakeTexts;

    [Header("Level and Quiz Data")]
    [SerializeField] private List<BoardDataSO> boardManagers;
    [SerializeField] private List<QuizPackSO> quizPacks;

    [Header("Ranking References")]
    [SerializeField] private int pointsPerCorrect;
    [SerializeField] private int penaltyPerMove;
    [SerializeField] private int WinnerScore;

    // Networked indices (server authoritative) -> synced automatically to late joiners
    public NetworkVariable<int> CurrentBoardIndex = new NetworkVariable<int>(-1);
    public NetworkVariable<int> CurrentQuizIndex = new NetworkVariable<int>(-1);

    // Networked current turn owner (owner client id) so clients know whose turn it is
    public NetworkVariable<ulong> CurrentTurnOwner = new NetworkVariable<ulong>(0);

    private void Awake()
    {
        // nothing here for now
    }

    private void OnEnable()
    {
        // Register NetworkVariable change handlers
        CurrentBoardIndex.OnValueChanged += OnBoardIndexChanged;
        CurrentQuizIndex.OnValueChanged += OnQuizIndexChanged;
        CurrentTurnOwner.OnValueChanged += OnCurrentTurnOwnerChanged;

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDisable()
    {
        CurrentBoardIndex.OnValueChanged -= OnBoardIndexChanged;
        CurrentQuizIndex.OnValueChanged -= OnQuizIndexChanged;
        CurrentTurnOwner.OnValueChanged -= OnCurrentTurnOwnerChanged;

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void Start()
    {
        // Attempt to find managers early
        FindingManagersInScene();

        // Ensure clients create HUDs for already spawned players (useful for late joiners)
        CreateClientHudsForExistingPlayers();

        // If the NetworkVariables were already set before this client started,
        // apply them now (OnValueChanged won't fire for existing value until it changes in some versions)
        if (CurrentBoardIndex.Value >= 0)
            ApplyBoard(CurrentBoardIndex.Value);

        if (CurrentQuizIndex.Value >= 0)
            ApplyQuiz(CurrentQuizIndex.Value);

        if (CurrentTurnOwner.Value != 0)
            OnCurrentTurnOwnerChanged(0, CurrentTurnOwner.Value);

        // reposition existing players (if any)
        foreach (var player in AllPlayers.Keys)
        {
            if (player != null)
                player.transform.position = PlayerSpawnLocation.position;
        }

        if (IsServer)
        {
            // Server picks a random board/quiz when starting the session
            SelectRandomBoardAndQuizServerRpc();
        }

        // Run level bootstrap - note: board/quiz will be applied when the indices are synced
        _ = BootstrapLevel();
    }

    private void OnClientConnected(ulong clientId)
    {
        // NetworkVariables sync automatically to late joiners - HUD creation handled via UpdatePlayerHudClientRpc
        // Also create HUDs for any existing local Player objects (defensive)
        CreateClientHudsForExistingPlayers();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SelectRandomBoardAndQuizServerRpc()
    {
        if (boardManagers == null || boardManagers.Count == 0 || quizPacks == null || quizPacks.Count == 0)
        {
            Debug.LogError("[MultiplayerFlowManager] SelectRandomBoardAndQuizServerRpc: No boards or quizzes configured on server!");
            return;
        }

        int boardIndex = Random.Range(0, boardManagers.Count);
        int quizIndex = Random.Range(0, quizPacks.Count);

        // Set networked indices — clients will receive updates automatically
        CurrentBoardIndex.Value = boardIndex;
        CurrentQuizIndex.Value = quizIndex;

        // Apply on server/host immediately
        ApplyBoard(boardIndex);
        ApplyQuiz(quizIndex);
    }

    // Called when CurrentBoardIndex changes (server -> clients)
    private void OnBoardIndexChanged(int oldIndex, int newIndex)
    {
        ApplyBoard(newIndex);
    }

    // Called when CurrentQuizIndex changes (server -> clients)
    private void OnQuizIndexChanged(int oldIndex, int newIndex)
    {
        ApplyQuiz(newIndex);
    }

    // Called when current turn owner changes (server -> clients)
    private void OnCurrentTurnOwnerChanged(ulong oldOwner, ulong newOwner)
    {
        // find player locally by OwnerClientId
        if (newOwner == 0)
        {
            CurrentPlayer = null;
            UpdateTurnIndicators(null);
            UpdateHUD();
            return;
        }

        var localPlayer = FindObjectsOfType<Player>().FirstOrDefault(p => p.OwnerClientId == newOwner);
        if (localPlayer != null)
        {
            CurrentPlayer = localPlayer;
            UpdateTurnIndicators(CurrentPlayer);
            UpdateHUD();
        }
        else
        {
            // Possibly this client hasn't spawned the Player object yet — log for debugging
            Debug.Log($"[MultiplayerFlowManager] OnCurrentTurnOwnerChanged: couldn't find Player with owner {newOwner} on this client.");
        }
    }

    private void ApplyBoard(int boardIndex)
    {
        // defensive checks
        if (boardManagers == null)
        {
            Debug.LogError("[MultiplayerFlowManager] ApplyBoard: boardManagers list is null!");
            return;
        }

        if (boardIndex < 0 || boardIndex >= boardManagers.Count)
        {
            Debug.LogError($"[MultiplayerFlowManager] ApplyBoard: boardIndex {boardIndex} out of range (0..{boardManagers.Count - 1})");
            return;
        }

        currentBoardData = boardManagers[boardIndex];
        Debug.Log($"[MultiplayerFlowManager] Applying board index {boardIndex}: {currentBoardData?.name}");

        if (currentBoardData?.BoardPrefab != null && boardParent != null)
        {
            // optional: clear previous board instances (uncomment if you want single instance)
            // foreach (Transform t in boardParent) Destroy(t.gameObject);

            Instantiate(currentBoardData.BoardPrefab, boardParent);
            if (BoardLogicManager == null) BoardLogicManager = FindFirstObjectByType<BoardLogicManager>();
            if (BoardLogicManager != null)
            {
                try
                {
                    BoardLogicManager.Instance.GenerateTilesPositionWithNumbers(currentBoardData.NumberPrefabToSpawnOnBoard, currentBoardData.BoardWidth, currentBoardData.BoardHeight);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MultiplayerFlowManager] Error generating tiles: {ex}");
                }
            }
            else
            {
                Debug.LogWarning("[MultiplayerFlowManager] BoardLogicManager not found when applying board.");
            }
        }
    }

    private void ApplyQuiz(int quizIndex)
    {
        if (quizPacks == null)
        {
            Debug.LogError("[MultiplayerFlowManager] ApplyQuiz: quizPacks list is null!");
            return;
        }

        if (quizIndex < 0 || quizIndex >= quizPacks.Count)
        {
            Debug.LogError($"[MultiplayerFlowManager] ApplyQuiz: quizIndex {quizIndex} out of range (0..{quizPacks.Count - 1})");
            return;
        }

        currentQuizPack = quizPacks[quizIndex];
        Debug.Log($"[MultiplayerFlowManager] Applying quiz index {quizIndex}: {currentQuizPack?.name}");

        FindingManagersInScene();
        if (quizManager != null && currentQuizPack != null)
        {
            // load questions for this client
            quizManager.LoadQuestions(currentQuizPack);
        }
        else
        {
            Debug.LogWarning("[MultiplayerFlowManager] quizManager is null or currentQuizPack is null in ApplyQuiz.");
        }
    }

    private void FindingManagersInScene()
    {
        if (movementManager == null) movementManager = FindFirstObjectByType<MovementManager>();
        if (quizManager == null) quizManager = FindFirstObjectByType<QuizManager>();
        if (onlineTurnHandler == null) onlineTurnHandler = FindFirstObjectByType<OnlineTurnLogic>();
        if (BoardLogicManager == null) BoardLogicManager = FindFirstObjectByType<BoardLogicManager>();
    }

    [ContextMenu("Start Game Online")]
    public async void StartGame()
    {
        foreach (var player in AllPlayers.Keys)
        {
            if (player != null)
                player.transform.position = PlayerSpawnLocation.position;
        }
        if (IsServer)
        {
            StartCoroutine(StartFirstTurnNextFrame());
        }
    }

    private IEnumerator StartFirstTurnNextFrame()
    {
        yield return null;
        if (IsServer) StartTurns();
    }

    [ContextMenu("Spawn Board")]
    public async Task BootstrapLevel()
    {
        HowToPlayPanel = Instantiate(HowToPlayPanelPrefab, CanvasTransform);

        if (IsHost) // Host
        {
            Debug.Log("Is Server (Host)");
            var startButton = GameObject.FindGameObjectWithTag("StartGameButton")?.GetComponent<Button>();
            if (startButton != null)
            {
                startButton.onClick.AddListener(() =>
                {
                    StartGame();
                    Destroy(HowToPlayPanel);
                });
            }

            var closeObj = GameObject.FindGameObjectWithTag("StartGameCloseButton");
            if (closeObj != null) closeObj.SetActive(false);
        }
        else // Client
        {
            Debug.Log("Is Client");
            var startObj = GameObject.FindGameObjectWithTag("StartGameButton");
            if (startObj != null) startObj.SetActive(false);

            var closeBtnObj = GameObject.FindGameObjectWithTag("StartGameCloseButton");
            if (closeBtnObj != null)
            {
                var closeBtn = closeBtnObj.GetComponent<Button>();
                if (closeBtn != null) closeBtn.onClick.AddListener(() => Destroy(HowToPlayPanel));
            }
        }

        // If currentBoardData is already set (via NetworkVariable), instantiate board here as well
        if (currentBoardData != null && currentBoardData.BoardPrefab != null)
        {
            Instantiate(currentBoardData.BoardPrefab, boardParent);
            if (BoardLogicManager == null) BoardLogicManager = FindFirstObjectByType<BoardLogicManager>();
            await Task.Yield();
            if (BoardLogicManager != null)
            {
                BoardLogicManager.Instance.GenerateTilesPositionWithNumbers(currentBoardData.NumberPrefabToSpawnOnBoard, currentBoardData.BoardWidth, currentBoardData.BoardHeight);
            }
        }
        else
        {
            Debug.LogWarning("[MultiplayerFlowManager] BootstrapLevel: Board prefab missing or currentBoardData not yet applied.");
        }

        FindingManagersInScene();

        RegisterAllNetworkPlayers();

        // Load questions if quiz was already applied
        if (quizManager != null && currentQuizPack != null)
            quizManager.LoadQuestions(currentQuizPack);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RegisterPlayerServerRpc(ulong clientId, ulong networkObjectId)
    {
        var playerObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId].GetComponent<Player>();
        if (playerObj == null) return;

        Color c = PlayerColors[AllPlayers.Count % PlayerColors.Length];
        playerObj.ApplyColor(c);
        playerObj.Color = c;

        var data = new PlayerGameData(playerObj.PlayerName, playerObj.IsCpu ? PlayerType.CPU : PlayerType.Human, c);

        if (!AllPlayers.ContainsKey(playerObj))
        {
            AllPlayers[playerObj] = data;
        }

        onlineTurnHandler.RegisterPlayers(AllPlayers.Keys.ToList());

        // Tell all clients to refresh their HUDs
        UpdatePlayerHudClientRpc(playerObj.NetworkObjectId, playerObj.PlayerName, c);
    }

    [ClientRpc]
    private void UpdatePlayerHudClientRpc(ulong playerNetId, string playerName, Color color)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetId, out var netObj))
        {
            Debug.LogWarning($"[MultiplayerFlowManager] UpdatePlayerHudClientRpc: player net object {playerNetId} not found on client.");
            return;
        }

        var playerObj = netObj.GetComponent<Player>();
        if (playerObj == null)
        {
            Debug.LogWarning($"[MultiplayerFlowManager] UpdatePlayerHudClientRpc: Player component missing on net object {playerNetId}.");
            return;
        }

        if (!playerHuds.ContainsKey(playerObj))
        {
            GameObject hud = Instantiate(PlayerHudItem, PlayerHudItemPanelTransform);
            hud.name = $"{playerName}_HUD";

            // If your HUD expects a numeric index in a child, you'll need to decide how to set it.
            var nameText = hud.GetComponentInChildren<TMP_Text>();
            if (nameText != null) nameText.text = playerName;

            var img = hud.transform.GetChild(2).GetComponentInChildren<Image>();
            if (img != null) img.color = color;

            var turnIndicator = hud.transform.Find("TurnIndicator");
            if (turnIndicator != null) turnIndicator.gameObject.SetActive(false);

            playerHuds.Add(playerObj, hud);
        }
    }

    private void RegisterAllNetworkPlayers()
    {
        AllPlayers.Clear();

        var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        int idx = 0;
        foreach (var p in players)
        {
            Color c = (PlayerColors != null && PlayerColors.Length > 0) ? PlayerColors[idx % PlayerColors.Length] : p.Color;
            p.ApplyColor(c);
            p.Color = c;

            var data = new PlayerGameData(p.PlayerName, p.IsCpu ? PlayerType.CPU : PlayerType.Human, c);
            AllPlayers[p] = data;
            idx++;
        }

        onlineTurnHandler.RegisterPlayers(AllPlayers.Keys.ToList());
        // HUDs are created via UpdatePlayerHudClientRpc when players register,
        // but ensure local clients create HUDs for already-spawned players as well.
        CreateClientHudsForExistingPlayers();
    }

    private void StartTurns()
    {
        if (!IsServer || AllPlayers.Count == 0) return;
        Player current = onlineTurnHandler.GetCurrentPlayer();
        if (current != null) StartTurnForPlayer(current);
        UpdateTurnIndicators(current);
    }

    private void StartTurnForPlayer(Player p)
    {
        if (p == null) return;
        UpdateTurnIndicators(p);
        if (p.IsCpu)
        {
            //StartCoroutine(CpuSequence(p));
            return;
        }

        if (quizManager == null)
        {
            Debug.LogError("[MultiplayerFlowManager] StartTurnForPlayer: quizManager not ready or no questions.");
            return;
        }

        // set the current turn owner network variable so clients can update UI
        CurrentTurnOwner.Value = p.OwnerClientId;

        int idx = quizManager.GetRandomQuestionIndex();
        var q = quizManager.GetQuestionByIndex(idx);
        bool canUseHint = q != null && q.isHintAllowed;

        // tell only the owner client to show quiz
        ShowQuizClientRpc(idx, p.OwnerClientId, canUseHint);
    }

    [ClientRpc]
    private void ShowQuizClientRpc(int questionIdx, ulong targetClient, bool canUseHint, ClientRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClient) return;

        if (quizManager == null)
        {
            Debug.LogError("[MultiplayerFlowManager] ShowQuizClientRpc: quizManager is null on client.");
            return;
        }

        var q = quizManager.GetQuestionByIndex(questionIdx);
        int count = -1;
        try
        {
            // try to get questions count if API exists
            count = currentQuizPack.questions.Count;
        }
        catch
        {
            // ignore if method doesn't exist
        }

        if (q == null)
        {
            Debug.LogError($"[MultiplayerFlowManager] ShowQuizClientRpc: question is null for index {questionIdx}. quizManager loaded? true questions count: {count}");
            return;
        }

        var localPlayer = FindObjectsOfType<Player>().FirstOrDefault(p => p.OwnerClientId == targetClient);
        quizManager.ShowQuizForPlayer(q, localPlayer, canUseHint,
            () => { }, // onHint
            (qr) =>
            {
                SubmitQuizResultServerRpc(targetClient, questionIdx, qr.IsCorrect, qr.TimeTaken, qr.SelectedIndex);
            });
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitQuizResultServerRpc(ulong clientId, int qIdx, bool isCorrect, float time, int selectedIdx)
    {
        var player = AllPlayers.Keys.FirstOrDefault(p => p.OwnerClientId == clientId);
        if (player == null) return;

        var q = quizManager.GetQuestionByIndex(qIdx);
        var qr = new QuizResult { IsCorrect = isCorrect, TimeTaken = time, SelectedIndex = selectedIdx };

        movementManager.ProcessPostQuizMovement(player, isCorrect, q.questionsDifficulty, time, CurrentLevelData.Board, CurrentLevelData.DiceRollRangePerQuizDifficulty, (mres) =>
        {
            UpdatePlayerDataAfterMovement(player, q, qr, mres);

            if (mres.Finished)
            {
                if (AllPlayers.TryGetValue(player, out var d)) d.UpdatePlayersDataMarkFinished(true);
                onlineTurnHandler.RemovePlayerFromTurn(player);
            }

            if (CheckForGameEnd())
            {
                BroadcastLeaderboardClientRpc();
                return;
            }

            onlineTurnHandler.EndTurn();
            Player nxt = onlineTurnHandler.GetCurrentPlayer();
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
        int notFinished = AllPlayers.Values.Count(x => !x.PlayerCurrentGameStateData.IsFinished);
        return notFinished <= 1;
    }

    [ClientRpc]
    private void BroadcastLeaderboardClientRpc()
    {
        LeaderBoard.SetActive(true);
        Debug.Log("[MultiplayerFlowManager] === Leaderboard ===");
        var ranking = AllPlayers.OrderByDescending(kv => kv.Value.PlayerCurrentGameStateData.CurrentIndex).ThenByDescending(kv => kv.Value.PlayerCurrentGameStateData.TotalCorrectAnswered).ToList();
        int rank = 1;
        foreach (var kv in ranking)
        {
            SetLeaderBoardRankings(rank, kv);
            SetSummaryData(kv);
            Debug.Log($"{rank}. {kv.Value.Name} - tile:{kv.Value.PlayerCurrentGameStateData.CurrentIndex} correct:{kv.Value.PlayerCurrentGameStateData.TotalCorrectAnswered}");
            rank++;
            if (!GameModeManager.Instance.IsPrivateRoom)
            {
                int correct = kv.Value.PlayerCurrentGameStateData.TotalCorrectAnswered;
                int moves = kv.Value.PlayerCurrentGameStateData.MovesCounter;

                // Score formula
                CalculateAndUpdatePoints(correct, moves, rank == 1);
            }
        }
    }

    private void CalculateAndUpdatePoints(int correct, int moves, bool isWinner)
    {
        int score = (correct * pointsPerCorrect) - (moves * penaltyPerMove);
        if (score < 0) score = 0; // prevent negatives
        if (isWinner) score += WinnerScore;
        Leaderboard.Instance.AddScore(score);
        Debug.Log($"[Leaderboard] Submitted score {score} ");
    }

    /// PlayerHuds ///
    private void InstantiatePlayerHuds()
    {
        int i = 1;
        foreach (var kv in AllPlayers)
        {
            Player p = kv.Key;
            if (playerHuds.ContainsKey(p)) continue;
        GameObject hud = Instantiate(PlayerHudItem, PlayerHudItemPanelTransform);
        hud.name = $"{p.PlayerName}'s_HUD";

        // set player name text
        hud.transform.GetComponentInChildren<TMP_Text>().text = i.ToString();
        hud.transform.GetChild(3).GetComponent<TMP_Text>().text = p.PlayerName;
        // set player color if UI has Image
        hud.transform.GetChild(2).GetComponentInChildren<Image>().color = kv.Value.Color;

        // ensure TurnIndicator starts off
        var turnIndicator = hud.transform.Find("TurnIndicator");
        if (turnIndicator != null) turnIndicator.gameObject.SetActive(false);

        playerHuds.Add(p, hud);
        i++;
    }
}

// Create HUDs on clients for players that are already spawned locally (useful for late joiners)
private void CreateClientHudsForExistingPlayers()
{
    var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
    int i = 1;
    foreach (var p in players)
    {
        if (p == null) continue;
        if (playerHuds.ContainsKey(p)) { i++; continue; }

        GameObject hud = Instantiate(PlayerHudItem, PlayerHudItemPanelTransform);
        hud.name = $"{p.PlayerName}_HUD_local";

        var nameText = hud.GetComponentInChildren<TMP_Text>();
        if (nameText != null) nameText.text = p.PlayerName;

        // attempt to use Player.Color if available
        try
        {
            var img = hud.transform.GetChild(2).GetComponentInChildren<Image>();
            if (img != null) img.color = p.Color;
        }
        catch { }

        var turnIndicator = hud.transform.Find("TurnIndicator");
        if (turnIndicator != null) turnIndicator.gameObject.SetActive(false);

        playerHuds[p] = hud;
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

public void UpdateHUD(int stepsMoved = 0, bool snake = false, bool ladder = false)
{
    if (Info_Text == null) return;

    if (CurrentPlayer == null)
    {
        Info_Text.text = "";
        return;
    }

    // Base string = player's turn
    string baseText = (CurrentPlayer.OwnerClientId == NetworkManager.Singleton.LocalClientId)
        ? "Your Turn"
        : $"{CurrentPlayer.PlayerName}'s Turn";

    // Add details
    string details = "";
    if (stepsMoved > 0) details += $" Moved {stepsMoved} steps.";
    if (ladder && LadderTexts != null && LadderTexts.Count > 0) details += $"\n <color=green>{LadderTexts[Random.Range(0, LadderTexts.Count)]}</color>";
    if (snake && SnakeTexts != null && SnakeTexts.Count > 0) details += $"\n <color=red>{SnakeTexts[Random.Range(0, SnakeTexts.Count)]}</color>";
    Info_Text.text = baseText + details;
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
        summary.transform.GetChild(2).GetComponent<TMP_Text>().text = index + 1.ToString();
        summary.transform.GetChild(3).GetComponent<TMP_Text>().text = qa.Key;
        index++;
    }
}

public void SwitchScene(string SceneName)
{
    LoadingSceneManager.Instance.LoadofflineScene(SceneName);
    LoadingSceneManager.Instance.SetLoadingScreenMessage("Loading Menu");
}
}*/




using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AYellowpaper.SerializedCollections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MultiplayerFlowManager : NetworkBehaviour, IFlowManager
{
    [Header("References (assign in inspector)")]
    public LevelDataSO CurrentLevelData;

    public Transform boardParent;
    public Transform PlayerSpawnLocation;
    public MovementManager movementManager;
    public QuizManager quizManager;
    public OnlineTurnLogic onlineTurnHandler;
    public GameObject HowToPlayPanelPrefab;
    public Transform CanvasTransform;
    private GameObject HowToPlayPanel;
    public BoardLogicManager BoardLogicManager;
    public BoardDataSO currentBoardData;
    public QuizPackSO currentQuizPack;

    [Header("LeaderBoard References")] [SerializeField]
    private GameObject LeaderBoard;

    [SerializeField] private GameObject RankPrefab;
    [SerializeField] private Transform RankingTransform;

    [Header("Summary References")] [SerializeField]
    private GameObject SummaryPrefab;

    [SerializeField] private Transform SummaryTransform;

    [Header("Hud References")] [SerializeField]
    private GameObject PlayerHudItem;

    [SerializeField] private Transform PlayerHudItemPanelTransform;

    [SerializeField] private TMP_Text Info_Text;

    // centralized data (server-authoritative)
    public SerializedDictionary<Player, PlayerGameData> AllPlayers = new();
    [SerializeField] private Dictionary<Player, GameObject> playerHuds = new();
    private Player CurrentPlayer;
    [Header("Colors")] public Color[] PlayerColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };

    [SerializeField] private List<string> LadderTexts;
    [SerializeField] private List<string> SnakeTexts;

    [Header("Level and Quiz Data")] 
    [SerializeField] private List<BoardDataSO> boardManagers;
    [SerializeField] private List<QuizPackSO> quizPacks;

    [Header("Ranking References")] 
    [SerializeField] private int pointsPerCorrect;
    [SerializeField] private int penaltyPerMove;
    [SerializeField] private int WinnerScore;
    
    public NetworkVariable<int> CurrentBoardIndex = new(-1);
    public NetworkVariable<int> CurrentQuizIndex = new(-1);
    private void Awake()
    {
        //BootstrapLevel();
    }

    private void Start()
    {
        foreach (var player in AllPlayers.Keys)
        {
            player.transform.position = PlayerSpawnLocation.position;
        }
        CurrentBoardIndex.OnValueChanged += (oldVal, newVal) =>
        {
            if (boardManagers != null && newVal >= 0 && newVal < boardManagers.Count)
                currentBoardData = boardManagers[newVal];
        };
        CurrentQuizIndex.OnValueChanged += (oldVal, newVal) =>
        {
            if (quizPacks != null && newVal >= 0 && newVal < quizPacks.Count)
                currentQuizPack = quizPacks[newVal];
        };
       
        if (IsServer)
        {
            SelectRandomBoardAndQuizServerRpc();
        }
        BootstrapLevel();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SelectRandomBoardAndQuizServerRpc()
    {
        CurrentBoardIndex.Value = Random.Range(0, boardManagers.Count);
        CurrentQuizIndex.Value = Random.Range(0, quizPacks.Count);

        // Broadcast to all clients
        
    }
    [ClientRpc]
    private void SyncBoardAndQuizClientRpc()
    {
        currentBoardData = boardManagers[CurrentBoardIndex.Value];
        currentQuizPack = quizPacks[CurrentQuizIndex.Value];
    }
    private void FindingManagersInScene()
    {
        if (movementManager == null) movementManager = FindFirstObjectByType<MovementManager>();
        if (quizManager == null) quizManager = FindFirstObjectByType<QuizManager>();
        if (onlineTurnHandler == null) onlineTurnHandler = FindFirstObjectByType<OnlineTurnLogic>();
    }


    [ContextMenu("Start Game Online")]
    public async void StartGame()
    {
        foreach (var player in AllPlayers.Keys)
        {
            player.transform.position = BoardLogicManager.playerHouseLocation.position;
        }
        if (IsServer)
        {
            StartCoroutine(StartFirstTurnNextFrame());
        }
    }

    private IEnumerator StartFirstTurnNextFrame()
    {
        yield return null;
        if (IsServer) StartTurns();
    }
    
    [ContextMenu("Spawn Board")]
    public async Task BootstrapLevel()
    {
        /*if (CurrentLevelData.Board?.BoardPrefab == null) return;

        GameObject spawnedBoard = Instantiate(CurrentLevelData.Board.BoardPrefab, boardParent);
        if (spawnedBoard == null)
        {
            Debug.LogError("SpawnedBoard is null immediately after Instantiate!");
            return;
        }

        // safe call
        BoardLogicManager.Instance?.GenerateTilesPositionWithNumbers(CurrentLevelData.Board.NumberPrefabToSpawnOnBoard, CurrentLevelData.Board.BoardWidth, CurrentLevelData.Board.BoardHeight //spawnedBoard.transform
            );#1#*/
        SyncBoardAndQuizClientRpc();
        HowToPlayPanel = Instantiate(HowToPlayPanelPrefab,CanvasTransform);
        if (IsHost) // Host
        {
            Debug.Log("Is Server (Host)");
            // Host can see and use StartGameButton
            var startButton = GameObject.FindGameObjectWithTag("StartGameButton").GetComponent<Button>();
            startButton.onClick.AddListener(() =>
            {
                StartGame();
                Destroy(HowToPlayPanel);
            });

            // Hide the close button for host if you want
            GameObject.FindGameObjectWithTag("StartGameCloseButton").SetActive(false);
        }
        else // Client
        {
            Debug.Log("Is Client");
            // Clients cannot start the game -> hide start button
            GameObject.FindGameObjectWithTag("StartGameButton").SetActive(false);

            // Clients can only close the HowToPlay panel
            var closeBtn = GameObject.FindGameObjectWithTag("StartGameCloseButton").GetComponent<Button>();
            closeBtn.onClick.AddListener(() => Destroy(HowToPlayPanel));
        }
        
        // 1) Board
        if (CurrentLevelData.Board?.BoardPrefab != null)
        {
            Instantiate(CurrentLevelData.Board.BoardPrefab, boardParent);
            if(BoardLogicManager == null) BoardLogicManager = FindFirstObjectByType<BoardLogicManager>();
            await Task.Yield();
            
            BoardLogicManager.Instance.GenerateTilesPositionWithNumbers(currentBoardData.NumberPrefabToSpawnOnBoard, currentBoardData.BoardWidth, currentBoardData.BoardHeight);
        }
        else Debug.LogWarning("[OfflineFlowManager] Board prefab missing in levelData.");

        FindingManagersInScene();

        RegisterAllNetworkPlayers();
        quizManager?.LoadQuestions(currentQuizPack);
    }

    /*[ServerRpc(RequireOwnership = false)]
    public void RegisterPlayerServerRpc(ulong clientId, ulong networkObjectId)
    {
        var playerObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId].GetComponent<Player>();
        if (playerObj == null) return;

        Color c = (PlayerColors != null && PlayerColors.Length > 0) 
            ? PlayerColors[AllPlayers.Count % PlayerColors.Length] 
            : playerObj.Color;

        playerObj.ApplyColor(c);
        playerObj.Color = c;

        var data = new PlayerGameData(playerObj.PlayerName, playerObj.IsCpu ? PlayerType.CPU : PlayerType.Human, c);

        if (!AllPlayers.ContainsKey(playerObj))
        {
            AllPlayers[playerObj] = data;
            Debug.Log($"[Server] Registered player {playerObj.PlayerName} from client {clientId}");
        }

        // Sync to turn manager
        onlineTurnHandler.RegisterPlayers(AllPlayers.Keys.ToList());
        InstantiatePlayerHuds();
    }*/
    [ServerRpc(RequireOwnership = false)]
    public void RegisterPlayerServerRpc(ulong clientId, ulong networkObjectId)
    {
        var playerObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId].GetComponent<Player>();
        if (playerObj == null) return;

        Color c = PlayerColors[AllPlayers.Count % PlayerColors.Length];
        playerObj.ApplyColor(c);
        playerObj.Color = c;

        var data = new PlayerGameData(playerObj.PlayerName, playerObj.IsCpu ? PlayerType.CPU : PlayerType.Human, c);

        if (!AllPlayers.ContainsKey(playerObj))
        {
            AllPlayers[playerObj] = data;
        }

        onlineTurnHandler.RegisterPlayers(AllPlayers.Keys.ToList());

        // Tell all clients to refresh their HUDs
        UpdatePlayerHudClientRpc(playerObj.NetworkObjectId, playerObj.PlayerName, c);
    }
    [ClientRpc]
    private void UpdatePlayerHudClientRpc(ulong playerNetId, string playerName, Color color)
    {
        var playerObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[playerNetId].GetComponent<Player>();

        if (!playerHuds.ContainsKey(playerObj))
        {
            GameObject hud = Instantiate(PlayerHudItem, PlayerHudItemPanelTransform);
            hud.name = $"{playerName}_HUD";
            hud.transform.GetComponentInChildren<TMP_Text>().text = playerName;
            hud.transform.GetChild(2).GetComponentInChildren<Image>().color = color;
            hud.transform.Find("TurnIndicator").gameObject.SetActive(false);

            playerHuds.Add(playerObj, hud);
        }
    }

    private void RegisterAllNetworkPlayers()
    {
        AllPlayers.Clear();
        
        var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        int idx = 0;
        foreach (var p in players)
        {
            Color c = (PlayerColors != null && PlayerColors.Length > 0) ? PlayerColors[idx % PlayerColors.Length] : p.Color;
            p.ApplyColor(c);
            p.Color = c;

            var data = new PlayerGameData(p.PlayerName, p.IsCpu ? PlayerType.CPU : PlayerType.Human, c);
            AllPlayers[p] = data;
            idx++;
        }

        onlineTurnHandler.RegisterPlayers(AllPlayers.Keys.ToList()); 
        //InstantiatePlayerHuds();
    }

    private void StartTurns()
    {
        if (!IsServer || AllPlayers.Count == 0) return;
        Player current = onlineTurnHandler.GetCurrentPlayer();
        if (current != null) StartTurnForPlayer(current);
        UpdateTurnIndicators(current);
    }

    private void StartTurnForPlayer(Player p)
    {
        int idx = quizManager.GetRandomQuestionIndex();
        var q = quizManager.GetQuestionByIndex(idx);

        if (q == null)
        {
            Debug.LogError($"[MultiplayerFlowManager] Null question for player {p.PlayerName}, index {idx}");
            return;
        }

        bool canUseHint = q.isHintAllowed;
        ShowQuizClientRpc(idx, p.OwnerClientId, canUseHint);
        /*if (p == null) return;
        UpdateTurnIndicators(p);
        if (p.IsCpu)
        {
            //StartCoroutine(CpuSequence(p));
            return;
        }

        int idx = quizManager.GetRandomQuestionIndex();
        var q = quizManager.GetQuestionByIndex(idx);
        bool canUseHint = q != null && q.isHintAllowed;

        // tell only the owner client to show quiz
        ShowQuizClientRpc(idx, p.OwnerClientId, canUseHint);*/
    }

    [ClientRpc]
    private void ShowQuizClientRpc(int questionIdx, ulong targetClient, bool canUseHint, ClientRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClient) return;

        var q = quizManager.GetQuestionByIndex(questionIdx);
        var localPlayer = FindObjectsOfType<Player>().FirstOrDefault(p => p.OwnerClientId == targetClient);
        quizManager.ShowQuizForPlayer(q, localPlayer, canUseHint,
            () => { }, // onHint
            (qr) =>
            {
                SubmitQuizResultServerRpc(targetClient, questionIdx, qr.IsCorrect, qr.TimeTaken, qr.SelectedIndex);
            });
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitQuizResultServerRpc(ulong clientId, int qIdx, bool isCorrect, float time, int selectedIdx)
    {
        var player = AllPlayers.Keys.FirstOrDefault(p => p.OwnerClientId == clientId);
        if (player == null) return;

        var q = quizManager.GetQuestionByIndex(qIdx);
        var qr = new QuizResult { IsCorrect = isCorrect, TimeTaken = time, SelectedIndex = selectedIdx };

        movementManager.ProcessPostQuizMovement(player, isCorrect, q.questionsDifficulty, time, CurrentLevelData.Board, CurrentLevelData.DiceRollRangePerQuizDifficulty, (mres) =>
        {
            UpdatePlayerDataAfterMovement(player, q, qr, mres);

            if (mres.Finished)
            {
                if (AllPlayers.TryGetValue(player, out var d)) d.UpdatePlayersDataMarkFinished(true);
                onlineTurnHandler.RemovePlayerFromTurn(player);
            }

            if (CheckForGameEnd())
            {
                BroadcastLeaderboardClientRpc();
                return;
            }

            onlineTurnHandler.EndTurn();
            Player nxt = onlineTurnHandler.GetCurrentPlayer();
            if (nxt != null) StartTurnForPlayer(nxt);
        });
    }

    
    /*private IEnumerator CpuSequence(Player cpu)
    {
        yield return new WaitForSeconds(0.5f);
        bool correct = Random.value > 0.35f;
        float t = Random.Range(2f, 8f);
        QuestionsDifficulty d = QuestionsDifficulty.Easy;
        movementManager.ProcessPostQuizMovement(cpu, correct, d, t, CurrentLevelData.Board, CurrentLevelData.DiceRollRangePerQuizDifficulty, (mres) =>
        {
            UpdatePlayerDataAfterMovement(cpu, null, new QuizResult { IsCorrect = correct, SelectedIndex = -1, TimeTaken = t }, mres);
            if (mres.Finished)
            {
                if (AllPlayers.TryGetValue(cpu, out var cd)) cd.UpdatePlayersDataMarkFinished(true);
                onlineTurnHandler.RemovePlayerFromTurn(cpu);
            }

            if (CheckForGameEnd())
            {
                BroadcastLeaderboardClientRpc();
                return;
            }

            onlineTurnHandler.EndTurn();
            Player nxt = onlineTurnHandler.GetCurrentPlayer();
            if (nxt != null) StartTurnForPlayer(nxt);
        });
    }#1#*/

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
        int notFinished = AllPlayers.Values.Count(x => !x.PlayerCurrentGameStateData.IsFinished);
        return notFinished <= 1;
    }

    [ClientRpc]
    private void BroadcastLeaderboardClientRpc()
    {
        LeaderBoard.SetActive(true);
        Debug.Log("[MultiplayerFlowManager] === Leaderboard ===");
        var ranking = AllPlayers.OrderByDescending(kv => kv.Value.PlayerCurrentGameStateData.CurrentIndex).ThenByDescending(kv => kv.Value.PlayerCurrentGameStateData.TotalCorrectAnswered).ToList();
        int rank = 1;
        foreach (var kv in ranking)
        {
            SetLeaderBoardRankings(rank, kv);
            SetSummaryData(kv);
            Debug.Log($"{rank}. {kv.Value.Name} - tile:{kv.Value.PlayerCurrentGameStateData.CurrentIndex} correct:{kv.Value.PlayerCurrentGameStateData.TotalCorrectAnswered}");
            rank++;
            if (!GameModeManager.Instance.IsPrivateRoom)
            {
                int correct = kv.Value.PlayerCurrentGameStateData.TotalCorrectAnswered;
                int moves   = kv.Value.PlayerCurrentGameStateData.MovesCounter;

                // Score formula
                CalculateAndUpdatePoints(correct, moves,rank==1);
            }

        }

        // also print debug questions/answers of each player
        /*foreach (var kv in AllPlayers)
        {
            var summaries = kv.Key.QuestionsListForSummary();
            Debug.Log($"[QA Dump] {kv.Value.Name}:");
            foreach (var s in summaries)
                Debug.Log($"  Q: {s.Question} | Correct: {s.CorrectAnswer}");
        }#1#*/
    }

    private void CalculateAndUpdatePoints(int correct, int moves,bool isWinner)
    {
        KeyValuePair<Player, PlayerGameData> kv;
        int score = (correct * pointsPerCorrect) - (moves * penaltyPerMove);
        if (score < 0) score = 0; // prevent negatives
        if (isWinner) score += WinnerScore;
        Leaderboard.Instance.AddScore(score);
        Debug.Log($"[Leaderboard] Submitted score {score} ");
    }

    /// PlayerHuds ///
    
    private void InstantiatePlayerHuds()
    {
        int i = 1;
       //playerHuds.Clear(); 
        /*if (PlayerHudItemPanelTransform.childCount > 0)
        {
            foreach (GameObject child in PlayerHudItemPanelTransform)
            {
                Destroy(child);
            }
        }#1#*/
        foreach (var kv in AllPlayers)
        {
            Player p = kv.Key;
            if (playerHuds.ContainsKey(p)) continue;
            GameObject hud = Instantiate(PlayerHudItem, PlayerHudItemPanelTransform);
            hud.name = $"{p.PlayerName}'s_HUD";
            //hud.name = $"Player{i}_HUD";

            // set player name text
            hud.transform.GetComponentInChildren<TMP_Text>().text = i.ToString();
            hud.transform.GetChild(3).GetComponent<TMP_Text>().text = p.PlayerName;
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
    
    public void UpdateHUD( int stepsMoved = 0, bool snake = false, bool ladder = false)
    {
        // Base string = player's turn
        string baseText = (CurrentPlayer.OwnerClientId == NetworkManager.Singleton.LocalClientId)
            ? "Your Turn"
            : $"{CurrentPlayer.PlayerName}'s Turn";

        //string baseText = $"{CurrentPlayer.PlayerName}'s Turn";


        // Add details
        string details = "";
        if (stepsMoved > 0) details += $" Moved {stepsMoved} steps.";
        if (ladder) details += $"\n <color=green>{LadderTexts[Random.Range(0,LadderTexts.Count)]}</color>";
        if (snake) details += $"\n <color=red>{SnakeTexts[Random.Range(0,SnakeTexts.Count)]}</color>";
        Info_Text.text = baseText + details; 
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
            summary.transform.GetChild(2).GetComponent<TMP_Text>().text = index+1.ToString();
            summary.transform.GetChild(3).GetComponent<TMP_Text>().text = qa.Key;
            index++;

        }
        //summary.transform.GetChild(0).GetComponent<TMP_Text>().text = kv.Value.PlayerCurrentGameStateData.QuestionsAndAnswers.Keys;
    }
    public void SwitchScene(string SceneName)
    {
        LoadingSceneManager.Instance.LoadofflineScene(SceneName);
        LoadingSceneManager.Instance.SetLoadingScreenMessage("Loading Menu");
    }
    
    
}

