using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerManager))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    private PlayerManager pm;

    private int totalTiles => BoardManager.TilePositions.Count;

    private void Awake() => pm = GetComponent<PlayerManager>();

    public IEnumerator MovePlayerTileByTileCO(int steps, int currentIndex)
    {
        WaitForSeconds stepDelay = new WaitForSeconds(0.1f);
        int idx = currentIndex;
        for (int i = 0; i < steps; i++)
        {
            idx++;
            if (idx >= totalTiles) { idx = totalTiles - 1; break; }
            Vector3 target = BoardManager.GetTilePosition(idx);
            yield return StartCoroutine(MoveToTileCO(target, idx));
            yield return stepDelay;
        }
    }

    public IEnumerator MovePlayerToExactTileCO(int newIndex)
    {
        if (newIndex < 0 || newIndex >= totalTiles) yield break;
        Vector3 target = BoardManager.GetTilePosition(newIndex);
        yield return StartCoroutine(MoveToTileCO(target, newIndex));
    }

    private IEnumerator MoveToTileCO(Vector3 target, int indexNow)
    {
        bool soundPlayed = false;
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            if (!soundPlayed && SoundManager.Instance != null) { SoundManager.Instance.PlayStepSound(); soundPlayed = true; }
            yield return null;
        }
        transform.position = target;

        // Update tile index:
        // - server (MP), or
        // - offline (no NetworkManager or GameManager.isOfflineMode)
        bool offline = (NetworkManager.Singleton == null) || (GameManager.Instance != null && GameManager.Instance.isOfflineMode);
        if (offline || (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer))
        {
            var pmComp = GetComponent<PlayerManager>();
            pmComp.SetPlayerTileIndex(indexNow);
        }
    }
}
