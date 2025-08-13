using Unity.Netcode;
using UnityEngine;

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

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    [ContextMenu("Start Gamme")]
    private void StartGame()
    {
        if (IsServer && autoStartOnHost)
        {
            StartGameServerRpc();
        }
    }

    [ContextMenu("Event Wiring")]
    public void EventWiring()
    {
        turnManager = FindFirstObjectByType<TurnManager>();
        quizManager = FindFirstObjectByType<QuizManager>();
        levelManager = FindFirstObjectByType<LevelManager>();
        movementManager = FindFirstObjectByType<MovementManager>();
        netFlow = FindFirstObjectByType<NetworkFlowManager>();

        // event wiring
        turnManager.OnTurnStarted += HandleTurnStarted;
        quizManager.OnQuizCompleted += HandleQuizCompleted; // <- this will now fire on server for BOTH host & client turns


        // cache questions and level
        levelManager.CacheCurrentLevelIfNeeded();
        quizManager.CacheQuestionsFromLevel();
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc()
    {
        // ensure server has player list
        turnManager.InitializeFromScenePlayersServer();
        turnManager.ServerStartTurns();
    }

    private void HandleTurnStarted(PlayerManager p)
    {
        if (!IsServer) return;
        // server asks the client to show quiz via NetworkFlowManager
        netFlow.RequestQuizServerRpc(p.OwnerClientId);
    }

    private void HandleQuizCompleted(PlayerManager player, bool isCorrect, Difficulty difficulty, float timeTaken)
    {
        if (!IsServer) return;
        movementManager.ProcessPostQuizMovementServer(player, isCorrect, difficulty, timeTaken);
    }

    // helper for PlayerManager registration (server & local)
    public void RegisterPlayer(PlayerManager player)
    {
        turnManager.RegisterPlayer(player);
    }
}
