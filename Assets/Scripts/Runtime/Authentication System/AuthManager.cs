using System;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AuthManager : MonoBehaviour
{
    [Header("Scenes to load after success")]
    public string OfflineSceneName;

    [Header("Panels")]
    public GameObject SuccessFailPanel;
    public TMP_Text SuccessFailText;
    public GameObject SignUpPanel;
    public GameObject SignInPanel;

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

    [Header("Events")]
    public UnityEvent OnSignedIn;
    public UnityEvent OnExpired;
    public UnityEvent OnSignedOut;
    public UnityEvent<string> OnAuthMessage;

    [Header("Show Password - Sign In")]
    public Toggle ShowPasswordSignInToggle;
    public TMP_InputField ShowPasswordSignInText;

    [Header("Show Password - Sign Up")]
    public Toggle ShowPasswordSignUpToggle;
    public TMP_InputField ShowPasswordSignUpText;

    private static bool eventsRegistered;
    private CancellationTokenSource cts;
    private bool isSigningIn;

    private async void Start()
    {
        cts = new CancellationTokenSource();

        try
        {
            await WaitForBootstrapAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!eventsRegistered)
        {
            eventsRegistered = true;
            AuthExtensions.RegisterEvents(
                onSignedIn: () =>
                {
                    HideAuthPanels();
                    UpdateProfileUI();
                },
                onExpired: () => ShowMessage("Session expired. Please sign in again.", Color.yellow),
                onSignedOut: () => ShowMessage("Signed out.", Color.blue),
                onSignInFailed: msg => ShowMessage($"Sign-in failed: {msg}", Color.red)
            );
        }

        DisableManualAuthControls();

        if (AuthenticationService.Instance.IsSignedIn)
        {
            HideAuthPanels();
            UpdateProfileUI();
            LoadScene();
            return;
        }

        bool restored = await AuthExtensions.TryRestoreGooglePlayGamesSignInAsync(cts.Token);
        if (restored)
        {
            HideAuthPanels();
            UpdateProfileUI();
            LoadScene();
            return;
        }

        if (GooglePlaySignInButton != null)
        {
            GooglePlaySignInButton.onClick.RemoveAllListeners();
            GooglePlaySignInButton.onClick.AddListener(() => _ = SignInWithGooglePlayGamesAsync());
        }
    }

    public void LoadScene()
    {
        if (string.IsNullOrWhiteSpace(OfflineSceneName)) return;
        if (SceneManager.GetActiveScene().name == OfflineSceneName) return;

        if (LoadingSceneManager.Instance != null)
            LoadingSceneManager.Instance.LoadofflineScene(OfflineSceneName);
        else
            Debug.LogError("LoadingSceneManager instance not found!");
    }

    public void SignoutButton()
    {
        AuthExtensions.SignOut(true);
    }

    public void ShowMessage(string msg, Color color)
    {
        if (SuccessFailPanel != null) SuccessFailPanel.SetActive(true);

        if (SuccessFailText != null)
        {
            SuccessFailText.color = color;
            SuccessFailText.text = msg;
        }

        OnAuthMessage?.Invoke(msg);
    }

    public void ToggleSignInPassword(bool isOn)
    {
        SetPasswordVisibility(Password_SignIn, isOn);
    }

    public void ToggleSignUpPassword(bool isOn)
    {
        SetPasswordVisibility(Password_SignUp, isOn);
    }

    public void SignInButton()
    {
        _ = SignInWithGooglePlayGamesAsync();
    }

    public void SignUpProfile()
    {
        _ = SignInWithGooglePlayGamesAsync();
    }

    private async Task SignInWithGooglePlayGamesAsync()
    {
        if (isSigningIn || cts == null || cts.IsCancellationRequested) return;

        try
        {
            isSigningIn = true;
            SetGoogleButtonInteractable(false);
            ShowMessage("Signing in with Google Play Games...", Color.white);

            string result = await AuthExtensions.SignInWithGooglePlayGamesAsync(cts.Token);
            bool success = result.StartsWith("Success", StringComparison.OrdinalIgnoreCase);

            if (success)
            {
                UpdateProfileUI();
                HideAuthPanels();
                await Task.Delay(500, cts.Token);
                LoadScene();
            }
            else
            {
                ShowMessage(result, Color.red);
                SetGoogleButtonInteractable(true);
            }
        }
        catch (OperationCanceledException)
        {
            ShowMessage("Google Play Games sign-in cancelled.", Color.yellow);
        }
        finally
        {
            isSigningIn = false;
        }
    }

    private async Task WaitForBootstrapAsync(CancellationToken token)
    {
        int attempts = 0;
        while (GameBootStrapper.Instance != null && !GameBootStrapper.Instance.IsBootStrapped && attempts < 100)
        {
            token.ThrowIfCancellationRequested();
            await Task.Yield();
            attempts++;
        }
    }

    private void DisableManualAuthControls()
    {
        DisableInput(Username_SignIn);
        DisableInput(Password_SignIn);
        DisableInput(Username_SignUp);
        DisableInput(Password_SignUp);
        DisableInput(ShowPasswordSignInText);
        DisableInput(ShowPasswordSignUpText);

        DisableButton(UsernameSignInButton);
        DisableButton(UsernameSignUpButton);
        DisableButton(GuestButton);

        DisableToggle(ShowPasswordSignInToggle);
        DisableToggle(ShowPasswordSignUpToggle);
    }

    private void HideAuthPanels()
    {
        if (SuccessFailPanel != null) SuccessFailPanel.SetActive(false);
        if (SignUpPanel != null) SignUpPanel.SetActive(false);
        if (SignInPanel != null) SignInPanel.SetActive(false);

        HideParentPanel(Username_SignUp);
        HideParentPanel(Password_SignUp);
        HideParentPanel(Username_SignIn);
        HideParentPanel(Password_SignIn);
    }

    private void UpdateProfileUI()
    {
        if (UserName != null) UserName.text = AuthExtensions.GetCachedPlayerName();
        if (UID != null) UID.text = AuthExtensions.GetPlayerID();
    }

    private void SetGoogleButtonInteractable(bool interactable)
    {
        if (GooglePlaySignInButton != null) GooglePlaySignInButton.interactable = interactable;
    }

    private static void DisableInput(TMP_InputField input)
    {
        if (input == null) return;
        input.interactable = false;
        input.gameObject.SetActive(false);
    }

    private static void HideParentPanel(TMP_InputField input)
    {
        if (input == null) return;
        Transform parent = input.transform.parent;
        if (parent != null) parent.gameObject.SetActive(false);
    }

    private static void DisableButton(Button button)
    {
        if (button == null) return;
        button.interactable = false;
        button.gameObject.SetActive(false);
    }

    private static void DisableToggle(Toggle toggle)
    {
        if (toggle == null) return;
        toggle.interactable = false;
        toggle.gameObject.SetActive(false);
    }

    private static void SetPasswordVisibility(TMP_InputField input, bool visible)
    {
        if (input == null) return;
        input.contentType = visible ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
        input.ForceLabelUpdate();
    }

    private void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }
}
