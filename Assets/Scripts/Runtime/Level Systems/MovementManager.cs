using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementManager : MonoBehaviour
{
    public float endTurnDelay = 0.45f;
    private OfflineFlowManager offlinemanager;
    private IFlowManager FlowManager;
    public void ProcessPostQuizMovement(
        Player player,
        bool isCorrect,
        QuestionsDifficulty questionsDifficulty,
        float timeTaken,
        BoardDataSO board,
        DifficultyStepRange diceRollRange,
        Action<MovementResult> onComplete)
    {
        StartCoroutine(MoveCoroutine(player, isCorrect, questionsDifficulty, timeTaken, board, diceRollRange, onComplete));
    }

    private void Start()
    {
        FlowManager = FindFirstObjectByType<OfflineFlowManager>();
        if (FlowManager == null)
            FlowManager = FindFirstObjectByType<MultiplayerFlowManager>();
    }

    private IEnumerator MoveCoroutine(
        Player player,
        bool isCorrect,
        QuestionsDifficulty questionsDifficulty,
        float timeTaken,
        BoardDataSO board,
        DifficultyStepRange diceRollRange,
        Action<MovementResult> onComplete)
    {
        if (player == null)
        {
            onComplete?.Invoke(new MovementResult
            {
                Player = null,
                StepsTaken = 0,
                FinalTileIndex = 0,
                Finished = false,
                UsedSnakeOrLadder = false,
                Reason = "InvalidPlayer"
            });
            yield break;
        }

        // clear last HUD state at the start of movement
        int currentTile = BoardLogicManager.GetTileIndexFromPosition(player.transform.position);
        int tileCount = Math.Max(1, Mathf.Max(1, BoardLogicManager.TilePositions.Count)); // defensive

        // normalize snakes and ladders into zero-based dictionaries
        var laddersNormalized = new Dictionary<int, int>();
        var snakesNormalized = new Dictionary<int, int>();
        NormalizeBoardIndices(board, tileCount, ref laddersNormalized, ref snakesNormalized);

        var result = new MovementResult
        {
            Player = player,
            StepsTaken = 0,
            FinalTileIndex = currentTile,
            Finished = false,
            UsedSnakeOrLadder = false,
            Reason = "None"
        };

        // IMMUNITY CHECK (snake immune)
        /*if (!isCorrect && player.ImmuneToSnake)
        {
            result.Reason = "Immune";
            player.ShowStateHUD("Immune to Snake!");
            yield return new WaitForSeconds(endTurnDelay);
            onComplete?.Invoke(result);
            yield break;
        }*/

        // LADDER (if correct)
        if (isCorrect && laddersNormalized.Count > 0)
        {
            foreach (var kv in laddersNormalized)
            {
                int ladderStart = kv.Key; 
                int ladderEnd = kv.Value;
                if (ladderStart > currentTile && ladderStart - currentTile <= 5)
                {
                    int toMove = ladderStart - currentTile;
                    if (toMove > 0) yield return player.Movement.MovePlayerTileByTile(toMove);
                    SoundManager.Instance?.PlayLadderSound();
                    yield return player.Movement.MovePlayerDirectlyToTile(ladderEnd);

                    result.StepsTaken = toMove;
                    result.FinalTileIndex = ladderEnd;
                    result.Finished = result.FinalTileIndex >= BoardLogicManager.GetWinningTileIndex;
                    result.UsedSnakeOrLadder = true;
                    result.Reason = "Ladder";
                    
                    FlowManager.UpdateHUD(toMove,false,true);
                    yield return new WaitForSeconds(endTurnDelay);
                    onComplete?.Invoke(result);
                    yield break;
                }
            }
        }

        // SNAKE (if incorrect)
        if (!isCorrect && snakesNormalized.Count > 0)
        {
            foreach (var kv in snakesNormalized)
            {
                int snakeHead = kv.Key; 
                int snakeTail = kv.Value;
                if (snakeHead >= currentTile && snakeHead - currentTile <= 5)
                {
                    int forward = snakeHead - currentTile;
                    if (forward > 0) yield return player.Movement.MovePlayerTileByTile(forward);
                    SoundManager.Instance?.PlaySnakeSound();
                    yield return player.Movement.MovePlayerDirectlyToTile(snakeTail);

                    result.StepsTaken = forward;
                    result.FinalTileIndex = snakeTail;
                    result.Finished = result.FinalTileIndex >= BoardLogicManager.GetWinningTileIndex;
                    result.UsedSnakeOrLadder = true;
                    result.Reason = "Snake";

                    FlowManager.UpdateHUD(forward,true,false);

                    yield return new WaitForSeconds(endTurnDelay);
                    onComplete?.Invoke(result);
                    yield break;
                }
            }
        }

        // NORMAL movement (dice)
        int steps = GetSteps(isCorrect, questionsDifficulty, timeTaken, diceRollRange);
        if (steps > 0)
        {
            yield return player.Movement.MovePlayerTileByTile(steps);
            result.StepsTaken = steps;
            result.FinalTileIndex = BoardLogicManager.GetTileIndexFromPosition(player.transform.position);
            result.Finished = result.FinalTileIndex >= BoardLogicManager.GetWinningTileIndex;
            result.Reason = "Normal";

            FlowManager.UpdateHUD(steps,false,false);
        }

        yield return new WaitForSeconds(endTurnDelay);
        onComplete?.Invoke(result);
    }

    private void NormalizeBoardIndices(BoardDataSO board, int tileCount, ref Dictionary<int, int> laddersOut, ref Dictionary<int, int> snakesOut)
    {
        laddersOut = new Dictionary<int, int>();
        snakesOut = new Dictionary<int, int>();
        if (board == null) return;

        bool laddersNull = board.Ladders == null || board.Ladders.Count == 0;
        bool snakesNull = board.Snakes == null || board.Snakes.Count == 0;

        if (!laddersNull)
        {
            bool laddersAreOneBased = false;
            foreach (var kv in board.Ladders)
            {
                if (kv.Key >= tileCount || kv.Value >= tileCount) { laddersAreOneBased = true; break; }
            }

            foreach (var kv in board.Ladders)
            {
                int k = kv.Key - (laddersAreOneBased ? 1 : 0);
                int v = kv.Value - (laddersAreOneBased ? 1 : 0);
                k = Mathf.Clamp(k, 0, Math.Max(0, tileCount - 1));
                v = Mathf.Clamp(v, 0, Math.Max(0, tileCount - 1));
                if (!laddersOut.ContainsKey(k)) laddersOut[k] = v;
            }
        }

        if (!snakesNull)
        {
            bool snakesAreOneBased = false;
            foreach (var kv in board.Snakes)
            {
                if (kv.Key >= tileCount || kv.Value >= tileCount) { snakesAreOneBased = true; break; }
            }

            foreach (var kv in board.Snakes)
            {
                int k = kv.Key - (snakesAreOneBased ? 1 : 0);
                int v = kv.Value - (snakesAreOneBased ? 1 : 0);
                k = Mathf.Clamp(k, 0, Math.Max(0, tileCount - 1));
                v = Mathf.Clamp(v, 0, Math.Max(0, tileCount - 1));
                if (!snakesOut.ContainsKey(k)) snakesOut[k] = v;
            }
        }
    }

    private int GetSteps(bool correct, QuestionsDifficulty questionsDifficulty, float timeTaken, DifficultyStepRange diceRollRange)
    {
        if (!correct) return 0;
        if (diceRollRange == null)
        {
            Debug.LogWarning("[MovementManager] diceRollRange is null, defaulting to 1.");
            return 1;
        }
        if (!diceRollRange.TryGetValue(questionsDifficulty, out var range))
        {
            Debug.LogWarning($"[MovementManager] Missing dice range for {questionsDifficulty}, defaulting to 1.");
            return 1;
        }
        int s = UnityEngine.Random.Range(range.x, range.y + 1);
        if (timeTaken < 5f) s++;
        FlowManager.UpdateHUD(s,false,false);
        return s;
    }
}

/*
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementManager : MonoBehaviour
{
    public float endTurnDelay = 0.45f;

    public void ProcessPostQuizMovement(Player player, bool isCorrect, QuestionsDifficulty questionsDifficulty, float timeTaken, BoardDataSO board, DifficultyStepRange diceRollRange, Action<MovementResult> onComplete)
    {
        StartCoroutine(MoveCoroutine(player, isCorrect, questionsDifficulty, timeTaken, board, diceRollRange, onComplete));
    }

    private IEnumerator MoveCoroutine(Player player, bool isCorrect, QuestionsDifficulty questionsDifficulty, float timeTaken, BoardDataSO board, DifficultyStepRange diceRollRange, Action<MovementResult> onComplete)
    {
        if (player == null)
        {
            onComplete?.Invoke(new MovementResult { Player = null, StepsTaken = 0, FinalTileIndex = 0, Finished = false, UsedSnakeOrLadder = false, Reason = "InvalidPlayer" });
            yield break;
        }

        int currentTile = BoardLogicManager.GetTileIndexFromPosition(player.transform.position);
        int tileCount = Math.Max(1, UnityEngine.Mathf.Max(1, BoardLogicManager.TilePositions.Count)); // defensive

        // normalize snakes and ladders into zero-based dictionaries
        var laddersNormalized = new Dictionary<int, int>();
        var snakesNormalized = new Dictionary<int, int>();
        NormalizeBoardIndices(board, tileCount, ref laddersNormalized, ref snakesNormalized);

        var result = new MovementResult
        {
            Player = player,
            StepsTaken = 0,
            FinalTileIndex = currentTile,
            Finished = false,
            UsedSnakeOrLadder = false,
            Reason = "None"
        };

        // LADDER (if correct)
        if (isCorrect && laddersNormalized.Count > 0)
        {
            foreach (var kv in laddersNormalized)
            {
                int ladderStart = kv.Key; // zero-based start index
                int ladderEnd = kv.Value; // zero-based end index
                if (ladderStart > currentTile && ladderStart - currentTile <= 5)
                {
                    int toMove = ladderStart - currentTile;
                    if (toMove > 0) yield return player.Movement.MovePlayerTileByTile(toMove);
                    yield return player.Movement.MovePlayerDirectlyToTile(ladderEnd);
                    SoundManager.Instance?.PlayLadderSound();

                    result.StepsTaken = toMove;
                    result.FinalTileIndex = ladderEnd;
                    result.Finished = result.FinalTileIndex >= BoardLogicManager.GetWinningTileIndex;
                    result.UsedSnakeOrLadder = true;
                    result.Reason = "Ladder";

                    yield return new WaitForSeconds(endTurnDelay);
                    onComplete?.Invoke(result);
                    yield break;
                }
            }
        }

        // SNAKE (if incorrect)
        if (!isCorrect && snakesNormalized.Count > 0)
        {
            foreach (var kv in snakesNormalized)
            {
                int snakeHead = kv.Key; // zero-based head index
                int snakeTail = kv.Value; // zero-based tail index
                if (snakeHead >= currentTile && snakeHead - currentTile <= 5)
                {
                    int forward = snakeHead - currentTile;
                    if (forward > 0) yield return player.Movement.MovePlayerTileByTile(forward);
                    yield return player.Movement.MovePlayerDirectlyToTile(snakeTail);
                    SoundManager.Instance?.PlaySnakeSound();

                    result.StepsTaken = forward;
                    result.FinalTileIndex = snakeTail;
                    result.Finished = result.FinalTileIndex >= BoardLogicManager.GetWinningTileIndex;
                    result.UsedSnakeOrLadder = true;
                    result.Reason = "Snake";

                    yield return new WaitForSeconds(endTurnDelay);
                    onComplete?.Invoke(result);
                    yield break;
                }
            }
        }

        // NORMAL movement (dice)
        int steps = GetSteps(isCorrect, questionsDifficulty, timeTaken, diceRollRange);
        if (steps > 0)
        {
            yield return player.Movement.MovePlayerTileByTile(steps);
            result.StepsTaken = steps;
            result.FinalTileIndex = BoardLogicManager.GetTileIndexFromPosition(player.transform.position);
            result.Finished = result.FinalTileIndex >= BoardLogicManager.GetWinningTileIndex;
            result.Reason = "Normal";
        }

        yield return new WaitForSeconds(endTurnDelay);
        onComplete?.Invoke(result);
    }

    private void NormalizeBoardIndices(BoardDataSO board, int tileCount, ref Dictionary<int,int> laddersOut, ref Dictionary<int,int> snakesOut)
    {
        laddersOut = new Dictionary<int, int>();
        snakesOut = new Dictionary<int, int>();
        if (board == null) return;

        bool laddersNull = board.Ladders == null || board.Ladders.Count == 0;
        bool snakesNull = board.Snakes == null || board.Snakes.Count == 0;

        if (!laddersNull)
        {
            // detect 1-based vs 0-based by checking if any key/value is >= tileCount
            bool laddersAreOneBased = false;
            foreach (var kv in board.Ladders)
            {
                if (kv.Key >= tileCount || kv.Value >= tileCount) { laddersAreOneBased = true; break; }
            }

            foreach (var kv in board.Ladders)
            {
                int k = kv.Key - (laddersAreOneBased ? 1 : 0);
                int v = kv.Value - (laddersAreOneBased ? 1 : 0);
                k = Mathf.Clamp(k, 0, Math.Max(0, tileCount - 1));
                v = Mathf.Clamp(v, 0, Math.Max(0, tileCount - 1));
                if (!laddersOut.ContainsKey(k)) laddersOut[k] = v;
            }
        }

        if (!snakesNull)
        {
            bool snakesAreOneBased = false;
            foreach (var kv in board.Snakes)
            {
                if (kv.Key >= tileCount || kv.Value >= tileCount) { snakesAreOneBased = true; break; }
            }

            foreach (var kv in board.Snakes)
            {
                int k = kv.Key - (snakesAreOneBased ? 1 : 0);
                int v = kv.Value - (snakesAreOneBased ? 1 : 0);
                k = Mathf.Clamp(k, 0, Math.Max(0, tileCount - 1));
                v = Mathf.Clamp(v, 0, Math.Max(0, tileCount - 1));
                if (!snakesOut.ContainsKey(k)) snakesOut[k] = v;
            }
        }
    }

    private int GetSteps(bool correct, QuestionsDifficulty questionsDifficulty, float timeTaken, DifficultyStepRange diceRollRange)
    {
        if (!correct) return 0;
        if (diceRollRange == null)
        {
            Debug.LogWarning("[MovementManager] diceRollRange is null, defaulting to 1.");
            return 1;
        }
        if (!diceRollRange.TryGetValue(questionsDifficulty, out var range))
        {
            Debug.LogWarning($"[MovementManager] Missing dice range for {questionsDifficulty}, defaulting to 1.");
            return 1;
        }
        int s = UnityEngine.Random.Range(range.x, range.y + 1);
        if (timeTaken < 5f) s++;
        return s;
    }
}
*/
