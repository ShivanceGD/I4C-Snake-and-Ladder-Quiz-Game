using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float stepDuration = 0.18f;

    public IEnumerator MovePlayerTileByTile(int steps)
    {
        if (steps <= 0) yield break;
        for (int i = 0; i < steps; i++)
        {
            int currentIdx = BoardLogicManager.GetTileIndexFromPosition(transform.position);
            int nextIdx = Mathf.Min(BoardLogicManager.GetWinningTileIndex, currentIdx + 1);
            Vector3 target = BoardLogicManager.GetTilePosition(nextIdx);
            float elapsed = 0f;
            Vector3 start = transform.position;
            while (elapsed < stepDuration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, target, elapsed / stepDuration);
                yield return null;
            }
            transform.position = target;
            yield return null;
        }
    }

    public IEnumerator MovePlayerDirectlyToTile(int targetIndex)
    {
        Vector3 target = BoardLogicManager.GetTilePosition(targetIndex);
        float elapsed = 0f;
        float duration = Mathf.Max(0.12f, stepDuration * 1.2f);
        Vector3 start = transform.position;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        transform.position = target;
        yield return null;
    }
}