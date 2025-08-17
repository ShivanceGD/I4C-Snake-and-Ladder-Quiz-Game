using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TurnManager : NetworkBehaviour
{
    public event Action<PlayerManager> OnTurnStarted;

    public readonly List<PlayerManager> players = new();
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
        foreach (var p in all)
        {
            players.Add(p);
            p.SetPlayerInitialHomePos();
        }
        currentIndex = 0;
    }

    // Local init (offline)
    public void InitializeFromScenePlayersLocal()
    {
        players.Clear();
        var all = FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);
        foreach (var p in all)
        {
            players.Add(p);
            p.SetPlayerInitialHomePos();
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
            if (finished.Count >= players.Count)
            {
                ShowLeaderboard();
            }
        }
    }

    private void ShowLeaderboard()
    {
        Debug.Log("=== 🏆 Leaderboard ===");
        int rank = 1;
        foreach (var p in finished)
        {
            Debug.Log($"{rank}. {p.name}");
            rank++;
        }
        Debug.Log("=== Game Over ===");
    }

    private bool IsGameOver() => finished.Count >= players.Count;

    public bool IsPlayerFinished(PlayerManager p) => finished.Contains(p);
}
