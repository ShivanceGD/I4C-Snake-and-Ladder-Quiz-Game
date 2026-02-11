using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Services.Authentication;

public class LeaderboardAPIManager : MonoBehaviour
{
    public static LeaderboardAPIManager Instance { get; private set; }

    private const string UGS_BASE_URL = "https://leaderboards.services.api.unity.com/v1";
    private string projectId;
    private string accessToken;

    [Header("API Configuration")]
    [Tooltip("Get from Unity Dashboard → Project Settings → Project ID")]
    public string unityProjectId;
    
    [Tooltip("Service Account Key ID from Unity Dashboard")]
    public string serviceAccountKeyId;
    
    [Tooltip("Service Account Secret Key from Unity Dashboard")]
    public string serviceAccountSecretKey;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        projectId = unityProjectId;
    }

    // Get OAuth Token for API calls
    private async Task<bool> AuthenticateServiceAccount()
    {
        try
        {
            string authUrl = "https://services.api.unity.com/auth/v1/token";
            
            var authData = new
            {
                grant_type = "client_credentials",
                client_id = serviceAccountKeyId,
                client_secret = serviceAccountSecretKey
            };

            string jsonData = JsonUtility.ToJson(authData);
            
            using (UnityWebRequest request = new UnityWebRequest(authUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                    accessToken = response.access_token;
                    Debug.Log("Service Account authenticated successfully!");
                    return true;
                }
                else
                {
                    Debug.LogError($"Authentication failed: {request.error}");
                    return false;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Service Account authentication error: {e.Message}");
            return false;
        }
    }

    // Create Leaderboard via API
    public async Task<bool> CreateLeaderboard(string leaderboardId, string displayName, string description = "")
    {
        try
        {
            // Authenticate first
            if (string.IsNullOrEmpty(accessToken))
            {
                bool authenticated = await AuthenticateServiceAccount();
                if (!authenticated) return false;
            }

            string url = $"{UGS_BASE_URL}/projects/{projectId}/leaderboards";

            var leaderboardConfig = new LeaderboardCreateRequest
            {
                id = leaderboardId,
                name = displayName,
                sortOrder = "desc",
                updateType = "keepBest",
                bucketSize = 0,
                resetConfig = new ResetConfig { start = null, schedule = null }
            };

            string jsonData = JsonUtility.ToJson(leaderboardConfig);
            
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"Leaderboard '{leaderboardId}' created successfully!");
                    Debug.Log($"Response: {request.downloadHandler.text}");
                    return true;
                }
                else
                {
                    Debug.LogError($"Failed to create leaderboard: {request.error}");
                    Debug.LogError($"Response: {request.downloadHandler.text}");
                    
                    // Retry authentication if token expired
                    if (request.responseCode == 401)
                    {
                        accessToken = null;
                        Debug.Log("Token expired, retrying...");
                        return await CreateLeaderboard(leaderboardId, displayName, description);
                    }
                    
                    return false;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create leaderboard: {e.Message}");
            return false;
        }
    }

    // Delete Leaderboard via API
    public async Task<bool> DeleteLeaderboard(string leaderboardId)
    {
        try
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                bool authenticated = await AuthenticateServiceAccount();
                if (!authenticated) return false;
            }

            string url = $"{UGS_BASE_URL}/projects/{projectId}/leaderboards/{leaderboardId}";
            
            using (UnityWebRequest request = UnityWebRequest.Delete(url))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"Leaderboard '{leaderboardId}' deleted successfully!");
                    return true;
                }
                else
                {
                    Debug.LogError($"Failed to delete leaderboard: {request.error}");
                    return false;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete leaderboard: {e.Message}");
            return false;
        }
    }

    // Get Leaderboard Info
    public async Task<LeaderboardInfo> GetLeaderboardInfo(string leaderboardId)
    {
        try
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                bool authenticated = await AuthenticateServiceAccount();
                if (!authenticated) return null;
            }

            string url = $"{UGS_BASE_URL}/projects/{projectId}/leaderboards/{leaderboardId}";
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var info = JsonUtility.FromJson<LeaderboardInfo>(request.downloadHandler.text);
                    return info;
                }
                else
                {
                    Debug.LogError($"Failed to get leaderboard info: {request.error}");
                    return null;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to get leaderboard info: {e.Message}");
            return null;
        }
    }

    // List All Leaderboards
    public async Task<List<LeaderboardInfo>> ListAllLeaderboards()
    {
        try
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                bool authenticated = await AuthenticateServiceAccount();
                if (!authenticated) return null;
            }

            string url = $"{UGS_BASE_URL}/projects/{projectId}/leaderboards";
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<LeaderboardListResponse>(request.downloadHandler.text);
                    return response.results;
                }
                else
                {
                    Debug.LogError($"Failed to list leaderboards: {request.error}");
                    return null;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to list leaderboards: {e.Message}");
            return null;
        }
    }
}

// API Response Models
[Serializable]
public class AuthResponse
{
    public string access_token;
    public int expires_in;
    public string token_type;
}

[Serializable]
public class LeaderboardCreateRequest
{
    public string id;
    public string name;
    public string sortOrder;
    public string updateType;
    public int bucketSize;
    public ResetConfig resetConfig;
}

[Serializable]
public class ResetConfig
{
    public string start;
    public string schedule;
}

[Serializable]
public class LeaderboardInfo
{
    public string id;
    public string name;
    public string sortOrder;
    public string updateType;
    public int bucketSize;
    public string created;
    public string updated;
}

[Serializable]
public class LeaderboardListResponse
{
    public List<LeaderboardInfo> results;
    public int total;
}

// Extension method for async UnityWebRequest
public static class UnityWebRequestExtensions
{
    public static System.Threading.Tasks.Task SendWebRequest(this UnityWebRequest request)
    {
        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
        
        request.SendWebRequest().completed += _ =>
        {
            tcs.SetResult(true);
        };
        
        return tcs.Task;
    }
}
