using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Managers (assign in inspector)")]
    public LevelManager levelManager;
    public QuizManager quizManager;
    public TurnManager turnManager;
    public MovementManager movementManager;
    public NetworkFlowManager netFlow;

    [Header("Settings")]
    public bool autoStartOnHost = true;
    [Tooltip("How long (seconds) to wait for PlayerManager instances to appear before timing out")]
    public float playerWaitTimeout = 8f;

    [Header("Offline Mode")]
    [Tooltip("Enable to run single-player vs CPU without any RPCs/networking.")]
    public bool isOfflineMode = false;
    [Tooltip("Player prefab (must have PlayerManager). Used only in offline mode.")]
    public PlayerManager offlinePlayerPrefab;
    [Tooltip("CPU prefab (must have PlayerManager). Used only in offline mode.")]
    public PlayerManager offlineCpuPrefab;
    [Tooltip("Spawn points for Player (index 0) and CPU (index 1) in offline mode.")]
    public Transform[] offlineSpawnPoints = new Transform[2];
    [Tooltip("Automatically start turns when scene loads in offline mode.")]
    public bool autoStartOffline = true;

    private PlayerManager cpuPlayer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private new void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // fire-and-forget the async setup for the scene
        _ = SetupAfterSceneLoadedAsync(scene);
    }

    private async Task SetupAfterSceneLoadedAsync(Scene scene)
    {
        // 1) find/wait for manager components
        await EventWiringAsync();

        // ===== OFFLINE SINGLE PLAYER =====
        if (isOfflineMode)
        {
            // Instantiate/local register players if needed
            EnsureOfflinePlayersSpawned();

            // Manually populate players list from scene in the order: Player -> CPU
            var all = FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);
            foreach (var p in all)
            {
                // Ensure Player then CPU ordering if both exist
                RegisterPlayer(p);
            }

            // Identify CPU (assume second registered or name contains CPU)
            if (all.Length >= 2)
            {
                // Prefer anything named/flagged as CPU, fallback to "second"
                cpuPlayer = TryFindCpuExplicit(all) ?? all[1];
            }
            else if (all.Length == 1)
            {
                cpuPlayer = null; // Single-player only (no CPU)
            }

            if (autoStartOffline)
            {
                // Start local turns: first player in list goes first
                turnManager.StartLocalTurns();
            }
            if (!autoStartOffline)
            {
                //Display Rules For the games
                QuizManager.Instance.startGamePannel.SetActive(true);
                Button startButton = GameObject.FindGameObjectWithTag("StartGameButton").GetComponent<Button>();
                //startButton.transform.parent.gameObject.SetActive(true);
                startButton.onClick.AddListener(() => turnManager.StartLocalTurns());
                //startButton.transform.parent.gameObject.SetActive(false); //@krithik
            }
            return;
        }

        // ===== MULTIPLAYER (unchanged) =====
        if (!IsServer) return;

        // 3) Wait for PlayerManager objects spawned for connected clients
        await WaitForPlayerManagersAsync(playerWaitTimeout);

        // 4) initialize server-side player list (will find PlayerManager instances in scene)
        turnManager.InitializeFromScenePlayersServer();

        // 5) start turns if configured
        if (autoStartOnHost)
        {
            turnManager.ServerStartTurns();
        }
        if(!autoStartOnHost && IsServer)
        {
            //Display Rules For the games
            QuizManager.Instance.startGamePannel.SetActive(true);
            Button startButton = GameObject.FindGameObjectWithTag("StartGameButton").GetComponent<Button>();
            //startButton.transform.parent.gameObject.SetActive(true);
            startButton.onClick.AddListener(()=> turnManager.ServerStartTurns());
            //startButton.transform.parent.gameObject.SetActive(false); //@krithik
        }
    }

    private PlayerManager TryFindCpuExplicit(PlayerManager[] all)
    {
        foreach (var p in all)
        {
            if (p != null && p.name.ToLower().Contains("cpu")) return p;
        }
        return null;
    }

    private async Task EventWiringAsync()
    {
        await PopulateReferencesAsync();

        // avoid duplicate subscriptions
        if (turnManager != null)
        {
            turnManager.OnTurnStarted -= HandleTurnStarted;
            turnManager.OnTurnStarted += HandleTurnStarted;
        }

        if (quizManager != null)
        {
            quizManager.OnQuizCompleted -= HandleQuizCompleted;
            quizManager.OnQuizCompleted += HandleQuizCompleted;
        }

        levelManager?.CacheCurrentLevelIfNeeded();
        quizManager?.CacheQuestionsFromLevel();
    }

    private async Task PopulateReferencesAsync()
    {
        // loop until all required managers are found
        while (turnManager == null || quizManager == null || levelManager == null || movementManager == null || netFlow == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
            quizManager = FindFirstObjectByType<QuizManager>();
            levelManager = FindFirstObjectByType<LevelManager>();
            movementManager = FindFirstObjectByType<MovementManager>();
            netFlow = FindFirstObjectByType<NetworkFlowManager>();
            // wait next frame
            await Task.Yield();
        }
    }

    private async Task WaitForPlayerManagersAsync(float timeoutSeconds)
    {
        if (NetworkManager.Singleton == null) return;

        int expected = NetworkManager.Singleton.ConnectedClients.Count;
        float start = Time.unscaledTime;

        // quick success if expected <= 0
        if (expected <= 0) return;

        while (Time.unscaledTime - start < timeoutSeconds)
        {
            int found = FindObjectsByType<PlayerManager>(FindObjectsSortMode.None).Length;
            if (found >= expected)
            {
                Debug.Log($"GameManager: Found {found}/{expected} PlayerManager objects.");
                return;
            }
            await Task.Yield();
        }

        int finalFound = FindObjectsByType<PlayerManager>(FindObjectsSortMode.None).Length;
        Debug.LogWarning($"GameManager: WaitForPlayerManagersAsync timed out after {timeoutSeconds}s — found {finalFound}/{expected} PlayerManager objects. Proceeding anyway.");
    }

    // Called by clients (optional) to request server to start the game.
    // Host/server can also call StartGameAsServer() directly in SetupAfterSceneLoadedAsync.
    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc()
    {
        if (!IsServer) return;
        turnManager.InitializeFromScenePlayersServer();
        turnManager.ServerStartTurns();
    }

    private void HandleTurnStarted(PlayerManager p)
    {
        // OFFLINE: Player gets quiz, CPU auto-plays without quiz
        if (isOfflineMode)
        {
            if (p == null) return;

            if (cpuPlayer != null && p == cpuPlayer)
            {
                // CPU takes its turn automatically
                StartCoroutine(CPUTakeTurn());
            }
            else
            {
                // Human player: show quiz locally (pick random question)
                int idx = quizManager.GetRandomQuestionIndex();
                var q = quizManager.GetQuestionByIndex(idx);
                quizManager.ShowQuiz(q, p);
            }
            return;
        }

        // MULTIPLAYER (unchanged): only server triggers target client's quiz
        if (!IsServer) return;
        netFlow.RequestQuizServerRpc(p.OwnerClientId);
    }

    private void HandleQuizCompleted(PlayerManager player, bool isCorrect, Difficulty difficulty, float timeTaken)
    {
        // OFFLINE: resolve movement locally
        if (isOfflineMode)
        {
            movementManager.ProcessPostQuizMovementOffline(player, isCorrect, difficulty, timeTaken);
            return;
        }

        // MULTIPLAYER (unchanged): server-only resolves movement
        if (!IsServer) return;
        movementManager.ProcessPostQuizMovementServer(player, isCorrect, difficulty, timeTaken);
    }

    // helper for PlayerManager registration (server-only in MP, allowed in offline)
    public void RegisterPlayer(PlayerManager player)
    {
        if (player == null) return;

        if (isOfflineMode)
        {
            if (turnManager == null) turnManager = FindFirstObjectByType<TurnManager>();
            turnManager?.RegisterPlayer(player);
            return;
        }

        if (!IsServer) return; // important: ignore client-side calls in MP
        if (turnManager == null) turnManager = FindFirstObjectByType<TurnManager>();
        turnManager?.RegisterPlayer(player);
    }

    // ===== OFFLINE HELPERS =====
    private void EnsureOfflinePlayersSpawned()
    {
        var existing = FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);
        if (existing.Length >= 2) return; // already there

        // Spawn Player
        if (offlinePlayerPrefab != null && (existing.Length == 0 || !HasPlayer(existing)))
        {
            var pos = offlineSpawnPoints != null && offlineSpawnPoints.Length > 0 && offlineSpawnPoints[0] != null
                ? offlineSpawnPoints[0].position
                : Vector3.zero;
            var player = Instantiate(offlinePlayerPrefab, pos, Quaternion.identity);
            player.name = "Player (Offline)";
        }

        // Spawn CPU
        existing = FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);
        if (offlineCpuPrefab != null && existing.Length < 2)
        {
            var pos = offlineSpawnPoints != null && offlineSpawnPoints.Length > 1 && offlineSpawnPoints[1] != null
                ? offlineSpawnPoints[1].position
                : Vector3.zero;
            var cpu = Instantiate(offlineCpuPrefab, pos, Quaternion.identity);
            cpu.name = "CPU (Offline)";
        }
    }

    private bool HasPlayer(PlayerManager[] existing)
    {
        foreach (var pm in existing)
        {
            if (pm != null && !pm.name.ToLower().Contains("cpu")) return true;
        }
        return false;
    }

    private IEnumerator CPUTakeTurn()
    {
        yield return new WaitForSeconds(0.5f); // small delay for UX

        if (cpuPlayer == null) yield break;

        // Randomize CPU correctness so it can trigger snakes/ladders through existing logic
        bool cpuIsCorrect = Random.value > 0.35f;       // ~65% correct
        float cpuTimeTaken = Random.Range(2f, 8f);      // variable speed
        Difficulty cpuDifficulty = Difficulty.Easy;     // use your ranges for Easy

        movementManager.ProcessPostQuizMovementOffline(cpuPlayer, cpuIsCorrect, cpuDifficulty, cpuTimeTaken);
        // MoveCoroutine will advance turn locally when it finishes.
    }
}
