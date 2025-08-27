using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using UnityEngine.Events;

public static class AuthExtensions
{

    /*#region Auth Events Handling
    public static void RegisterEvents(UnityEvent onSignedIn, UnityEvent onExpired, UnityEvent onSignedOut)
    {
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log($"✅ Signed in. PlayerID: {AuthenticationService.Instance.PlayerId}");
            onSignedIn?.Invoke();
        };

        AuthenticationService.Instance.Expired += () =>
        {
            Debug.Log("⚠️ Access token expired.");
            onExpired?.Invoke();
        };
        AuthenticationService.Instance.SignInFailed += (e) =>
        {
            Debug.Log("⚠️" + e);
            onSignedOut?.Invoke();
        };

        AuthenticationService.Instance.SignedOut += () =>
        {
            Debug.Log("🚪 Signed out.");
            onSignedOut?.Invoke();
        };
    }*/
    #region Auth Events Handling
    public static void RegisterEvents(UnityAction onSignedIn, UnityAction onExpired, UnityAction onSignedOut, UnityAction<string> onSignInFailed)
    {
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log($"✅ Signed in. PlayerID: {AuthenticationService.Instance.PlayerId}");
            onSignedIn?.Invoke();
        };

        AuthenticationService.Instance.Expired += () =>
        {
            Debug.Log("⚠️ Access token expired.");
            onExpired?.Invoke();
        };

        AuthenticationService.Instance.SignedOut += () =>
        {
            Debug.Log("🚪 Signed out.");
            onSignedOut?.Invoke();
        };

        AuthenticationService.Instance.SignInFailed += (exception) =>
        {
            string reason = exception?.Message ?? "Unknown error";
            Debug.LogError($"❌ Sign-in failed: {reason}");
            onSignInFailed?.Invoke(reason);
        };
    }
    #endregion

    #region Sign-Up / Sign-In
    public static async Task<string> SignUpWithUsernamePasswordAsync(string username, string password)
    {
        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
            Debug.Log("✅ Sign-up successful.");
            return "Success: Account created!";
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"Sign-up failed: {ex.Message}");
            return $"Sign-up failed: {ex.Message}";
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"Sign-up failed: {ex.Message}");
            return $"Sign-up failed: {ex.Message}";
        }
    }

    public static async Task<string> SignInWithUsernamePasswordAsync(string username, string password)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            Debug.Log("✅ Sign-in successful.");
            return "Success: Signed in!";
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"Sign-in failed: {ex.Message}");
            return $"Sign-in failed: {ex.Message}";
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"Sign-in failed: {ex.Message}");
            return $"Sign-in failed: {ex.Message}";
        }
    }
    #endregion

    public static void CheckStates()
    {
        Debug.Log($"IsSignedIn: {AuthenticationService.Instance.IsSignedIn}");
        Debug.Log($"IsAuthorized: {AuthenticationService.Instance.IsAuthorized}");
        Debug.Log($"IsExpired: {AuthenticationService.Instance.IsExpired}");
        Debug.Log($"SessionTokenExists: {AuthenticationService.Instance.SessionTokenExists}");
        Debug.Log($"PlayerId: {AuthenticationService.Instance.PlayerId}");
        Debug.Log($"PlayerName (cached): {AuthenticationService.Instance.PlayerName}");
    }
    //#endregion

    #region Update Player Name and Password
    public static async Task UpdatePlayerNameAsync(string playerName)
    {
        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
            Debug.Log($"Player name updated to '{playerName}'");
        }
        catch (AuthenticationException ex) { Debug.LogException(ex); }
        catch (RequestFailedException ex) { Debug.LogException(ex); }
    }

    public static async Task<string> GetPlayerNameAsync()
    {
        try
        {
            return await AuthenticationService.Instance.GetPlayerNameAsync();
        }
        catch (AuthenticationException ex) { Debug.LogException(ex); }
        catch (RequestFailedException ex) { Debug.LogException(ex); }
        return null;
    }

    public static string GetCachedPlayerName() => AuthenticationService.Instance.PlayerName;

    public static string GetPlayerID() => AuthenticationService.Instance.PlayerId;

    public static async Task UpdatePasswordAsync(string currentPassword, string newPassword)
    {
        try
        {
            await AuthenticationService.Instance.UpdatePasswordAsync(currentPassword, newPassword);
            Debug.Log("Password updated.");
        }
        catch (AuthenticationException ex) { Debug.LogException(ex); }
        catch (RequestFailedException ex) { Debug.LogException(ex); }
    }
    #endregion

    #region Sign-In Methods
    public static async Task SignInCachedOrAnonymousAsync()
    {
        if (AuthenticationService.Instance.SessionTokenExists)
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Signed in with cached session.");
                return;
            }
            catch
            {
                Debug.Log("Cached session invalid, signing in anonymously...");
            }
        }
        await SignInAnonymouslyAsync();
    }

    public static async Task SignInAnonymouslyAsync()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Signed in anonymously.");
        }
        catch (AuthenticationException ex) { Debug.LogException(ex); }
        catch (RequestFailedException ex) { Debug.LogException(ex); }
    }

    /*public static async Task SignInWithUsernamePasswordAsync(string username, string password)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            Debug.Log("Signed in with username/password.");
        }
        catch (AuthenticationException ex) { Debug.LogException(ex); }
        catch (RequestFailedException ex) { Debug.LogException(ex); }
    }

    public static async Task SignUpWithUsernamePasswordAsync(string username, string password)
    {
        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
            Debug.Log("Signed up with username/password.");
        }
        catch (AuthenticationException ex) { Debug.LogException(ex); }
        catch (RequestFailedException ex) { Debug.LogException(ex); }
    }

    public static async Task LinkUsernamePasswordAsync(string username, string password)
    {
        try
        {
            await AuthenticationService.Instance.AddUsernamePasswordAsync(username, password);
            Debug.Log("Username/password linked to account.");
        }
        catch (AuthenticationException ex) { Debug.LogException(ex); }
        catch (RequestFailedException ex) { Debug.LogException(ex); }
    }*/
    #endregion

    #region Signout and Session Handling
    public static void SignOut(bool clearSession = false)
    {
        AuthenticationService.Instance.SignOut(clearSession);
        Debug.Log(clearSession ? "Signed out & session cleared." : "Signed out (session kept).");
    }

    public static void ClearSessionToken()
    {
        AuthenticationService.Instance.ClearSessionToken();
        Debug.Log("Session token cleared.");
    }
    #endregion
}
