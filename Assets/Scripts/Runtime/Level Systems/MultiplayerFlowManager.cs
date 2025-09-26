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
    public NetworkVariable<int> currentBoardIndex = new NetworkVariable<int>();
    public NetworkVariable<int> currentQuizIndex = new NetworkVariable<int>();
    /*
    private void Awake()
    {
        //BootstrapLevel();
    }
    */

    /*private void Start()
    {
        foreach (var player in AllPlayers.Keys)
        {
            player.transform.position = PlayerSpawnLocation.position;
        }
       
        if (IsServer)
        {
            SelectRandomBoardAndQuizServerRpc();
        }
        BootstrapLevel();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SelectRandomBoardAndQuizServerRpc()
    {
        int boardIndex = Random.Range(0, boardManagers.Count);
        int quizIndex = Random.Range(0, quizPacks.Count);

        // Store on host
        currentBoardData = boardManagers[boardIndex];
        currentQuizPack = quizPacks[quizIndex];

        // Broadcast to all clients
        SyncBoardAndQuizClientRpc(boardIndex, quizIndex);
    }*/
    private void Awake()
    {
        // Subscribe to NetworkVariable changes
        currentBoardIndex.OnValueChanged += OnBoardChanged;
        currentQuizIndex.OnValueChanged += OnQuizChanged;
    }

    private void Start()
    {
        foreach (var player in AllPlayers.Keys)
        {
            player.transform.position = PlayerSpawnLocation.position;
        }

        if (IsServer)
        {
            SelectRandomBoardAndQuizServerRpc();
        } 
        currentQuizPack = quizPacks[currentQuizIndex.Value]; 
        Debug.Log(currentQuizPack.name); 
        currentBoardData = boardManagers[currentBoardIndex.Value]; 
        Debug.Log(currentBoardData.name); 
        BootstrapLevel();
    }
    private void OnBoardChanged(int oldVal, int newVal)
    {
        Debug.Log($"[NetworkSync] Board index changed from {oldVal} to {newVal}");

        if (newVal >= 0 && newVal < boardManagers.Count)
        {
            currentBoardData = boardManagers[newVal];
            Debug.Log($"[NetworkSync] currentBoardData set to: {currentBoardData.name}");
            //SpawnBoardForClient();
            
        }
        else
        {
            Debug.LogError($"[NetworkSync] Invalid board index: {newVal}");
        }
    }

    private void OnQuizChanged(int oldVal, int newVal)
    {
        Debug.Log($"[NetworkSync] Quiz index changed from {oldVal} to {newVal}");

        if (newVal >= 0 && newVal < quizPacks.Count)
        {
            currentQuizPack = quizPacks[newVal];
            Debug.Log($"[NetworkSync] currentQuizPack set to: {currentQuizPack.name}");

            if (quizManager != null)
            {
                quizManager.LoadQuestions(currentQuizPack);
                Debug.Log($"[NetworkSync] Questions loaded for client from quiz pack: {currentQuizPack.name}");
            }
            else
            {
                Debug.LogError("[NetworkSync] quizManager is null! Cannot load questions.");
            }
        }
        else
        {
            Debug.LogError($"[NetworkSync] Invalid quiz index: {newVal}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SelectRandomBoardAndQuizServerRpc()
    {
        // Assign to NetworkVariables (auto-syncs to clients)
        currentBoardIndex.Value = Random.Range(0, boardManagers.Count);;
        currentQuizIndex.Value = Random.Range(0, quizPacks.Count);;

        //Debug.Log($"[Server] Selected board {boardIndex} and quiz {quizIndex}");
    }
    /*[ClientRpc]
    private void SyncBoardAndQuizClientRpc(int boardIndex, int quizIndex)
    {
        currentBoardData = boardManagers[boardIndex];
        currentQuizPack = quizPacks[quizIndex];
    }*/
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
        /*if (CurrentLevelData.Board?.BoardPrefab == null) return;

        GameObject spawnedBoard = Instantiate(CurrentLevelData.Board.BoardPrefab, boardParent);
        if (spawnedBoard == null)
        {
            Debug.LogError("SpawnedBoard is null immediately after Instantiate!");
            return;
        }

        // safe call
        BoardLogicManager.Instance?.GenerateTilesPositionWithNumbers(CurrentLevelData.Board.NumberPrefabToSpawnOnBoard, CurrentLevelData.Board.BoardWidth, CurrentLevelData.Board.BoardHeight //spawnedBoard.transform
            );*/
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

    [ServerRpc(RequireOwnership = false)]
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
        Debug.Log($"[RegisterAllNetworkPlayers] Found {players.Length} players:");
        foreach (var p in players)
        {
            Debug.Log($"- {p.name} | Owner: {p.OwnerClientId} | PlayerName: {p.PlayerName}");
        }
        onlineTurnHandler.RegisterPlayers(AllPlayers.Keys.ToList()); 
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
        if (p.IsCpu)
        {
            //StartCoroutine(CpuSequence(p));
            return;
        }

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
    }*/

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
        }*/
    }

    private void CalculateAndUpdatePoints(int correct, int moves,bool isWinner)
    {
        //KeyValuePair<Player, PlayerGameData> kv;
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
        }*/
        foreach (var kv in AllPlayers)
        {
            Player p = kv.Key;
            if (playerHuds.ContainsKey(p)) return;
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
