using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class GameBootStrapper : MonoBehaviour
{
    public UIManager UiManager;
    //public AuthManager AuthManagerPrefab;
    //public GameManager GameManagerPrefab;
    public SoundManager SoundManagerPrefab;
    public RemoteConfigLoadManager RemoteConfigManagerPrefab;

    public bool IsBootStrapped { get; private set; }

    private CancellationTokenSource cts;

    private void Awake()
    {
        cts = new CancellationTokenSource();
        _ = InitializeGameAsync(cts.Token);
    }

    private async Task InitializeGameAsync(CancellationToken token)
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn)
            {
                await AuthExtensions.SignInCachedOrAnonymousAsync();
            }

            RemoteConfigService.Instance.FetchConfigs(new userAttribute(), new appAttribute());

            if (token.IsCancellationRequested) return;
        
            //Instantiate(UiManager);
            //Show Loading Screen
            /*Instantiate(AuthManagerPrefab);
            Instantiate(GameManagerPrefab);
            Instantiate(SoundManagerPrefab);
            Instantiate(RemoteConfigManagerPrefab);*/

            IsBootStrapped = true;
            Debug.Log("[BootStrapper] Initialization complete.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BootStrapper] Initialization failed: {ex}");
        }
    }

    private void OnDestroy()
    {
        cts.Cancel();
        cts.Dispose();
    }

    private struct userAttribute { }
    private struct appAttribute { }
}