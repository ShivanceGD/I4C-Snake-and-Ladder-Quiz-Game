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

    // NEW: Local initialization (offline)
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
        if (!IsServer || players.Count == 0) return;
        SkipFinished();
        OnTurnStarted?.Invoke(players[currentIndex]);
    }

    // NEW: Local start (offline)
    public void StartLocalTurns()
    {
        if (players.Count == 0)
            InitializeFromScenePlayersLocal();
        if (players.Count == 0) return;

        SkipFinished();
        OnTurnStarted?.Invoke(players[currentIndex]);
    }

    public void ServerAdvanceTurn()
    {
        if (!IsServer || players.Count == 0) return;
        currentIndex = (currentIndex + 1) % players.Count;
        SkipFinished();
        OnTurnStarted?.Invoke(players[currentIndex]);
    }

    public void AdvanceLocalTurn()
    {
        if (players.Count == 0) return;
        currentIndex = (currentIndex + 1) % players.Count;
        SkipFinished();
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

    public void MarkPlayerFinished(PlayerManager p) { if (p != null) finished.Add(p); }
    public bool IsPlayerFinished(PlayerManager p) => finished.Contains(p);
}
