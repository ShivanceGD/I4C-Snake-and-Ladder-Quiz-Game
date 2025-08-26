using System;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GameBootStrapper :MonoBehaviour
{
    public AuthManager AuthManager;
    public GameManager GameManager;
    public GameObject LoadingScreen;
    public SoundManager SoundManager;
    public RemoteConfigLoadManager RemoteConfigManager;
    
    public struct userAttribute{}
    public struct appAttribute{}
    private async void Awake()
    {
        
        await UnityServices.InitializeAsync();
        if (Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn) return;
        await AuthExtensions.SignInCachedOrAnonymousAsync();
        
        RemoteConfigService.Instance.FetchConfigs(new userAttribute{}, new appAttribute{});
    }
}