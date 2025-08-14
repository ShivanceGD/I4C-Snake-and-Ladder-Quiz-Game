using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private  new void OnDestroy()
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

        // 2) only server needs to register players + start turns
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

            await Task.Yield(); // wait next frame
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
        if (!IsServer) return;
        netFlow.RequestQuizServerRpc(p.OwnerClientId);
    }

    private void HandleQuizCompleted(PlayerManager player, bool isCorrect, Difficulty difficulty, float timeTaken)
    {
        if (!IsServer) return;
        movementManager.ProcessPostQuizMovementServer(player, isCorrect, difficulty, timeTaken);
    }

    // helper for PlayerManager registration (server-only)
    public void RegisterPlayer(PlayerManager player)
    {
        if (!IsServer) return; // important: ignore client-side calls
        if (turnManager == null) turnManager = FindFirstObjectByType<TurnManager>();
        turnManager?.RegisterPlayer(player);
    }
}
