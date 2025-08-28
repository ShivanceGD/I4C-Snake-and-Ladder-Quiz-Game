using System.Collections;
using System.Linq;
using AYellowpaper.SerializedCollections;
using Unity.Netcode;
using UnityEngine;

public class MultiplayerFlowManager : NetworkBehaviour
{
    [Header("References (assign in inspector)")]
    public LevelDataSO CurrentLevelData;
    public Transform boardParent;
    public MovementManager movementManager;
    public QuizManager quizManager;
    public OnlineTurnLogic onlineTurnHandler;

    // centralized data (server-authoritative)
    public SerializedDictionary<Player, PlayerGameData> AllPlayers = new();

    [Header("Colors")]
    public Color[] PlayerColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };

    private void Awake()
    {
        if (movementManager == null) movementManager = FindFirstObjectByType<MovementManager>();
        if (quizManager == null) quizManager = FindFirstObjectByType<QuizManager>();
        if (onlineTurnHandler == null) onlineTurnHandler = FindFirstObjectByType<OnlineTurnLogic>();
    }
    
    
    [ContextMenu("Start Game Online")]
    public void StartGame()
    {
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
    public void BootstrapLevel()
    {
        if (CurrentLevelData.Board?.BoardPrefab == null) return;

        GameObject spawnedBoard = Instantiate(CurrentLevelData.Board.BoardPrefab, boardParent);
        if (spawnedBoard == null)
        {
            Debug.LogError("SpawnedBoard is null immediately after Instantiate!");
            return;
        }

        // safe call
        BoardLogicManager.Instance?.GenerateTilesPositionWithNumbers(CurrentLevelData.Board.NumberPrefabToSpawnOnBoard, CurrentLevelData.Board.BoardWidth, CurrentLevelData.Board.BoardHeight //spawnedBoard.transform
            );

        RegisterAllNetworkPlayers();
        quizManager?.LoadQuestions(CurrentLevelData.LevelQuizSCO);
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
    }

    private void StartTurns()
    {
        if (!IsServer || AllPlayers.Count == 0) return;
        Player current = onlineTurnHandler.GetCurrentPlayer();
        if (current != null) StartTurnForPlayer(current);
    }

    private void StartTurnForPlayer(Player p)
    {
        if (p == null) return;
        if (p.IsCpu)
        {
            StartCoroutine(CpuSequence(p));
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

    
    private IEnumerator CpuSequence(Player cpu)
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
        Debug.Log("[MultiplayerFlowManager] === Leaderboard ===");
        var ranking = AllPlayers.OrderByDescending(kv => kv.Value.PlayerCurrentGameStateData.CurrentIndex).ThenByDescending(kv => kv.Value.PlayerCurrentGameStateData.TotalCorrectAnswered).ToList();
        int rank = 1;
        foreach (var kv in ranking)
        {
            Debug.Log($"{rank}. {kv.Value.Name} - tile:{kv.Value.PlayerCurrentGameStateData.CurrentIndex} correct:{kv.Value.PlayerCurrentGameStateData.TotalCorrectAnswered}");
            rank++;
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
}
