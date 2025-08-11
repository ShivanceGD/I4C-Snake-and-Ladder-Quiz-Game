using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerManager))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerManager playerManager;

    [Header("Player Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;

    private int totalTilesCount => BoardManager.TilePositions.Count;

    private void OnEnable()
    {
        playerManager = gameObject.GetComponent<PlayerManager>();
    }

    public IEnumerator MovePlayerTileByTileCO(int steps, int currentIndex)
    {
        WaitForSeconds delay = new(0.1f);
        for (int i = 0; i < steps; i++)
        {
            currentIndex++;
            if (currentIndex >= totalTilesCount)
            {
                currentIndex = totalTilesCount - 1;
                break;
            }
            Vector3 target = BoardManager.GetTilePosition(currentIndex);

            yield return StartCoroutine(MoveToTileCO(target, currentIndex));
            yield return delay;
        }
    }

    public IEnumerator MovePlayerToExactTileCO(int newIndex)
    {
        if (newIndex < 0 || newIndex >= totalTilesCount) yield break;

        Vector3 target = BoardManager.GetTilePosition(newIndex);
        yield return StartCoroutine(MoveToTileCO(target, newIndex));
    }

    private IEnumerator MoveToTileCO(Vector3 target, int indexNow)
    {
        bool soundPlayed = false;
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            if (!soundPlayed)
            {
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayStepSound();
                }
                soundPlayed = true;
            }
            yield return null;
        }
        transform.position = target;
        playerManager.currentIndex = indexNow;
    }
}
