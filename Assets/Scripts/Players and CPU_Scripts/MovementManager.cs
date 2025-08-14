using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class MovementManager : NetworkBehaviour
{
    [Header("Dependencies")]
    public TurnManager turnManager;

    [Header("Settings")]
    public float endTurnDelay = 0.5f;

    // Server entry
    public void ProcessPostQuizMovementServer(PlayerManager player, bool isCorrect, Difficulty difficulty, float timeTaken)
    {
        if (!IsServer) { Debug.LogWarning("Call on server only."); return; }
        StartCoroutine(MoveCoroutine(player, isCorrect, difficulty, timeTaken));
    }

    private IEnumerator MoveCoroutine(PlayerManager player, bool isCorrect, Difficulty difficulty, float timeTaken)
    {
        if (player == null) yield break;
        var board = LevelManager.Instance.CurrentLevel.BoardJointsSCO;
        int currentTile = player.GetPlayerCurrentTileIndex();

        if (isCorrect)
        {
            foreach (var ladder in board.Ladders)
            {
                if (ladder.Key > currentTile && ladder.Key - currentTile <= 5)
                {
                    yield return player.MovePlayerTileByTile((ladder.Key - 1) - currentTile);
                    yield return player.MovePlayerDirectlyToTile(ladder.Value - 1);
                    CheckWin(player);
                    yield return new WaitForSeconds(endTurnDelay);
                    turnManager.ServerAdvanceTurn();
                    yield break;
                }
            }
        }
        else
        {
            foreach (var snake in board.Snakes)
            {
                if (snake.Key >= currentTile && snake.Key - currentTile <= 5)
                {
                    yield return player.MovePlayerTileByTile((snake.Key - 1) - currentTile);
                    yield return player.MovePlayerDirectlyToTile(snake.Value - 1);
                    CheckWin(player);
                    yield return new WaitForSeconds(endTurnDelay);
                    turnManager.ServerAdvanceTurn();
                    yield break;
                }
            }
        }

        int steps = GetSteps(isCorrect, difficulty, timeTaken);
        if (steps > 0)
        {
            yield return player.MovePlayerTileByTile(steps);
            CheckWin(player);
        }

        yield return new WaitForSeconds(endTurnDelay);
        turnManager.ServerAdvanceTurn();
    }

    private void CheckWin(PlayerManager player)
    {
        if (player.GetPlayerCurrentTileIndex() >= BoardManager.WinningTileIndex && !turnManager.IsPlayerFinished(player))
        {
            player.SetPlayerTileIndex(BoardManager.WinningTileIndex);
            turnManager.MarkPlayerFinished(player);
            Debug.Log($"🎉 {player.name} finished.");
        }
    }

    private int GetSteps(bool correct, Difficulty difficulty, float timeTaken)
    {
        if (!correct) return 0;
        if (!LevelManager.Instance.CurrentLevel.DiceRollRangePerQuizDifficulty.TryGetValue(difficulty, out var range))
        {
            Debug.LogWarning($"Missing dice range for {difficulty}, defaulting to 1.");
            return 1;
        }
        int s = Random.Range(range.x, range.y + 1);
        if (timeTaken < 5f) s++;
        return s;
    }
}
