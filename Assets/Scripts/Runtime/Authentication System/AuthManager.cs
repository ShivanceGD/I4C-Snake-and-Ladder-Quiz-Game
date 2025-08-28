/*
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
    public UnityEvent OnSignedIn, onExpired, onSignedOut,OnSignInFailed;

    public void RegisterAuthEvents()
    {
        
        AuthExtensions.RegisterEvents(OnSignedIn, onExpired, onSignedOut,OnSignInFailed);
    }

    [ContextMenu("LINK")]
    public async void SignUpProfile()
    {
        await AuthExtensions.SignUpWithUsernamePasswordAsync(Username.text,Password.text);
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
*/
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using Unity.Services.Core;

public class AuthManager : MonoBehaviour
{
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
    public UnityEvent<string> OnAuthMessage; // carries messages

    private void Start()
    {
        UnityServices.InitializeAsync();
        // Subscribe UI buttons
#if Unity_Editor || UNITY_STANDALONE_WIN
        {
        GooglePlaySignInButton.gameObject.SetActive(false);
        }
#endif
        UsernameSignUpButton.onClick.AddListener(SignUpProfile);
        UsernameSignInButton.onClick.AddListener(SignInButton);
        GuestButton.onClick.AddListener(GuestSignIn);
        //Logout.onClick.AddListener(SignoutButton);

        // Register auth events
        AuthExtensions.RegisterEvents(
            onSignedIn: () => ShowMessage("Signed in successfully!", Color.green),
            onExpired: () => ShowMessage("Session expired. Please sign in again.", Color.yellow),
            onSignedOut: () => ShowMessage("Signed out.", Color.blue),
            onSignInFailed: (msg) => ShowMessage($"Sign-in failed: {msg}", Color.red)
        );
    }

    public async void SignUpProfile()
    {
        if (string.IsNullOrEmpty(Username_SignUp.text) || string.IsNullOrEmpty(Password_SignUp.text))
        {
            ShowMessage("Username and Password cannot be empty!", Color.red);
            return;
        }

        string result = await AuthExtensions.SignUpWithUsernamePasswordAsync(Username_SignUp.text, Password_SignUp.text);
        ShowMessage(result, Color.green);
        UpdateProfileUI();
    }

    public async void GuestSignIn()
    {
        await AuthExtensions.SignInAnonymouslyAsync();
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
            ShowMessage("Signed in successfully!", Color.green);
        else
            ShowMessage(result, Color.red);

        UpdateProfileUI();
    }

    public void SignoutButton()
    {
        AuthExtensions.SignOut();
        ShowMessage("Signed out successfully.", Color.yellow);
        UpdateProfileUI();
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

