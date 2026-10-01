using System;
using System.Threading;
using System.Threading.Tasks;
#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Events;

public static class AuthExtensions
{
    private const string DefaultPlayerName = "Player";
    private const int GooglePlayGamesTimeoutSeconds = 20;
    private static bool playGamesActivated;
    private static string cachedGooglePlayGamesName;

    public static void RegisterEvents(UnityAction onSignedIn, UnityAction onExpired, UnityAction onSignedOut, UnityAction<string> onSignInFailed)
    {
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log($"Signed in. PlayerID: {AuthenticationService.Instance.PlayerId}");
            onSignedIn?.Invoke();
        };

        AuthenticationService.Instance.Expired += () =>
        {
            Debug.Log("Access token expired.");
            onExpired?.Invoke();
        };

        AuthenticationService.Instance.SignedOut += () =>
        {
            Debug.Log("Signed out.");
            onSignedOut?.Invoke();
        };

        AuthenticationService.Instance.SignInFailed += exception =>
        {
            string reason = exception?.Message ?? "Unknown error";
            Debug.LogError($"Sign-in failed: {reason}");
            onSignInFailed?.Invoke(reason);
        };
    }

    public static async Task<string> SignInWithGooglePlayGamesAsync(CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();

            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                return "Unity Services are still starting. Please try again.";
            }

            EnsurePlayGamesActivated();

            bool authenticated = await AuthenticateGooglePlayGamesAsync(token);
            if (!authenticated)
            {
                return "Google Play Games sign-in failed. Please try again.";
            }

            token.ThrowIfCancellationRequested();
            string authCode = await RequestServerAuthCodeAsync(token);
            if (string.IsNullOrEmpty(authCode))
            {
                return "Google Play Games server auth code was empty. Check Play Games setup.";
            }

            token.ThrowIfCancellationRequested();
            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode);

            cachedGooglePlayGamesName = GetGooglePlayGamesDisplayName();
            string resolvedName = ResolvePlayerName(cachedGooglePlayGamesName);
            await UpdatePlayerNameAsync(resolvedName);

            Debug.Log($"Google Play Games sign-in complete. PlayerID: {AuthenticationService.Instance.PlayerId}");
            return "Success: Signed in with Google Play Games!";
        }
        catch (OperationCanceledException)
        {
            return "Google Play Games sign-in cancelled.";
        }
        catch (AuthenticationException ex)
        {
            Debug.LogException(ex);
            return $"Authentication failed: {ex.Message}";
        }
        catch (RequestFailedException ex)
        {
            Debug.LogException(ex);
            return $"Authentication request failed: {ex.Message}";
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return $"Google Play Games sign-in failed: {ex.Message}";
        }
    }

    public static async Task<bool> TryRestoreGooglePlayGamesSignInAsync(CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();

            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                return false;
            }

            if (AuthenticationService.Instance.IsSignedIn)
            {
                return true;
            }

            if (AuthenticationService.Instance.SessionTokenExists)
            {
                try
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Cached Unity Authentication session restore failed: {ex.Message}");
                }
            }

            EnsurePlayGamesActivated();

            bool authenticated = await AuthenticateGooglePlayGamesSilentlyAsync(token);
            if (!authenticated)
            {
                return false;
            }

            token.ThrowIfCancellationRequested();
            string authCode = await RequestServerAuthCodeAsync(token);
            if (string.IsNullOrEmpty(authCode))
            {
                return false;
            }

            token.ThrowIfCancellationRequested();
            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode);

            cachedGooglePlayGamesName = GetGooglePlayGamesDisplayName();
            string resolvedName = ResolvePlayerName(cachedGooglePlayGamesName);
            await UpdatePlayerNameAsync(resolvedName);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Google Play Games restore failed: {ex.Message}");
            return false;
        }
    }

    public static void CheckStates()
    {
        Debug.Log($"IsSignedIn: {AuthenticationService.Instance.IsSignedIn}");
        Debug.Log($"IsAuthorized: {AuthenticationService.Instance.IsAuthorized}");
        Debug.Log($"IsExpired: {AuthenticationService.Instance.IsExpired}");
        Debug.Log($"SessionTokenExists: {AuthenticationService.Instance.SessionTokenExists}");
        Debug.Log($"PlayerId: {AuthenticationService.Instance.PlayerId}");
        Debug.Log($"PlayerName (cached): {AuthenticationService.Instance.PlayerName}");
    }

    public static async Task UpdatePlayerNameAsync(string playerName)
    {
        string resolvedName = ResolvePlayerName(playerName);

        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(resolvedName);
            cachedGooglePlayGamesName = resolvedName;
            Debug.Log($"Player name updated to '{resolvedName}'");
        }
        catch (AuthenticationException ex) { Debug.LogException(ex); }
        catch (RequestFailedException ex) { Debug.LogException(ex); }
    }

    public static async Task<string> GetPlayerNameAsync()
    {
        try
        {
            string playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
            return ResolvePlayerName(playerName);
        }
        catch (AuthenticationException ex) { Debug.LogException(ex); }
        catch (RequestFailedException ex) { Debug.LogException(ex); }

        return GetCachedPlayerName();
    }

    public static string GetCachedPlayerName()
    {
        string playerName = AuthenticationService.Instance.PlayerName;
        if (!string.IsNullOrWhiteSpace(playerName)) return playerName;
        if (!string.IsNullOrWhiteSpace(cachedGooglePlayGamesName)) return cachedGooglePlayGamesName;

        cachedGooglePlayGamesName = GetGooglePlayGamesDisplayName();
        return ResolvePlayerName(cachedGooglePlayGamesName);
    }

    public static string GetPlayerID()
    {
        return string.IsNullOrEmpty(AuthenticationService.Instance.PlayerId)
            ? "N/A"
            : AuthenticationService.Instance.PlayerId;
    }

    public static void SignOut(bool clearSession = false)
    {
        AuthenticationService.Instance.SignOut(clearSession);
        Debug.Log(clearSession ? "Signed out and session cleared." : "Signed out.");
    }

    public static void ClearSessionToken()
    {
        AuthenticationService.Instance.ClearSessionToken();
        Debug.Log("Session token cleared.");
    }

    private static void EnsurePlayGamesActivated()
    {
#if UNITY_ANDROID
        if (playGamesActivated) return;
        PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Activate();
        playGamesActivated = true;
#else
        throw new InvalidOperationException("Google Play Games sign-in requires Android build target.");
#endif
    }

    private static async Task<bool> AuthenticateGooglePlayGamesAsync(CancellationToken token)
    {
#if UNITY_ANDROID
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        using (token.Register(() => tcs.TrySetCanceled()))
        {
            PlayGamesPlatform.Instance.ManuallyAuthenticate(status =>
            {
                Debug.Log($"Google Play Games manual sign-in finished with status: {status}");
                tcs.TrySetResult(status == SignInStatus.Success);
            });

            return await WithTimeoutAsync(tcs.Task, GooglePlayGamesTimeoutSeconds, false, token);
        }
#else
        throw new InvalidOperationException("Google Play Games sign-in requires Android build target.");
#endif
    }

    private static async Task<bool> AuthenticateGooglePlayGamesSilentlyAsync(CancellationToken token)
    {
#if UNITY_ANDROID
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        using (token.Register(() => tcs.TrySetCanceled()))
        {
            PlayGamesPlatform.Instance.Authenticate(status =>
            {
                Debug.Log($"Google Play Games silent sign-in finished with status: {status}");
                tcs.TrySetResult(status == SignInStatus.Success);
            });

            return await WithTimeoutAsync(tcs.Task, GooglePlayGamesTimeoutSeconds, false, token);
        }
#else
        return false;
#endif
    }

    private static async Task<string> RequestServerAuthCodeAsync(CancellationToken token)
    {
#if UNITY_ANDROID
        TaskCompletionSource<string> tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        using (token.Register(() => tcs.TrySetCanceled()))
        {
            PlayGamesPlatform.Instance.RequestServerSideAccess(false, authCode =>
            {
                tcs.TrySetResult(authCode);
            });

            return await WithTimeoutAsync(tcs.Task, GooglePlayGamesTimeoutSeconds, string.Empty, token);
        }
#else
        throw new InvalidOperationException("Google Play Games server auth code requires Android build target.");
#endif
    }

    private static string GetGooglePlayGamesDisplayName()
    {
#if UNITY_ANDROID
        try
        {
            return PlayGamesPlatform.Instance.GetUserDisplayName();
        }
        catch
        {
            return string.Empty;
        }
#else
        return string.Empty;
#endif
    }

    private static string ResolvePlayerName(string preferredName)
    {
        if (!string.IsNullOrWhiteSpace(preferredName)) return preferredName.Trim();
        string playerId = AuthenticationService.Instance.PlayerId;

        if (!string.IsNullOrEmpty(playerId))
        {
            int suffixLength = Mathf.Min(6, playerId.Length);
            return $"{DefaultPlayerName}{playerId.Substring(playerId.Length - suffixLength, suffixLength)}";
        }

        return DefaultPlayerName;
    }

    private static async Task<T> WithTimeoutAsync<T>(Task<T> task, int timeoutSeconds, T timeoutResult, CancellationToken token)
    {
        Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), token);
        Task completedTask = await Task.WhenAny(task, timeoutTask);

        if (completedTask == task)
        {
            return await task;
        }

        return timeoutResult;
    }
}
