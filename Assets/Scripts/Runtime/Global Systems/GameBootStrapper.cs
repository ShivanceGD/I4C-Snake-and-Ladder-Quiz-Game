using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class GameBootStrapper : MonoBehaviour
{
    public static GameBootStrapper Instance { get; private set; }

    [SerializeField] private string FirstSceneToLoad;
    [SerializeField] private string BootStrapperSceneName;
    
    [SerializeField]private SoundManager SoundManagerPrefab;
    [SerializeField]private RemoteConfigLoadManager RemoteConfigManagerPrefab;
    [SerializeField]private LoadingSceneManager LoadingSceneManagerPrefab;
    [SerializeField] private SaveAndLoadManager SaveAndLoadManagerPrefab;
    [SerializeField] private Analytics_Manager AnalyticsManagerPrefab;
    public bool IsBootStrapped { get; private set; }

    private CancellationTokenSource cts;

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        try
        {
            LoadingSceneManager loadingSceneManager = await CloneEverything();
            cts = new CancellationTokenSource();
            await InitializeGameAsync(cts.Token);

            if (loadingSceneManager != null)
            {
                loadingSceneManager.LoadofflineScene(FirstSceneToLoad);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async Task<LoadingSceneManager> CloneEverything()
    {
        LoadingSceneManager loadedSceneManager = Instantiate(LoadingSceneManagerPrefab);
        SoundManager loadedSoundManager = Instantiate(SoundManagerPrefab);
        RemoteConfigLoadManager RemoteConfigLoadManager = Instantiate(RemoteConfigManagerPrefab);
        SaveAndLoadManager SaveAndLoadManager = Instantiate(SaveAndLoadManagerPrefab);
        Analytics_Manager analyticsManager = Instantiate(AnalyticsManagerPrefab);
        
        await Task.Yield();
        return loadedSceneManager;
    }


    private async Task InitializeGameAsync(CancellationToken token)
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (token.IsCancellationRequested) return;

            try
            {
                if (Analytics_Manager.Instance != null)
                {
                    Analytics_Manager.Instance.StartCollection();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BootStrapper] Analytics startup failed: {ex.Message}");
            }

            try
            {
                RemoteConfigService.Instance.FetchConfigs(new userAttribute(), new appAttribute());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BootStrapper] Remote Config startup failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BootStrapper] Initialization failed: {ex}");
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                IsBootStrapped = true;
                Debug.Log("[BootStrapper] Initialization complete.");
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        cts?.Cancel();
        cts?.Dispose();
    }

    private struct userAttribute { }
    private struct appAttribute { }
}
