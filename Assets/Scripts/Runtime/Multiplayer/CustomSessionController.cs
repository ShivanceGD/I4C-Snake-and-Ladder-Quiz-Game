using System;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

public class CustomSessionController : MonoBehaviour
{
    public static CustomSessionController Instance { get; private set; }

    public int MaxPlayers = 4;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public async Task CreatePrivateSessionAsync(string sessionName)
    {
        var multiplayer = Unity.Services.Multiplayer.MultiplayerService.Instance;
        if (multiplayer == null)
        {
            Debug.LogError("[CustomSessionController] MultiplayerService.Instance is null. Ensure services are initialized.");
            return;
        }

        var clampUI = ClampUIWithinSafeArea.Instance;
        if (clampUI != null)
        {
            clampUI.SetPrivateRoom(true);
        }

        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.IsPrivateRoom = true;
        }

        var sessionOptions = new SessionOptions
        {
            Name = sessionName,
            MaxPlayers = MaxPlayers,
            IsPrivate = true,
            IsLocked = false,
        };

        sessionOptions.WithRelayNetwork();

        try
        {
            var session = await multiplayer.CreateSessionAsync(sessionOptions);

            if (clampUI != null)
            {
                clampUI.OnJoinedSession(session);
            }

            Debug.Log($"[CustomSessionController] Private session '{sessionName}' created. Code: {session.Code}");
        }
        catch (SessionException e)
        {
            Debug.LogError($"[CustomSessionController] Failed to create private session: {e.Message}");
        }
    }

    public async Task QuickJoinAsync()
    {
        var multiplayer = Unity.Services.Multiplayer.MultiplayerService.Instance;
        if (multiplayer == null)
        {
            Debug.LogError("[CustomSessionController] MultiplayerService.Instance is null. Ensure services are initialized.");
            return;
        }

        var clampUI = ClampUIWithinSafeArea.Instance;
        if (clampUI != null)
        {
            clampUI.SetPrivateRoom(false);
        }

        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.IsPrivateRoom = false;
        }

        var quickJoinOptions = new QuickJoinOptions
        {
            CreateSession = true
        };

        var sessionOptions = new SessionOptions
        {
            Name = "QuickMatch",
            MaxPlayers = MaxPlayers,
            IsPrivate = false,
            IsLocked = false,
        };

        sessionOptions.WithRelayNetwork();

        try
        {
            var session = await multiplayer.MatchmakeSessionAsync(quickJoinOptions, sessionOptions);

            if (clampUI != null)
            {
                clampUI.OnJoinedSession(session);
            }

            Debug.Log("[CustomSessionController] Joined public session via Quick Join.");
        }
        catch (SessionException e)
        {
            Debug.LogError($"[CustomSessionController] Quick Join failed: {e.Message}");
        }
    }
}
