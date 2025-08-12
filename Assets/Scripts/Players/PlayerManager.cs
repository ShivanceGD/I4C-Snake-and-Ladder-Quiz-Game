using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Vector3Int homeCellPosition;
    [SerializeField] private Tilemap houseTilemap;


    [Header("Player Stats")]
    public int RemainingHints = 3;
    public int currentIndex = -1;

    public bool HasHints() => RemainingHints > 0;
    public void UseHint() => RemainingHints = Mathf.Max(0, RemainingHints - 1);

    private void Start()
    {
        if (IsServer)
        {
            RegisterPlayer(this);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        SetPlayerInitialHomePos();
    }

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

    [ContextMenu("Set Home Position")]
    public void SetPlayerInitialHomePos()
    {
        GameObject house = GameObject.FindGameObjectWithTag("House");
        
        if (house != null && house.TryGetComponent(out houseTilemap))
        {
            Debug.Log("House Found");
            transform.position = houseTilemap.GetCellCenterWorld(homeCellPosition);
        }
    }

    public IEnumerator MovePlayerTileByTile(int stepsToMove)
    {
        return playerMovement.MovePlayerTileByTileCO(stepsToMove, currentIndex);
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
