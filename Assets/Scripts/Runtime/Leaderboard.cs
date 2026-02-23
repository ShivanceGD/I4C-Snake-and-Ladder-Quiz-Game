using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

public class Leaderboard : MonoBehaviour
{
    public static Leaderboard Instance;
    // Create a leaderboard with this ID in the Unity Dashboard
    const string LeaderboardId = "Indian";
    [SerializeField] private int offset;
    [SerializeField] private int limit;
    string VersionId { get; set; }
    int Offset { get; set; }
    int Limit { get; set; }
   // int RangeLimit { get; set; }
    //List<string> FriendIds { get; set; }

    /*async void Awake()
    {
        await UnityServices.InitializeAsync();

        await SignInAnonymously();
    }
    

    async Task SignInAnonymously()
    {
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in as: " + AuthenticationService.Instance.PlayerId);
        };
        AuthenticationService.Instance.SignInFailed += s =>
        {
            // Take some action here...
            Debug.Log(s);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }*/
    private void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async void AddScore(int score)
    {
        var scoreResponse = await LeaderboardsService.Instance.AddPlayerScoreAsync(LeaderboardId, score);
        Debug.Log(JsonConvert.SerializeObject(scoreResponse));
        
    }

    /*public async Task<LeaderboardEntry> GetScores()
    {
        var scoresResponse=await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId);
        //Debug.Log(JsonConvert.SerializeObject(scoresResponse));
        return scoresResponse;
    }*/

    public async Task<LeaderboardScoresPage> GetPaginatedScores()
    {
        Offset = offset;
        Limit = limit;
        var Scores=await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId, new GetScoresOptions{Offset = Offset, Limit = Limit});
        //Debug.Log(JsonConvert.SerializeObject(scoresResponse));
        return Scores;
    }

    public async Task<LeaderboardEntry> GetPlayerScore()
    {
        
        try
        {
            var playerScore = await LeaderboardsService.Instance.GetPlayerScoreAsync(LeaderboardId);
            Debug.Log($"Player ID: {playerScore.PlayerId},Player Name:{playerScore.PlayerName}, Score: {playerScore.Score}");
            return playerScore;
        }
        catch (LeaderboardsException ex)
        {
            if (ex.Reason == LeaderboardsExceptionReason.EntryNotFound)
            {
                // Optionally set default score
                Debug.Log("Player has no score yet. Setting default score = 0");
                await LeaderboardsService.Instance.AddPlayerScoreAsync(LeaderboardId, 0);
                var playerScore = await LeaderboardsService.Instance.GetPlayerScoreAsync(LeaderboardId);
                return playerScore;
            }
            else
            {
                Debug.LogError($"Leaderboard error: {ex.Message}");
                return null;
            }
        }
    }
    /*public async void GetsPlayerScore()
    {
        
        try
        {
            var playerScore = await LeaderboardsService.Instance.GetPlayerScoreAsync(LeaderboardId);
            Debug.Log($"Player ID: {playerScore.PlayerId}, Score: {playerScore.Score}");
        }
        catch (LeaderboardsException ex)
        {
            if (ex.Reason == LeaderboardsExceptionReason.EntryNotFound)
            {
                // Optionally set default score
                await LeaderboardsService.Instance.AddPlayerScoreAsync(LeaderboardId, 0);
                Debug.Log("Player has no score yet. Setting default score = 0");
            }
            else
            {
                Debug.LogError($"Leaderboard error: {ex.Message}");
            }
        }
        
    }*/

    public async void GetVersionScores()
    {
        var versionScoresResponse =
            await LeaderboardsService.Instance.GetVersionScoresAsync(LeaderboardId, VersionId);
    Debug.Log(JsonConvert.SerializeObject(versionScoresResponse));
    }
}