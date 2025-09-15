/*using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class OnlineTurnLogic : NetworkBehaviour
{
    private List<Player> players = new();
    private int currentIndex = 0;

    public void RegisterPlayers(IEnumerable<Player> list)
    {
        players.Clear();
        if (list == null) return;
        players.AddRange(list);
        currentIndex = 0;
    }

    public Player GetCurrentPlayer()
    {
        if (players.Count == 0) return null;
        currentIndex = Mathf.Clamp(currentIndex, 0, players.Count - 1);
        return players[currentIndex];
    }

    public void EndTurn()
    {
        if (players.Count == 0) return;
        currentIndex = (currentIndex + 1) % players.Count;
        UpdateHUDs();
    }

    public void RemovePlayerFromTurn(Player p)
    {
        if (p == null) return;
        int idx = players.IndexOf(p);
        if (idx < 0) return;
        players.RemoveAt(idx);
        if (players.Count == 0) { currentIndex = 0; return; }
        currentIndex = currentIndex % players.Count;
    }
    
    private void UpdateHUDs()
    {
        if (players.Count == 0) return;

        Player current = GetCurrentPlayer();
        if (current == null) return;

        ulong localId = NetworkManager.Singleton.LocalClientId;

        foreach (var p in players)
        {
            if (p == current)
            {
                if (p.OwnerClientId == localId)
                {
                    // It's MY turn
                    p.ShowTurnHUD("Your Turn");
                }
                else
                {
                    // It's someone else's turn
                    p.ShowTurnHUD($"{current.name}'s Turn");
                }
            }
            else
            {
                p.ClearHUD();
            }
        }
    }
}*/
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class OnlineTurnLogic : NetworkBehaviour
{
    private readonly List<Player> players = new();
    private int currentIndex = 0;

    public void RegisterPlayers(IEnumerable<Player> list)
    {
        players.Clear();
        if (list == null) return;
        players.AddRange(list);
        currentIndex = 0;
        ShowCurrentTurnHUD(); // immediately show first turn
    }

    public Player GetCurrentPlayer()
    {
        if (players.Count == 0) return null;
        currentIndex = Mathf.Clamp(currentIndex, 0, players.Count - 1);
        return players[currentIndex];
    }

    /// <summary>
    /// Call this when the current player's action/turn is fully complete.
    /// </summary>
    public void EndTurn()
    {
        if (players.Count == 0) return;

        // advance turn index
        currentIndex = (currentIndex + 1) % players.Count;

        // show new player's turn
        ShowCurrentTurnHUD();
    }

    public void RemovePlayerFromTurn(Player p)
    {
        if (p == null) return;

        int idx = players.IndexOf(p);
        if (idx < 0) return;

        players.RemoveAt(idx);

        if (players.Count == 0)
        {
            currentIndex = 0;
            return;
        }

        currentIndex = currentIndex % players.Count;
        ShowCurrentTurnHUD();
    }

    /// <summary>
    /// Shows HUD for the current player until EndTurn() is called.
    /// </summary>
    private void ShowCurrentTurnHUD()
    {
        if (players.Count == 0) return;

        Player current = GetCurrentPlayer();
        if (current == null) return;

        ulong localId = NetworkManager.Singleton.LocalClientId;

        foreach (var p in players)
        {
            if (p == current)
            {
                if (p.OwnerClientId == localId)
                {
                    // It's MY turn (local player)
                   // p.ShowTurnHUD("Your Turn");
                }
                else
                {
                    string displayName = string.IsNullOrEmpty(current.PlayerName) ? $"Player {current.OwnerClientId}" : current.PlayerName;
                   // p.ShowTurnHUD($"{displayName}'s Turn");
                }
            }
            else
            {
                // For all non-current players, also show whose turn it is
                string displayName = string.IsNullOrEmpty(current.PlayerName) 
                    ? $"Player {current.OwnerClientId}" 
                    : current.PlayerName;

                // If it's not their turn, show "X's Turn"
                /*if (current.OwnerClientId == localId)
                {
                    p.ShowTurnHUD("Your Turn");
                }
                else
                {
                    p.ShowTurnHUD($"{displayName}'s Turn");
                }*/
            }
        }
    }
}
