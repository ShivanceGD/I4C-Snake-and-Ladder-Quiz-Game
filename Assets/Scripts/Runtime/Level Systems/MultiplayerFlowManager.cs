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

    [Header("LeaderBoard References")]
    [SerializeField] private GameObject LeaderBoard;
    [SerializeField] private GameObject RankPrefab;
    [SerializeField] private Transform RankingTransform;

    [Header("Summary References")]
    [SerializeField] private GameObject SummaryPrefab;
    [SerializeField] private Transform SummaryTransform;

    [Header("Hud References")]
    [SerializeField] private GameObject PlayerHudItem;
    [SerializeField] private Transform PlayerHudItemPanelTransform;
    [SerializeField] private TMP_Text Info_Text;

    // centralized data (server-authoritative)
    public SerializedDictionary<Player, PlayerGameData> AllPlayers = new();
    [SerializeField] private Dictionary<Player, GameObject> playerHuds = new();
    private Player CurrentPlayer;

    [Header("Colors")]
    public Color[] PlayerColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };
    [SerializeField] private List<string> LadderTexts;
    [SerializeField] private List<string> SnakeTexts;

    [Header("Level and Quiz Data")]
    [SerializeField] private List<BoardDataSO> boardManagers;
    [SerializeField] private List<QuizPackSO> quizPacks;

    [Header("Ranking References")]
    [SerializeField] private int pointsPerCorrect;
    [SerializeField] private int penaltyPerMove;
    [SerializeField] private int WinnerScore;

    public NetworkVariable<int> currentBoardIndex = new NetworkVariable<int>();
    public NetworkVariable<int> currentQuizIndex = new NetworkVariable<int>();

    // Added
    private bool bootstrapped = false;

    private void Awake()
    {
        currentBoardIndex.OnValueChanged += OnBoardChanged;
        currentQuizIndex.OnValueChanged += OnQuizChanged;
        FindingManagersInScene();
    }

    private void OnDestroy()
    {
        currentBoardIndex.OnValueChanged -= OnBoardChanged;
        currentQuizIndex.OnValueChanged -= OnQuizChanged;
    }

    private void Start()
    {
        foreach (var player in AllPlayers.Keys)
        {
            if (player != null && PlayerSpawnLocation != null)
                player.transform.position = PlayerSpawnLocation.position;
        }

        if (IsServer)
        {
            // Server picks random board/quiz early
            SelectRandomBoardAndQuizServerRpc();
        }

        // For host, values are immediately available locally
        if (IsHost)
        {
            if (quizPacks != null && currentQuizIndex.Value >= 0 && currentQuizIndex.Value < quizPacks.Count)
                currentQuizPack = quizPacks[currentQuizIndex.Value];

            if (boardManagers != null && currentBoardIndex.Value >= 0 && currentBoardIndex.Value < boardManagers.Count)
                currentBoardData = boardManagers[currentBoardIndex.Value];
        }

        // Try bootstrap if variables are already synced
        TryBootstrapIfReady();
    }

    private void OnBoardChanged(int oldVal, int newVal)
    {
        FindingManagersInScene();
        if (boardManagers != null && newVal >= 0 && newVal < boardManagers.Count)
            currentBoardData = boardManagers[newVal];

        TryBootstrapIfReady();
    }

    private void OnQuizChanged(int oldVal, int newVal)
    {
        FindingManagersInScene();
        if (quizPacks != null && newVal >= 0 && newVal < quizPacks.Count)
        {
            currentQuizPack = quizPacks[newVal];
            quizManager?.LoadQuestions(currentQuizPack);
        }

        TryBootstrapIfReady();
    }

    [ContextMenu("randomBoardAndQuiz")]
    [ServerRpc(RequireOwnership = false)]
    private void SelectRandomBoardAndQuizServerRpc()
    {
        if (boardManagers != null && boardManagers.Count > 0)
            currentBoardIndex.Value = Random.Range(0, boardManagers.Count);

        if (quizPacks != null && quizPacks.Count > 0)
            currentQuizIndex.Value = Random.Range(0, quizPacks.Count);
    }

    private void FindingManagersInScene()
    {
        if (movementManager == null)
            movementManager = FindFirstObjectByType<MovementManager>();

        if (quizManager == null)
            quizManager = FindFirstObjectByType<QuizManager>();

        if (onlineTurnHandler == null)
            onlineTurnHandler = FindFirstObjectByType<OnlineTurnLogic>();

        if (BoardLogicManager == null)
            BoardLogicManager = FindFirstObjectByType<BoardLogicManager>();
    }

    private void TryBootstrapIfReady()
    {
        if (bootstrapped) return;

        bool boardReady = boardManagers != null && currentBoardIndex.Value >= 0 && currentBoardIndex.Value < boardManagers.Count;
        bool quizReady = quizPacks != null && currentQuizIndex.Value >= 0 && currentQuizIndex.Value < quizPacks.Count;

        FindingManagersInScene();

        if (boardReady)
            currentBoardData = boardManagers[currentBoardIndex.Value];

        if (quizReady)
        {
            currentQuizPack = quizPacks[currentQuizIndex.Value];
            quizManager?.LoadQuestions(currentQuizPack);
        }

        if (boardReady && quizReady)
        {
            bootstrapped = true;
            _ = BootstrapLevel();
        }
    }

    [ContextMenu("Start Game Online")]
    public async void StartGame()
    {
        foreach (var player in AllPlayers.Keys)
        {
            if (player != null && PlayerSpawnLocation != null)
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
        // Double-check managers
        FindingManagersInScene();

        if (HowToPlayPanelPrefab != null && CanvasTransform != null)
            HowToPlayPanel = Instantiate(HowToPlayPanelPrefab, CanvasTransform);

        if (IsHost) // Host
        {
            Debug.Log("Is Server (Host)");
            var startButtonGO = GameObject.FindGameObjectWithTag("StartGameButton");
            if (startButtonGO != null)
            {
                var startButton = startButtonGO.GetComponent<Button>();
                if (startButton != null)
                {
                    startButton.onClick.AddListener(() =>
                    {
                        StartGame();
                        if (HowToPlayPanel != null) Destroy(HowToPlayPanel);
                    });
                }
            }

            var closeBtnGO = GameObject.FindGameObjectWithTag("StartGameCloseButton");
            if (closeBtnGO != null) closeBtnGO.SetActive(false);
        }
        else // Client
        {
            Debug.Log("Is Client");
            var startBtn = GameObject.FindGameObjectWithTag("StartGameButton");
            if (startBtn != null) startBtn.SetActive(false);

            var closeBtnGO = GameObject.FindGameObjectWithTag("StartGameCloseButton");
            if (closeBtnGO != null)
            {
                var closeBtn = closeBtnGO.GetComponent<Button>();
                if (closeBtn != null)
                {
                    closeBtn.onClick.AddListener(() =>
                    {
                        if (HowToPlayPanel != null) Destroy(HowToPlayPanel);
                    });
                }
            }
        }

        // 1) Board
        if (currentBoardData?.BoardPrefab != null)
        {
            Instantiate(currentBoardData.BoardPrefab, boardParent);

            if (BoardLogicManager == null)
                BoardLogicManager = FindFirstObjectByType<BoardLogicManager>();

            await Task.Yield();

            if (BoardLogicManager != null)
            {
                BoardLogicManager.Instance.GenerateTilesPositionWithNumbers(
                    currentBoardData.NumberPrefabToSpawnOnBoard,
                    currentBoardData.BoardWidth,
                    currentBoardData.BoardHeight
                );
            }
            else
            {
                Debug.LogError("[BootstrapLevel] BoardLogicManager missing after instantiate. Make sure it's present in scene.");
            }
        }
        else
        {
            Debug.LogWarning("[MultiplayerFlowManager] Board prefab missing in levelData.");
        }

        FindingManagersInScene();
        RegisterAllNetworkPlayers();
        quizManager?.LoadQuestions(currentQuizPack);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RegisterPlayerServerRpc(ulong clientId, ulong networkObjectId)
    {
        var spawned = NetworkManager.Singleton.SpawnManager.SpawnedObjects;
        if (!spawned.ContainsKey(networkObjectId)) return;

        var playerObj = spawned[networkObjectId].GetComponent<Player>();
        if (playerObj == null) return;

        int index = -1;
        if (clientId == NetworkManager.Singleton.LocalClientId && PlayerColors.Length > 0)
            index = 0; // host red
        else if (PlayerColors != null && PlayerColors.Length > 0)
        {
            index = AllPlayers.Count % PlayerColors.Length;
            if (index == 0) index = 1;
        }

        if (index >= 0)
        {
            playerObj.NetworkColorIndex.Value = index;
            playerObj.ApplyColor(PlayerColors[index]);
        }

        string resolvedName = playerObj.NetworkPlayerName.Value.Length > 0 ? playerObj.NetworkPlayerName.Value.ToString() : playerObj.PlayerName;
        var data = new PlayerGameData(resolvedName,
            playerObj.IsCpu ? PlayerType.CPU : PlayerType.Human,
            (index >= 0 && PlayerColors != null && index < PlayerColors.Length) ? PlayerColors[index] : playerObj.Color);

        if (!AllPlayers.ContainsKey(playerObj))
            AllPlayers[playerObj] = data;

        onlineTurnHandler?.RegisterPlayers(AllPlayers.Keys.ToList());
        InstantiatePlayerHuds();
    }

    private void RegisterAllNetworkPlayers()
    {
        AllPlayers.Clear();
        var players = FindObjectsByType<Player>(FindObjectsSortMode.None);

        foreach (var p in players)
        {
            if (p == null) continue;

            string name = p.NetworkPlayerName.Value.Length > 0 ? p.NetworkPlayerName.Value.ToString() : p.PlayerName;
            Color color = (p.NetworkColorIndex.Value >= 0 && PlayerColors != null && p.NetworkColorIndex.Value < PlayerColors.Length) ?
                          PlayerColors[p.NetworkColorIndex.Value] : p.Color;

            p.ApplyColor(color);
            var data = new PlayerGameData(name, p.IsCpu ? PlayerType.CPU : PlayerType.Human, color);
            AllPlayers[p] = data;
        }

        onlineTurnHandler?.RegisterPlayers(AllPlayers.Keys.ToList());
        InstantiatePlayerHuds();
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
        if (p.IsCpu) return;

        int idx = quizManager != null ? quizManager.GetRandomQuestionIndex() : -1;
        var q = idx >= 0 && quizManager != null ? quizManager.GetQuestionByIndex(idx) : null;
        bool canUseHint = q != null && q.isHintAllowed;

        ShowQuizClientRpc(idx, p.OwnerClientId, canUseHint);
    }

    [ClientRpc]
    private void ShowQuizClientRpc(int questionIdx, ulong targetClient, bool canUseHint, ClientRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClient) return;

        FindingManagersInScene();

        if (quizManager == null)
        {
            Debug.LogError("[ShowQuizClientRpc] quizManager is null on client. Can't show quiz.");
            return;
        }

        var q = quizManager.GetQuestionByIndex(questionIdx);
        if (q == null)
        {
            Debug.LogError("[ShowQuizClientRpc] question is null (index " + questionIdx + ")");
            return;
        }

        var localPlayer = FindObjectsOfType<Player>().FirstOrDefault(p => p.OwnerClientId == targetClient);
        quizManager.ShowQuizForPlayer(q, localPlayer, canUseHint, () => { }, (qr) =>
        {
            SubmitQuizResultServerRpc(targetClient, questionIdx, qr.IsCorrect, qr.TimeTaken, qr.SelectedIndex);
        });
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitQuizResultServerRpc(ulong clientId, int qIdx, bool isCorrect, float time, int selectedIdx)
    {
        var player = AllPlayers.Keys.FirstOrDefault(p => p.OwnerClientId == clientId);
        if (player == null)
        {
            Debug.LogWarning("[SubmitQuizResultServerRpc] Player not found for clientId: " + clientId);
            return;
        }

        var q = quizManager != null ? quizManager.GetQuestionByIndex(qIdx) : null;
        var qr = new QuizResult
        {
            IsCorrect = isCorrect,
            TimeTaken = time,
            SelectedIndex = selectedIdx
        };

        movementManager?.ProcessPostQuizMovement(player, isCorrect, q != null ? q.questionsDifficulty : 0,
            time, currentBoardData,
            CurrentLevelData != null ? CurrentLevelData.DiceRollRangePerQuizDifficulty : null,
            (mres) =>
            {
                UpdatePlayerDataAfterMovement(player, q, qr, mres);

                if (mres.Finished)
                {
                    if (AllPlayers.TryGetValue(player, out var d))
                        d.UpdatePlayersDataMarkFinished(true);

                    onlineTurnHandler?.RemovePlayerFromTurn(player);
                }

                if (CheckForGameEnd())
                {
                    BroadcastLeaderboardClientRpc();
                    return;
                }

                onlineTurnHandler?.EndTurn();
                Player nxt = onlineTurnHandler?.GetCurrentPlayer();
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
            data.UpdatePlayersDataIndexData(movementResult.FinalTileIndex);

        data.UpdatePlayersDataMovesCounter();

        if (movementResult.Finished)
            data.UpdatePlayersDataMarkFinished(true);
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

        var ranking = AllPlayers
            .OrderByDescending(kv => kv.Value.PlayerCurrentGameStateData.CurrentIndex)
            .ThenByDescending(kv => kv.Value.PlayerCurrentGameStateData.TotalCorrectAnswered)
            .ToList();

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

                CalculateAndUpdatePoints(correct, moves, rank == 1);
            }
        }
    }

    private void CalculateAndUpdatePoints(int correct, int moves, bool isWinner)
    {
        int score = (correct * pointsPerCorrect) - (moves * penaltyPerMove);
        if (score < 0) score = 0;

        if (isWinner) score += WinnerScore;

        Leaderboard.Instance?.AddScore(score);
        Debug.Log($"[Leaderboard] Submitted score {score} ");
    }

    #region PlayerHUD Methods
    public void InstantiatePlayerHuds()
    {
        int i = 1;

        foreach (var kv in AllPlayers)
        {
            Player p = kv.Key;
            if (playerHuds.ContainsKey(p)) continue;

            GameObject hud = Instantiate(PlayerHudItem, PlayerHudItemPanelTransform);
            hud.name = $"{kv.Value.Name}'s_HUD";

            // slot number
            var slotText = hud.transform.GetComponentInChildren<TMP_Text>();
            if (slotText != null) slotText.text = i.ToString();

            // player name
            if (hud.transform.childCount > 3)
            {
                var nameText = hud.transform.GetChild(3).GetComponent<TMP_Text>();
                if (nameText != null) nameText.text = kv.Value.Name;
            }

            // player color
            if (hud.transform.childCount > 2)
            {
                var img = hud.transform.GetChild(2).GetComponentInChildren<Image>();
                if (img != null) img.color = kv.Value.Color;
            }

            // turn indicator
            var turnIndicator = hud.transform.Find("TurnIndicator");
            if (turnIndicator != null) turnIndicator.gameObject.SetActive(false);

            playerHuds.Add(p, hud);
            i++;
        }

        SyncHudClientRpc();
    }

    [ClientRpc]
    private void SyncHudClientRpc()
    {
        foreach (var kv in AllPlayers)
        {
            UpdateHudName(kv.Key, kv.Value.Name);
            UpdateHudColor(kv.Key, kv.Value.Color);
        }

        var current = onlineTurnHandler?.GetCurrentPlayer();
        if (current != null)
            UpdateTurnIndicatorsClientRpc(current.OwnerClientId);
    }

    public void UpdateHudColor(Player p, Color c)
    {
        if (playerHuds.TryGetValue(p, out var hud))
        {
            if (hud.transform.childCount > 2)
            {
                var img = hud.transform.GetChild(2).GetComponentInChildren<Image>();
                if (img != null) img.color = c;
            }
        }

        if (AllPlayers.TryGetValue(p, out var data))
            data.Color = c;
    }

    private void UpdateTurnIndicators(Player current)
    {
        if (current == null) return;
        UpdateTurnIndicatorsClientRpc(current.OwnerClientId);
    }

    [ClientRpc]
    private void UpdateTurnIndicatorsClientRpc(ulong currentPlayerId)
    {
        foreach (var kv in playerHuds)
        {
            Transform indicator = kv.Value.transform.Find("TurnIndicator");
            if (indicator != null)
                indicator.gameObject.SetActive(kv.Key.OwnerClientId == currentPlayerId);
        }
    }

    public void UpdateHudName(Player p, string name)
    {
        if (playerHuds.TryGetValue(p, out var hud))
        {
            if (hud.transform.childCount > 3)
            {
                var nameText = hud.transform.GetChild(3).GetComponent<TMP_Text>();
                if (nameText != null) nameText.text = name;
            }

            hud.name = $"{name}'s_HUD";

            int playerIndex = AllPlayers.Keys.ToList().IndexOf(p) + 1;
            var slotText = hud.transform.GetComponentInChildren<TMP_Text>();
            if (slotText != null) slotText.text = playerIndex.ToString();
        }

        if (AllPlayers.TryGetValue(p, out var data))
            data.Name = name;
    }

    public void UpdateHUD(int stepsMoved, bool snake, bool ladder)
    {
        var currentplayer = onlineTurnHandler.GetCurrentPlayer();
        if (currentplayer == null) return;

        UpdateHUDClientRPC(stepsMoved, snake, ladder, currentplayer.OwnerClientId);
    }

    [ClientRpc]
    public void UpdateHUDClientRPC(int stepsMoved = 0, bool snake = false, bool ladder = false, ulong currentPlayerClinetID = 0)
    {
        var currentPlayer = FindObjectsOfType<Player>().FirstOrDefault(p => p.OwnerClientId == currentPlayerClinetID);
        if (currentPlayer == null || Info_Text == null) return;

        string baseText = (currentPlayer.OwnerClientId == NetworkManager.Singleton.LocalClientId) ?
            "Your Turn" : $"{currentPlayer.PlayerName}'s Turn";

        string details = "";
        if (stepsMoved > 0) details += $" Moved {stepsMoved} steps.";
        if (ladder) details += $"\n <color=green>{LadderTexts[Random.Range(0, LadderTexts.Count)]}</color>";
        if (snake) details += $"\n <color=red>{SnakeTexts[Random.Range(0, SnakeTexts.Count)]}</color>";

        Info_Text.text = baseText + details;
    }
    #endregion

    private void SetLeaderBoardRankings(int rank, KeyValuePair<Player, PlayerGameData> kv)
    {
        GameObject ranks = Instantiate(RankPrefab, RankingTransform);

        if (ranks.transform.childCount > 4)
        {
            var img = ranks.transform.GetChild(4).GetChild(3).GetComponent<Image>();
            if (img != null) img.color = kv.Value.Color;
        }

        if (ranks.transform.childCount > 0)
        {
            var nameText = ranks.transform.GetChild(0).GetComponent<TMP_Text>();
            if (nameText != null) nameText.text = kv.Value.Name;
        }

        if (ranks.transform.childCount > 1)
        {
            var rankText = ranks.transform.GetChild(1).GetComponentInChildren<TMP_Text>();
            if (rankText != null) rankText.text = rank.ToString();
        }
    }

    private void SetSummaryData(KeyValuePair<Player, PlayerGameData> kv)
    {
        int index = 0;
        foreach (var qa in kv.Value.PlayerCurrentGameStateData.QuestionsAndAnswers)
        {
            GameObject summary = Instantiate(SummaryPrefab, SummaryTransform);

            if (summary.transform.childCount > 1)
            {
                var ansText = summary.transform.GetChild(1).GetComponentInChildren<TMP_Text>();
                if (ansText != null) ansText.text = qa.Value;
            }

            if (summary.transform.childCount > 2)
            {
                var idxText = summary.transform.GetChild(2).GetComponent<TMP_Text>();
                if (idxText != null) idxText.text = (index + 1).ToString();
            }

            if (summary.transform.childCount > 3)
            {
                var qText = summary.transform.GetChild(3).GetComponent<TMP_Text>();
                if (qText != null) qText.text = qa.Key;
            }

            index++;
        }
    }

    public void SwitchScene(string SceneName)
    {
        LoadingSceneManager.Instance?.LoadofflineScene(SceneName);
        LoadingSceneManager.Instance?.SetLoadingScreenMessage("Loading Menu");
    }
}
