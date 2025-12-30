using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [Header("Player meta")] public string PlayerName = "Player";

    public NetworkVariable<FixedString64Bytes> NetworkPlayerName = new NetworkVariable<FixedString64Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> NetworkColorIndex = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool IsCpu = false;
    public Color Color = Color.white;
    public SpriteRenderer PlayerSprite;
    public GameObject ShieldVisual;
    [HideInInspector] public List<QuizQuestionData> QuestionsList = new();
    public int MovesTaken;
    public PlayerMovement Movement { get; private set; }

    private void Awake()
    {
        Movement = GetComponent<PlayerMovement>();
    }

    public ulong OwnerClientId => GetComponent<NetworkObject>()?.OwnerClientId ?? 0ul;

    public void ApplyColor(Color c)
    {
        Color = c;
        if (PlayerSprite != null) PlayerSprite.color = c;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            string authName = AuthExtensions.GetCachedPlayerName();
            SendNameToServerServerRpc(authName);
        }

        NetworkPlayerName.OnValueChanged += (oldVal, newVal) =>
        {
            PlayerName = newVal.ToString();
            var manager = FindFirstObjectByType<MultiplayerFlowManager>();
            manager?.UpdateHudName(this, PlayerName);
        };

        NetworkColorIndex.OnValueChanged += (oldVal, newVal) =>
        {
            var manager = FindFirstObjectByType<MultiplayerFlowManager>();
            if (manager != null && newVal >= 0 && newVal < manager.PlayerColors.Length)
            {
                Color c = manager.PlayerColors[newVal];
                ApplyColor(c);
                manager.UpdateHudColor(this, c);
            }
        };

        // apply initial values if present
        if (NetworkPlayerName.Value.Length > 0)
        {
            PlayerName = NetworkPlayerName.Value.ToString();
            FindFirstObjectByType<MultiplayerFlowManager>()?.UpdateHudName(this, PlayerName);
        }

        if (NetworkColorIndex.Value >= 0)
        {
            var manager = FindFirstObjectByType<MultiplayerFlowManager>();
            if (manager != null && NetworkColorIndex.Value < manager.PlayerColors.Length)
            {
                Color c = manager.PlayerColors[NetworkColorIndex.Value];
                ApplyColor(c);
                manager.UpdateHudColor(this, c);
                // ✅ Delay re-apply once network state is fully synced
                StartCoroutine(ForceColorApplyNextFrame());
            }
        }
        /*if (NetworkColorIndex.Value >= 0)
        {
            var manager = FindFirstObjectByType<MultiplayerFlowManager>();
            if (manager != null && NetworkColorIndex.Value < manager.PlayerColors.Length)
            {
                ApplyColor(manager.PlayerColors[NetworkColorIndex.Value]);
                manager.UpdateHudColor(this, manager.PlayerColors[NetworkColorIndex.Value]);
            }
        }*/


        if (IsServer)
        {
            FindFirstObjectByType<MultiplayerFlowManager>()?.RegisterPlayerServerRpc(OwnerClientId, NetworkObjectId);
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void SendNameToServerServerRpc(string name)
    {
        NetworkPlayerName.Value = name;
    }

    private IEnumerator ForceColorApplyNextFrame()
    {
        yield return null; // wait one frame so Netcode syncs
        if (NetworkColorIndex.Value >= 0)
        {
            var manager = FindFirstObjectByType<MultiplayerFlowManager>();
            if (manager != null && NetworkColorIndex.Value < manager.PlayerColors.Length)
            {
                Color c = manager.PlayerColors[NetworkColorIndex.Value];
                ApplyColor(c);
                manager.UpdateHudColor(this, c);
                Debug.Log($"[ForceApply] Applied color {c} after sync for {PlayerName}");
            }
        }
    }
    public void ShowShield(bool show)
    {
        if (ShieldVisual != null)
            ShieldVisual.SetActive(show);
    }
}
