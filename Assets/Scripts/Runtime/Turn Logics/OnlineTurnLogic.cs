using System.Collections.Generic;
using UnityEngine;

public class OnlineTurnLogic : MonoBehaviour
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
}