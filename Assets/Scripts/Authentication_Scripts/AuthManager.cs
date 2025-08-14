using UnityEngine;
using Unity.Services.Core;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

public class AuthManager : MonoBehaviour
{
    [Header("Pannels")]
    //public GameObject AuthenticationPannel;
    public GameObject SuccessFailPannel;
    public TMP_Text SuccessFail_text;

    [Header("Profile")]
    public TMP_Text UserName;
    public TMP_Text UID;

    [Header("Username Password")]
    public TMP_InputField Username;
    public TMP_InputField Password;
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

    [ContextMenu("LINK")]
    public async void LinkProfileToIDP()
    {
        await AuthExtensions.LinkUsernamePasswordAsync(Username.text,Password.text);
    }

    public void GetUserName()
    {
        Username.text = AuthExtensions.GetCachedPlayerName();
        UID.text = AuthExtensions.GetPlayerID();
    }
    public async void SignInButton()
    {
      await  AuthExtensions.SignInWithUsernamePasswordAsync(Username.text, Password.text);
    }
    public void UpdateText(string txt)
    {
        SuccessFail_text.text = txt;
    }
    public void SignoutButton()
    {
        AuthExtensions.SignOut();
    }
}
