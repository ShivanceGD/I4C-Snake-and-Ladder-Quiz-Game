using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Players : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private Vector3Int homeCellPosition;
    [SerializeField] private int currentIndex = -1;

    [Header("Player Stats")]
    public int RemainingHints = 3;

    private Tilemap houseTilemap;

    public int CurrentIndex => currentIndex;

    private void Start()
    {
        if (IsServer)
        {
            GameManager.Instance.RegisterPlayer(this);
        }

        if (GameObject.FindGameObjectWithTag("Player House")?.TryGetComponent(out houseTilemap) == true)
        {
            transform.position = houseTilemap.GetCellCenterWorld(homeCellPosition);
        }
    }

    public bool CanUseHint() => RemainingHints > 0;
    public void UseHint() => RemainingHints = Mathf.Max(0, RemainingHints - 1);

    public IEnumerator MovePlayerToDestinationTile(int steps)
    {
        WaitForSeconds delay = new WaitForSeconds(0.1f);
        for (int i = 0; i < steps; i++)
        {
            currentIndex++;
            if (currentIndex >= BoardManager.tilePositions.Count)
            {
                currentIndex = BoardManager.tilePositions.Count - 1;
                break;
            }
            Vector3 target = BoardManager.tilePositions[currentIndex];
            yield return StartCoroutine(MoveToTile(target));
            yield return delay;
        }
    }

    public IEnumerator MovePlayerToExactIndex(int newIndex)
    {
        if (newIndex < 0 || newIndex >= BoardManager.tilePositions.Count) yield break;
        currentIndex = newIndex;
        Vector3 target = BoardManager.tilePositions[currentIndex];
        yield return StartCoroutine(MoveToTile(target));
    }

    private IEnumerator MoveToTile(Vector3 target)
    {
        bool soundPlayed = false;
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            if (!soundPlayed)
            {
                SoundManager.Instance?.PlayStepSound();
                soundPlayed = true;
            }
            yield return null;
        }
        transform.position = target;
    }

    public void SetCurrentIndex(int index)
    {
        currentIndex = Mathf.Clamp(index, 0, BoardManager.tilePositions.Count - 1);
    }
}
