using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerManager : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Vector3Int homeCellPosition;
    public int currentIndex = -1;

    [Header("Player Stats")]
    public int RemainingHints = 3;

    private Tilemap houseTilemap;

    public int CurrentIndex => currentIndex;

    private void Start()
    {
        if (IsServer)
        {
            RegisterPlayer(this);
        }
        SetPlayerInitialHomePos();
    }


    public bool HasHints() => RemainingHints > 0;
    public void UseHint() => RemainingHints = Mathf.Max(0, RemainingHints - 1);

    public void RegisterPlayer(PlayerManager player)
    {
        if (!GameManager.Instance.TotalPlayers.Contains(player))
        {
            GameManager.Instance.TotalPlayers.Add(player);

            if (IsServer)
            {
                GameManager.Instance.playerClientIds.Add(player.OwnerClientId);
            }
        }
    }

    private void SetPlayerInitialHomePos()
    {
        GameObject house = GameObject.FindGameObjectWithTag("Player House");

        if (house != null && house.TryGetComponent(out houseTilemap))
        {
            transform.position = houseTilemap.GetCellCenterWorld(homeCellPosition);
        }
    }

    public IEnumerator MovePlayerTileByTile(int stepsToMove)
    {
        Debug.Log(CurrentIndex);
        return playerMovement.MovePlayerTileByTileCO(stepsToMove, CurrentIndex);
    }
    public IEnumerator MovePlayerDirectlyToTile(int stepsToMove)
    {
        return playerMovement.MovePlayerToExactTileCO(stepsToMove);
    }

    public void SetCurrentIndex(int index)
    {
        currentIndex = Mathf.Clamp(index, 0, BoardManager.TilePositions.Count - 1);
    }
}
