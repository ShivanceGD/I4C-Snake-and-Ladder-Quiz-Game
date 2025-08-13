using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerManager : NetworkBehaviour
{
    [Header("References")]
    public PlayerMovement playerMovement;
    public Vector3Int homeCellPosition;
    public Tilemap houseTilemap;

    [Header("Stats")]
    public int startingHints = 3;

    // network-synced tile index (server authoritative writes)
    private NetworkVariable<int> networkTileIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private int localHints;

    private void Awake() { if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>(); }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        localHints = startingHints;

        // register with GameManager (server & local)
        //RegsiterPlayer();

        SetPlayerInitialHomePos();
    }

    [ContextMenu("Register Player")]
    public void RegsiterPlayer()
    {
        GameManager.Instance?.RegisterPlayer(this);

        if (!IsOwner && NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            // clients may rely on server updates to tileIndex; subscribe if needed
        }
    }

    public void SetPlayerTileIndex(int idx)
    {
        if (IsServer)
        {
            int clamped = Mathf.Clamp(idx, 0, BoardManager.TilePositions.Count - 1);
            networkTileIndex.Value = clamped;
        }
        else
        {
            // request server to set (optional): for simplicity server control only
            Debug.LogWarning("Client attempted to set tile index directly; operation is server-only.");
        }
    }

    public int GetPlayerCurrentTileIndex() => networkTileIndex.Value;

    public bool HasHints() => localHints > 0;
    public void UseHint() => localHints = Mathf.Max(0, localHints - 1);

    public IEnumerator MovePlayerTileByTile(int stepsToMove) => playerMovement.MovePlayerTileByTileCO(stepsToMove, GetPlayerCurrentTileIndex());
    public IEnumerator MovePlayerDirectlyToTile(int newIndex) => playerMovement.MovePlayerToExactTileCO(newIndex);

    [ContextMenu("Set Home Position")]
    public void SetPlayerInitialHomePos()
    {
        GameObject house = GameObject.FindGameObjectWithTag("Player House");
        if (house != null && house.TryGetComponent(out houseTilemap))
        {
            transform.position = houseTilemap.GetCellCenterWorld(homeCellPosition);
        }
    }

    // Helpers used by NetworkFlowManager
    public static PlayerManager GetLocalPlayer()
    {
        if (NetworkManager.Singleton == null)
        {
            return FindAnyObjectByType<PlayerManager>();
        }

        foreach (var p in FindObjectsByType<PlayerManager>(FindObjectsSortMode.None))
        {
            if (p.OwnerClientId == NetworkManager.Singleton.LocalClientId) return p;
        }
        return null;
    }

    public static PlayerManager GetByClientId(ulong clientId)
    {
        foreach (var p in FindObjectsByType<PlayerManager>(FindObjectsSortMode.None))
        {
            if (p.OwnerClientId == clientId) return p;
        }
        return null;
    }
}
