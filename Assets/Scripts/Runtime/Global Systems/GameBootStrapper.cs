using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class GameBootStrapper : MonoBehaviour
{
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
        try
        {
            await CloneEverything();
            cts = new CancellationTokenSource();
            _ = InitializeGameAsync(cts.Token);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async Task CloneEverything()
    {
        LoadingSceneManager loadedSceneManager = Instantiate(LoadingSceneManagerPrefab);
        SoundManager loadedSoundManager = Instantiate(SoundManagerPrefab);
        RemoteConfigLoadManager RemoteConfigLoadManager = Instantiate(RemoteConfigManagerPrefab);
        SaveAndLoadManager SaveAndLoadManager = Instantiate(SaveAndLoadManagerPrefab);
        Analytics_Manager analyticsManager = Instantiate(AnalyticsManagerPrefab);
        
        await Task.Yield();
        
        if (!string.IsNullOrEmpty(BootStrapperSceneName))
        {
            var unloadOp = SceneManager.UnloadSceneAsync(BootStrapperSceneName);
            if (unloadOp != null)
            {
                while (!unloadOp.isDone) await Task.Yield();
            }
        }
        // Load the first scene
        loadedSceneManager.LoadofflineScene(FirstSceneToLoad);
    }


    private async Task InitializeGameAsync(CancellationToken token)
    {
        try
        {
            await UnityServices.InitializeAsync();
            Analytics_Manager.Instance.StartCollection();
            RemoteConfigService.Instance.FetchConfigs(new userAttribute(), new appAttribute());
            if (token.IsCancellationRequested) return;
            
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