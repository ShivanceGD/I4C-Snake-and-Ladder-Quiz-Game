/*
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
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
    public List<QuizQuestionData> QuestionsList = new List<QuizQuestionData>();
    
    [SerializeField]
    private NetworkVariable<int> networkTileIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ✅ Network synced player color
    public NetworkVariable<Color> PlayerColor = new NetworkVariable<Color>(
        Color.white, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private int localHints;
    public SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        localHints = startingHints;

        // Apply initial color (sync across clients automatically via callback)
        PlayerColor.OnValueChanged += OnColorChanged;
        OnColorChanged(Color.white, PlayerColor.Value);

        if (IsServer)
        {
            RegsiterPlayer(); // Register with GameManager
        }

        SetPlayerInitialHomePos();
    }

    private void OnDestroy()
    {
        PlayerColor.OnValueChanged -= OnColorChanged;
    }

    private void OnColorChanged(Color oldColor, Color newColor)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = newColor;
    }

    [ContextMenu("Register Player")]
    public void RegsiterPlayer()
    {
        GameManager.Instance?.RegisterPlayer(this);
    }

    public void SetPlayerTileIndex(int idx)
    {
        bool offline = (NetworkManager.Singleton == null) || (GameManager.Instance != null && GameManager.Instance.isOfflineMode);

        if (IsServer || offline)
        {
            int clamped = Mathf.Clamp(idx, 0, BoardManager.TilePositions.Count - 1);
            networkTileIndex.Value = clamped;
            
            
        }
        else
        {
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
        GameObject house = GameObject.FindGameObjectWithTag("House");
        if (house != null && house.TryGetComponent(out houseTilemap))
        {
            transform.position = houseTilemap.GetCellCenterWorld(homeCellPosition);
        }
    }

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
*/
