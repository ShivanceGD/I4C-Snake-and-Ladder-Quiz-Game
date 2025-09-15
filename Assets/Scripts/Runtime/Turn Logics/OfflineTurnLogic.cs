/*using System.Collections.Generic;
using UnityEngine;

public class OfflineTurnLogic : MonoBehaviour
{
    private readonly List<Player> players = new();
    private int currentIndex;

    public int PlayerCount => players.Count;

    public void RegisterPlayers(IEnumerable<Player> list)
    {
        players.Clear();
        if (list == null) return;
        players.AddRange(list);
        currentIndex = 0;
        ShowCurrentTurnHUD(); // show first turn immediately
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
        if (!p) return;

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

    public bool IsEmpty() => players.Count == 0;

    /// <summary>
    /// Updates HUD for current player, keeping it visible until EndTurn() is called.
    /// </summary>
    private void ShowCurrentTurnHUD()
    {
        if (players.Count == 0) return;

        Player current = GetCurrentPlayer();
        if (current == null) return;

        // Human player turn
        if (!current.IsCpu)
        {
            foreach (var p in players)
            {
                if (p == current)
                    p.ShowTurnHUD("Your Turn");
                else
                    p.ShowTurnHUD($"{p.PlayerName}'s turn");
            }
        }
        // CPU turn
        else
        {
            foreach (var p in players)
            {
                if (p == current)
                {
                    // If it's your turn and you're not CPU → "Your Turn"
                    if (!p.IsCpu)
                        p.ShowTurnHUD("Your Turn");
                    else
                        p.ShowTurnHUD("CPU Turn");
                }
                else
                {
                    // Everyone else sees "<Name>'s Turn"
                    p.ShowTurnHUD($"{current.PlayerName}'s Turn");
                }
            }
        }
    }
}*/
using System.Collections.Generic;
using UnityEngine;

public class OfflineTurnLogic : MonoBehaviour
{
    private readonly List<Player> players = new ();
    private int currentIndex;

    public int PlayerCount => players.Count;

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
    }

    public void RemovePlayerFromTurn(Player p)
    {
        if (!p) return;
        int idx = players.IndexOf(p);
        if (idx < 0) return;
        players.RemoveAt(idx);
        if (players.Count == 0) { currentIndex = 0; return; }
        currentIndex = currentIndex % players.Count;
    }

    public bool IsEmpty() => players.Count == 0;
    
   
    }
