using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class TurnManager : NetworkBehaviour
{
    public event Action<PlayerManager> OnTurnStarted;
    public GameObject RankingPrefab;
    public Transform Leaderboard_Transform;
    public GameObject Leaderboard;
    public readonly List<PlayerManager> players = new();
    public List<Color> PlayerColors = new List<Color>();
    public GameObject SummaryPrefab;
    public Transform SummaryTransform;
    private readonly HashSet<PlayerManager> finished = new HashSet<PlayerManager>();
    private int currentIndex = 0;

    public void RegisterPlayer(PlayerManager p)
    {
        if (p == null || players.Contains(p)) return;
        players.Add(p);
    }

    public void InitializeFromScenePlayersServer()
    {
        if (!IsServer) return;
        players.Clear();
        var all = FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);
        int i = 0;
        foreach (var p in all)
        {
            players.Add(p);
            p.SetPlayerInitialHomePos();

            if (i < PlayerColors.Count)
                p.PlayerColor.Value = PlayerColors[i]; // ✅ syncs automatically

            i++;
        }
        currentIndex = 0;
    }


    // Local init (offline)
    public void InitializeFromScenePlayersLocal()
    {
        players.Clear();
        var all = FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);
        int i = 0;
        foreach (var p in all)
        {
            players.Add(p);
            p.SetPlayerInitialHomePos();
            if (i < PlayerColors.Count)
                p.PlayerColor.Value = PlayerColors[i];
            i++;
        }
        currentIndex = 0;
    }

    public void ServerStartTurns()
    {
        if (!IsServer || players.Count == 0 || IsGameOver()) return;
        SkipFinished();
        OnTurnStarted?.Invoke(players[currentIndex]);
    }

    public void StartLocalTurns()
    {
        if (players.Count == 0)
            InitializeFromScenePlayersLocal();
        if (players.Count == 0 || IsGameOver()) return;

        SkipFinished();
        OnTurnStarted?.Invoke(players[currentIndex]);
    }

    public void ServerAdvanceTurn()
    {
        if (!IsServer || players.Count == 0 || IsGameOver()) return;
        currentIndex = (currentIndex + 1) % players.Count;
        SkipFinished();
        if (!IsGameOver())
            OnTurnStarted?.Invoke(players[currentIndex]);
    }

    public void AdvanceLocalTurn()
    {
        if (players.Count == 0 || IsGameOver()) return;
        currentIndex = (currentIndex + 1) % players.Count;
        SkipFinished();
        if (!IsGameOver())
            OnTurnStarted?.Invoke(players[currentIndex]);
    }

    private void SkipFinished()
    {
        int safety = 0;
        while (players.Count > 0 && finished.Contains(players[currentIndex]) && safety++ < 100)
        {
            currentIndex = (currentIndex + 1) % players.Count;
        }
    }

    public void MarkPlayerFinished(PlayerManager p)
    {
        if (p != null && !finished.Contains(p))
        {
            finished.Add(p);
            Debug.Log($"✅ {p.name} finished. Place: {finished.Count}");

            // If all players are done → show leaderboard
            if (finished.Count >= players.Count - 1)
            {
                Leaderboard.SetActive(true);
                ShowLeaderboard();
                ShowPlayerSummary(p);
            }
        }
    }

    private void ShowPlayerSummary(PlayerManager p)
    {
        foreach(QuizQuestionData ques in p.QuestionsList)
        {
            GameObject obj = Instantiate(SummaryPrefab, SummaryTransform);
            obj.GetComponent<TMP_Text>().text = ques.question; // Display Question
            obj.GetComponentInChildren<TMP_Text>().text = ques.options[ques.correctAnswerIndex]; //Display Correct Answer
        }
    }
    private void ShowLeaderboard()
    {
        Debug.Log("=== 🏆 Leaderboard ===");
        int rank = 1;
        foreach (var p in finished)
        {
            Debug.Log($"{rank}. {p.name}");
            GameObject obj = Instantiate(RankingPrefab, Leaderboard_Transform);
            obj.GetComponentInChildren<TMP_Text>().text = p.NetworkManager.name;
            obj.transform.GetChild(3).GetComponentInChildren<TMP_Text>().text = $"#{rank}";
            rank++;
        }
        Debug.Log("=== Game Over ===");
    }

    private bool IsGameOver() => finished.Count >= players.Count;

    public bool IsPlayerFinished(PlayerManager p) => finished.Contains(p);
}
