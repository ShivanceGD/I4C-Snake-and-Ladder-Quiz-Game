using UnityEngine;
using Unity.Services.Core;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

public class AuthManager : MonoBehaviour
{
    [Header("Pannels")]
    public GameObject AuthenticationPannel;
    public GameObject SuccessFailPannel;

    [Header("Profile")]
    public TMP_Text UserName;
    public TMP_Text UID;

    [Header("Username Password")]
    public TMP_Text Username;
    public TMP_Text Password;
    public Button UsernameSignUpButton, UsernameSignInButton;

    [Header("Logout")]
    public Button Logout;

    [Header("Events")]
    public UnityEvent OnSignedIn, onExpired, onSignedOut;

    private async void Awake()
    {
        await UnityServices.InitializeAsync();
        AuthExtensions.RegisterEvents(OnSignedIn, onExpired, onSignedOut);

        if (Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn) return;
        await AuthExtensions.SignInCachedOrAnonymousAsync();
    }

    [ContextMenu("lINK")]
    public async void LinkProfileToIDP()
    {
        await AuthExtensions.LinkUsernamePasswordAsync("Akash12", "Akash@12");
    }

    public void GetUserName()
    {
        Username.text = AuthExtensions.GetCachedPlayerName();
        UID.text = AuthExtensions.GetPlayerID();
    }
}
