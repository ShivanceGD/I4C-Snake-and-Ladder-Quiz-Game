using System;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Unity.Services.Authentication;
using UnityEngine.UI;
using Unity.Services.Core;

public class AuthManager : MonoBehaviour
{
    [Header("Scenes to load after success")]
    public string OfflineSceneName;
    
    [Header("Panels")]
    public GameObject SuccessFailPanel;
    public TMP_Text SuccessFailText;

    [Header("Profile")]
    public TMP_Text UserName;
    public TMP_Text UID;

    [Header("SignIn References")]
    public TMP_InputField Username_SignIn;
    public TMP_InputField Password_SignIn;
    public Button UsernameSignInButton;
    [Header("SignUp References")]
    public TMP_InputField Username_SignUp;
    public TMP_InputField Password_SignUp;
    public Button UsernameSignUpButton;
    public Button GuestButton;
    public Button GooglePlaySignInButton;

    [Header("Logout")]
   // public Button Logout;

    [Header("Events")]
    public UnityEvent OnSignedIn;
    public UnityEvent OnExpired;
    public UnityEvent OnSignedOut;
    public UnityEvent<string> OnAuthMessage;

    private async void Start()
    {
        UnityServices.InitializeAsync();

#if Unity_Editor || UNITY_STANDALONE_WIN || UNITY_ANDROID
        {
        GooglePlaySignInButton.gameObject.SetActive(false);
        }
#endif
        UsernameSignUpButton.onClick.AddListener(SignUpProfile);
        UsernameSignInButton.onClick.AddListener(SignInButton);
        GuestButton.onClick.AddListener(GuestSignIn);

        // Register auth events
        AuthExtensions.RegisterEvents(
            onSignedIn: () => ShowMessage("Signed in successfully!", Color.green),
            onExpired: () => ShowMessage("Session expired. Please sign in again.", Color.yellow),
            onSignedOut: () => ShowMessage("Signed out.", Color.blue),
            onSignInFailed: (msg) => ShowMessage($"Sign-in failed: {msg}", Color.red)
        );
        if (AuthenticationService.Instance.SessionTokenExists)
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                LoadingSceneManager.Instance.LoadofflineScene(OfflineSceneName);
                Debug.Log("Signed in with cached session.");
                return;
            }
            catch
            {
                Debug.Log("Cached session invalid, signing in anonymously...");
            }
        }
    }

    public void LoadScene()
    {
        if (LoadingSceneManager.Instance != null)
            LoadingSceneManager.Instance.LoadofflineScene(OfflineSceneName);
        else
            Debug.LogError("LoadingSceneManager instance not found!");
    }
    public async void SignUpProfile()
    {
        if (string.IsNullOrEmpty(Username_SignUp.text) || string.IsNullOrEmpty(Password_SignUp.text))
        {
            ShowMessage("Username and Password cannot be empty!", Color.red);
            return;
        }

        string result = await AuthExtensions.SignUpWithUsernamePasswordAsync(Username_SignUp.text, Password_SignUp.text);
        //ShowMessage(result, Color.red);
        //UpdateProfileUI();*/
        if (result.StartsWith("Success", StringComparison.OrdinalIgnoreCase))
        {
            ShowMessage("Account created successfully!", Color.green);
            new WaitForSeconds(2f); // small delay for UI feedback (optional)
            LoadScene();
        }
        else
        {
            ShowMessage(result, Color.red);
        }
    }

    public async void GuestSignIn()
    {
        try
        {
            await AuthExtensions.SignInAnonymouslyAsync();
            LoadingSceneManager.Instance.LoadofflineScene(OfflineSceneName);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
    }
    public async void SignInButton()
    {
        if (string.IsNullOrEmpty(Username_SignIn.text) || string.IsNullOrEmpty(Password_SignIn.text))
        {
            ShowMessage("Username and Password cannot be empty!", Color.red);
            return;
        }

        string result = await AuthExtensions.SignInWithUsernamePasswordAsync(Username_SignIn.text, Password_SignIn.text);
        if (result.StartsWith("Success"))
        {
            ShowMessage("Signed in successfully!", Color.green);
             new WaitForSeconds(2f);
            LoadScene();
        }
        
        else
            ShowMessage(result, Color.red);

        //UpdateProfileUI();
    }

    public void SignoutButton()
    {
        AuthExtensions.SignOut(true);
        //ShowMessage("Signed out successfully.", Color.yellow);
        //UpdateProfileUI();
    }

    private void UpdateProfileUI()
    {
        UserName.text = AuthExtensions.GetCachedPlayerName() ?? "Not Signed In";
        UID.text = AuthExtensions.GetPlayerID() ?? "N/A";
    }

    public void ShowMessage(string msg, Color color)
    {
        SuccessFailPanel.SetActive(true);
        SuccessFailText.color = color;
        SuccessFailText.text = msg;
    }
}

