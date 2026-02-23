using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.CloudSave;
using Unity.Services.RemoteConfig;

public class TournamentManager : MonoBehaviour
{
    public static TournamentManager Instance { get; private set; }

    [Header("Admin Configuration")] 
    private List<string> adminUIDs = new List<string>();
    private bool isCurrentUserAdmin = false;

    [Header("Tournament Data")] 
    private List<TournamentData> allTournaments = new List<TournamentData>();

    [Header("Auto-Delete Settings")]
    private const float AUTO_DELETE_HOURS = 12f; // Delete 12 hours after end

    public event Action<List<TournamentData>> OnTournamentsUpdated;
    public event Action<string> OnTournamentJoined;
    public event Action<string> OnTournamentStarted;
    public event Action<string> OnTournamentEnded;
    public event Action<string, string> OnScoreSubmitted; // tournamentId, playerId

    private const string TOURNAMENTS_KEY = "all_tournaments";

    // IST TimeZone (UTC +5:30)
    private static readonly TimeZoneInfo ISTTimeZone = TimeZoneInfo.CreateCustomTimeZone(
        "IST",
        new TimeSpan(5, 30, 0),
        "India Standard Time",
        "India Standard Time"
    );

    private float statusCheckTimer = 0f;
    private const float STATUS_CHECK_INTERVAL = 5f; // Check every 5 seconds

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        await InitializeUnityServices();
    }

    private async Task InitializeUnityServices()
    {
        try
        {
            await FetchAdminUIDs();
            await LoadTournaments();

            Debug.Log($"Unity Services initialized. Player UID: {AuthenticationService.Instance.PlayerId}");
            Debug.Log($"Is Admin: {isCurrentUserAdmin}");
            Debug.Log($"Current IST Time: {GetCurrentISTTime():yyyy-MM-dd HH:mm:ss}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
        }
    }

    // ==================== TIMEZONE METHODS ====================

    // Get current time in IST
    public static DateTime GetCurrentISTTime()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ISTTimeZone);
    }

    // Convert IST to UTC for storage
    public static DateTime ConvertISTToUTC(DateTime istTime)
    {
        DateTime unspecifiedTime = DateTime.SpecifyKind(istTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecifiedTime, ISTTimeZone);
    }

    // Convert UTC to IST for display
    public static DateTime ConvertUTCToIST(DateTime utcTime)
    {
        DateTime utcDateTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, ISTTimeZone);
    }

    // ==================== ADMIN METHODS ====================

    private async Task FetchAdminUIDs()
    {
        try
        {
            adminUIDs = await RemoteConfigLoadManager.Instance.GetTournamentAdmins();
            if (adminUIDs.Count == 0)
            {
                Debug.LogWarning("No admin UIDs found in Remote Config");
            }

            for (int i = 0; i < adminUIDs.Count; i++)
            {
                Debug.Log($"Admin UID {i + 1}: {adminUIDs[i]}");
            }

            string currentUID = AuthenticationService.Instance.PlayerId;
            isCurrentUserAdmin = adminUIDs.Contains(currentUID);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to fetch admin UIDs: {e.Message}");
        }
    }

    public bool IsAdmin()
    {
        return isCurrentUserAdmin;
    }

    // Check if current user is the creator of a specific tournament
    public bool IsCreatorOfTournament(string tournamentId)
    {
        var tournament = GetTournament(tournamentId);
        if (tournament == null) return false;

        string currentUID = AuthenticationService.Instance.PlayerId;
        return tournament.creatorUID == currentUID;
    }

    // Get tournaments created by current user
    public List<TournamentData> GetMyCreatedTournaments()
    {
        string currentUID = AuthenticationService.Instance.PlayerId;
        return allTournaments.Where(t => t.creatorUID == currentUID).ToList();
    }

    // ==================== TOURNAMENT CRUD OPERATIONS ====================

    // Create Tournament (Only Admins) - Times in IST
    public async Task<bool> CreateTournament(string name, DateTime startTimeIST, DateTime endTimeIST,
        bool isPrivate, string password, int maxPlayers)
    {
        if (!isCurrentUserAdmin)
        {
            Debug.LogError("Only admins can create tournaments!");
            return false;
        }

        try
        {
            DateTime startUnspecified = DateTime.SpecifyKind(startTimeIST, DateTimeKind.Unspecified);
            DateTime endUnspecified = DateTime.SpecifyKind(endTimeIST, DateTimeKind.Unspecified);

            DateTime startTimeUTC = ConvertISTToUTC(startUnspecified);
            DateTime endTimeUTC = ConvertISTToUTC(endUnspecified);

            startTimeUTC = DateTime.SpecifyKind(startTimeUTC, DateTimeKind.Utc);
            endTimeUTC = DateTime.SpecifyKind(endTimeUTC, DateTimeKind.Utc);

            TournamentData newTournament = new TournamentData
            {
                tournamentName = name,
                startTime = startTimeUTC,
                endTime = endTimeUTC,
                isPrivate = isPrivate,
                password = password,
                creatorUID = AuthenticationService.Instance.PlayerId,
                maxPlayers = maxPlayers,
                status = TournamentStatus.Upcoming
            };

            allTournaments.Add(newTournament);
            await SaveTournaments();

            Debug.Log($"Tournament '{name}' created with ID: {newTournament.tournamentId}");
            Debug.Log($"Start (IST): {ConvertUTCToIST(newTournament.startTime):yyyy-MM-dd HH:mm}");
            Debug.Log($"End (IST): {ConvertUTCToIST(newTournament.endTime):yyyy-MM-dd HH:mm}");

            OnTournamentsUpdated?.Invoke(allTournaments);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create tournament: {e.Message}");
            return false;
        }
    }

    // Delete Tournament - ONLY CREATOR
    public async Task<bool> DeleteTournament(string tournamentId)
    {
        try
        {
            TournamentData tournament = allTournaments.FirstOrDefault(t => t.tournamentId == tournamentId);

            if (tournament == null)
            {
                Debug.LogError("Tournament not found!");
                return false;
            }

            if (!IsCreatorOfTournament(tournamentId))
            {
                Debug.LogError("Only the tournament creator can delete this tournament!");
                return false;
            }

            allTournaments.Remove(tournament);
            await SaveTournaments();

            Debug.Log($"Tournament '{tournament.tournamentName}' deleted successfully!");
            OnTournamentsUpdated?.Invoke(allTournaments);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete tournament: {e.Message}");
            return false;
        }
    }

    // Join Tournament
    public async Task<bool> JoinTournament(string tournamentId, string password = "")
    {
        try
        {
            TournamentData tournament = allTournaments.FirstOrDefault(t => t.tournamentId == tournamentId);

            if (tournament == null)
            {
                Debug.LogError("Tournament not found!");
                return false;
            }

            if (tournament.status != TournamentStatus.Upcoming)
            {
                Debug.LogError("Tournament has already started or ended!");
                return false;
            }

            if (tournament.isPrivate && tournament.password != password)
            {
                Debug.LogError("Incorrect password!");
                return false;
            }

            if (tournament.participantUIDs.Count >= tournament.maxPlayers)
            {
                Debug.LogError("Tournament is full!");
                return false;
            }

            string playerUID = AuthenticationService.Instance.PlayerId;

            if (tournament.participantUIDs.Contains(playerUID))
            {
                Debug.LogWarning("Already joined this tournament!");
                return false;
            }

            tournament.participantUIDs.Add(playerUID);
            await SaveTournaments();

            Debug.Log($"Successfully joined tournament: {tournament.tournamentName}");
            OnTournamentJoined?.Invoke(tournamentId);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join tournament: {e.Message}");
            return false;
        }
    }

    // Leave Tournament
    public async Task<bool> LeaveTournament(string tournamentId)
    {
        try
        {
            TournamentData tournament = allTournaments.FirstOrDefault(t => t.tournamentId == tournamentId);

            if (tournament == null)
            {
                Debug.LogError("Tournament not found!");
                return false;
            }

            if (tournament.status != TournamentStatus.Upcoming)
            {
                Debug.LogError("Cannot leave a tournament that has already started!");
                return false;
            }

            string playerUID = AuthenticationService.Instance.PlayerId;

            if (!tournament.participantUIDs.Contains(playerUID))
            {
                Debug.LogWarning("Not a participant in this tournament!");
                return false;
            }

            tournament.participantUIDs.Remove(playerUID);
            await SaveTournaments();

            Debug.Log($"Successfully left tournament: {tournament.tournamentName}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to leave tournament: {e.Message}");
            return false;
        }
    }

    // Start Tournament MANUALLY - ONLY CREATOR
    public async Task<bool> StartTournament(string tournamentId)
    {
        try
        {
            TournamentData tournament = allTournaments.FirstOrDefault(t => t.tournamentId == tournamentId);

            if (tournament == null)
            {
                Debug.LogError("Tournament not found!");
                return false;
            }

            if (!IsCreatorOfTournament(tournamentId))
            {
                Debug.LogError("Only the tournament creator can start this tournament!");
                return false;
            }

            if (tournament.status != TournamentStatus.Upcoming)
            {
                Debug.LogError("Tournament has already started or ended!");
                return false;
            }

            if (tournament.participantUIDs.Count == 0)
            {
                Debug.LogError("Cannot start tournament with no participants!");
                return false;
            }

            if (string.IsNullOrEmpty(tournament.selectedQuizPackName))
            {
                Debug.LogError("Cannot start tournament without selecting a quiz pack!");
                return false;
            }

            tournament.status = TournamentStatus.Active;
            await SaveTournaments();

            Debug.Log($"Tournament '{tournament.tournamentName}' started manually by creator!");
            OnTournamentStarted?.Invoke(tournamentId);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start tournament: {e.Message}");
            return false;
        }
    }

    // End Tournament - ONLY CREATOR
    public async Task<bool> EndTournament(string tournamentId)
    {
        try
        {
            TournamentData tournament = allTournaments.FirstOrDefault(t => t.tournamentId == tournamentId);

            if (tournament == null)
            {
                Debug.LogError("Tournament not found!");
                return false;
            }

            if (!IsCreatorOfTournament(tournamentId))
            {
                Debug.LogError("Only the tournament creator can end this tournament!");
                return false;
            }

            if (tournament.status != TournamentStatus.Active)
            {
                Debug.LogError("Tournament is not active!");
                return false;
            }

            tournament.status = TournamentStatus.Ended;
            tournament.endTime = DateTime.UtcNow;
            await SaveTournaments();

            Debug.Log($"Tournament '{tournament.tournamentName}' has ended!");
            OnTournamentEnded?.Invoke(tournamentId);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to end tournament: {e.Message}");
            return false;
        }
    }

    // Set Quiz Pack for Tournament - ONLY CREATOR
    public async Task<bool> SetTournamentQuizPack(string tournamentId, string quizPackName)
    {
        try
        {
            TournamentData tournament = allTournaments.FirstOrDefault(t => t.tournamentId == tournamentId);

            if (tournament == null)
            {
                Debug.LogError("Tournament not found!");
                return false;
            }

            if (!IsCreatorOfTournament(tournamentId))
            {
                Debug.LogError("Only the tournament creator can set quiz packs!");
                return false;
            }

            if (tournament.status != TournamentStatus.Upcoming)
            {
                Debug.LogError("Cannot change quiz pack for active or ended tournament!");
                return false;
            }

            tournament.selectedQuizPackName = quizPackName;
            await SaveTournaments();

            Debug.Log($"Quiz Pack '{quizPackName}' set for tournament '{tournament.tournamentName}'");
            OnTournamentsUpdated?.Invoke(allTournaments);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to set quiz pack: {e.Message}");
            return false;
        }
    }

    // ==================== LOCAL LEADERBOARD SYSTEM ====================

    // Submit Score to Local Leaderboard
    public async Task<bool> SubmitScore(string tournamentId, int score, string playerName = null)
    {
        try
        {
            string playerUID = AuthenticationService.Instance.PlayerId;
            
            // Check if player can play
            if (!CanPlayerPlayTournament(tournamentId, playerUID))
            {
                Debug.LogError("Player cannot play this tournament!");
                return false;
            }
            
            var tournament = GetTournament(tournamentId);
            
            // Check if player has already played
            if (HasPlayerPlayed(tournamentId, playerUID))
            {
                Debug.LogWarning("Player has already played this tournament!");
                return false;
            }
            
            // Use provided name or default
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = $"Player_{playerUID.Substring(0, 6)}";
            }
            
            // Create leaderboard entry
            CustomLeaderboardEntry entry = new CustomLeaderboardEntry(playerUID, playerName, score);
            
            // Add to local leaderboard
            tournament.localLeaderboard.Add(entry);
            
            // Sort leaderboard by score (descending) and update ranks
            SortAndUpdateRanks(tournament);
            
            // Mark player as played
            if (!tournament.playersWhoPlayed.Contains(playerUID))
            {
                tournament.playersWhoPlayed.Add(playerUID);
            }
            
            // Save everything
            await SaveTournaments();
            
            int playerRank = GetPlayerRank(tournamentId, playerUID);
            Debug.Log($"✓ Score {score} submitted successfully to local leaderboard!");
            Debug.Log($"✓ Player rank: #{playerRank}");
            
            OnScoreSubmitted?.Invoke(tournamentId, playerUID);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to submit score: {e.Message}");
            return false;
        }
    }

    // Sort leaderboard and update all ranks
    private void SortAndUpdateRanks(TournamentData tournament)
    {
        if (tournament == null || tournament.localLeaderboard == null) return;
        
        // Sort by score (descending)
        tournament.localLeaderboard.Sort((a, b) => b.score.CompareTo(a.score));
        
        // Update ranks - handle ties
        int currentRank = 1;
        for (int i = 0; i < tournament.localLeaderboard.Count; i++)
        {
            if (i > 0 && tournament.localLeaderboard[i].score < tournament.localLeaderboard[i - 1].score)
            {
                currentRank = i + 1;
            }
            tournament.localLeaderboard[i].rank = currentRank;
        }
    }

    // Get Local Leaderboard (with guaranteed correct ranks)
    public List<CustomLeaderboardEntry> GetLocalLeaderboard(string tournamentId, int limit = 100)
    {
        var tournament = GetTournament(tournamentId);
        if (tournament == null)
        {
            Debug.LogError("Tournament not found!");
            return new List<CustomLeaderboardEntry>();
        }
        
        // Ensure ranks are up to date
        SortAndUpdateRanks(tournament);
        
        // Return top N entries
        int count = Mathf.Min(limit, tournament.localLeaderboard.Count);
        return tournament.localLeaderboard.GetRange(0, count);
    }

    // Get Top N Players
    public List<CustomLeaderboardEntry> GetTopPlayers(string tournamentId, int topN = 10)
    {
        var tournament = GetTournament(tournamentId);
        if (tournament == null)
        {
            Debug.LogError("Tournament not found!");
            return new List<CustomLeaderboardEntry>();
        }
        
        // Ensure ranks are up to date
        SortAndUpdateRanks(tournament);
        
        int count = Mathf.Min(topN, tournament.localLeaderboard.Count);
        return tournament.localLeaderboard.GetRange(0, count);
    }

    // Get Player's Rank
    public int GetPlayerRank(string tournamentId, string playerId = null)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            playerId = AuthenticationService.Instance.PlayerId;
        }
        
        var tournament = GetTournament(tournamentId);
        if (tournament == null) return -1;
        
        // Ensure ranks are up to date
        SortAndUpdateRanks(tournament);
        
        var entry = tournament.localLeaderboard.Find(e => e.playerId == playerId);
        return entry != null ? entry.rank : -1;
    }

    // Get Player's Score
    public int GetPlayerScore(string tournamentId, string playerId = null)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            playerId = AuthenticationService.Instance.PlayerId;
        }
        
        var tournament = GetTournament(tournamentId);
        if (tournament == null) return -1;
        
        var entry = tournament.localLeaderboard.Find(e => e.playerId == playerId);
        return entry != null ? entry.score : -1;
    }

    // Get Player's Leaderboard Entry (with rank)
    public CustomLeaderboardEntry GetPlayerEntry(string tournamentId, string playerId = null)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            playerId = AuthenticationService.Instance.PlayerId;
        }
        
        var tournament = GetTournament(tournamentId);
        if (tournament == null) return null;
        
        // Ensure ranks are up to date
        SortAndUpdateRanks(tournament);
        
        return tournament.localLeaderboard.Find(e => e.playerId == playerId);
    }

    // Get players around a specific rank (for context)
    public List<CustomLeaderboardEntry> GetPlayersAroundRank(string tournamentId, int rank, int range = 5)
    {
        var tournament = GetTournament(tournamentId);
        if (tournament == null || tournament.localLeaderboard.Count == 0)
        {
            return new List<CustomLeaderboardEntry>();
        }
        
        // Ensure ranks are up to date
        SortAndUpdateRanks(tournament);
        
        int startIndex = Mathf.Max(0, rank - range - 1);
        int endIndex = Mathf.Min(tournament.localLeaderboard.Count, rank + range);
        int count = endIndex - startIndex;
        
        return tournament.localLeaderboard.GetRange(startIndex, count);
    }

    // Get leaderboard entries around current player
    public List<CustomLeaderboardEntry> GetLeaderboardAroundPlayer(string tournamentId, int range = 5)
    {
        string playerId = AuthenticationService.Instance.PlayerId;
        int playerRank = GetPlayerRank(tournamentId, playerId);
        
        if (playerRank <= 0)
        {
            // Player not found, return top players
            return GetTopPlayers(tournamentId, range * 2);
        }
        
        return GetPlayersAroundRank(tournamentId, playerRank, range);
    }

    // ==================== PLAYER TRACKING METHODS ====================

    // Check if player can play tournament
    public bool CanPlayerPlayTournament(string tournamentId, string playerId = null)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            playerId = AuthenticationService.Instance.PlayerId;
        }

        var tournament = GetTournament(tournamentId);

        if (tournament == null)
        {
            Debug.LogError("Tournament not found!");
            return false;
        }

        // Must be active
        if (tournament.status != TournamentStatus.Active)
        {
            Debug.LogError("Tournament is not active!");
            return false;
        }

        // Must be participant
        if (!tournament.participantUIDs.Contains(playerId))
        {
            Debug.LogError("Player is not a participant!");
            return false;
        }

        // Check if already played
        if (tournament.playersWhoPlayed.Contains(playerId))
        {
            Debug.LogError("Player has already played this tournament!");
            return false;
        }

        // Must have quiz pack selected
        if (string.IsNullOrEmpty(tournament.selectedQuizPackName))
        {
            Debug.LogError("No quiz pack selected for this tournament!");
            return false;
        }

        return true;
    }

    // Check if player has already played (submitted score)
    public bool HasPlayerPlayed(string tournamentId, string playerId = null)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            playerId = AuthenticationService.Instance.PlayerId;
        }

        var tournament = GetTournament(tournamentId);
        if (tournament == null)
        {
            Debug.LogError("Tournament not found!");
            return false;
        }

        return tournament.playersWhoPlayed.Contains(playerId);
    }

    // Get list of players who have played
    public List<string> GetPlayersWhoPlayed(string tournamentId)
    {
        var tournament = GetTournament(tournamentId);
        if (tournament == null)
        {
            Debug.LogWarning("Tournament not found!");
            return new List<string>();
        }

        return new List<string>(tournament.playersWhoPlayed);
    }

    // Get list of players who joined but haven't played yet
    public List<string> GetPlayersWhoHaventPlayed(string tournamentId)
    {
        var tournament = GetTournament(tournamentId);
        if (tournament == null)
        {
            Debug.LogWarning("Tournament not found!");
            return new List<string>();
        }

        return tournament.participantUIDs
            .Where(uid => !tournament.playersWhoPlayed.Contains(uid))
            .ToList();
    }

    // Get participation statistics
    public (int joined, int played, int notPlayed) GetParticipationStats(string tournamentId)
    {
        var tournament = GetTournament(tournamentId);
        if (tournament == null)
        {
            return (0, 0, 0);
        }

        int joined = tournament.participantUIDs.Count;
        int played = tournament.playersWhoPlayed.Count;
        int notPlayed = joined - played;

        return (joined, played, notPlayed);
    }

    // ==================== TOURNAMENT QUERY METHODS ====================

    // Get All Active Tournaments
    public List<TournamentData> GetActiveTournaments()
    {
        return allTournaments.Where(t =>
            t.status == TournamentStatus.Upcoming ||
            t.status == TournamentStatus.Active
        ).ToList();
    }

    // Get All Tournaments
    public List<TournamentData> GetAllTournaments()
    {
        return new List<TournamentData>(allTournaments);
    }

    // Get Tournaments by Status
    public List<TournamentData> GetTournamentsByStatus(TournamentStatus status)
    {
        return allTournaments.Where(t => t.status == status).ToList();
    }

    // Get Tournaments Player Joined
    public List<TournamentData> GetPlayerTournaments(string playerId = null)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            playerId = AuthenticationService.Instance.PlayerId;
        }

        return allTournaments.Where(t => t.participantUIDs.Contains(playerId)).ToList();
    }

    // Get Tournament by ID
    public TournamentData GetTournament(string tournamentId)
    {
        return allTournaments.FirstOrDefault(t => t.tournamentId == tournamentId);
    }

    // Check if Player is in Tournament
    public bool IsPlayerInTournament(string tournamentId, string playerId = null)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            playerId = AuthenticationService.Instance.PlayerId;
        }

        var tournament = GetTournament(tournamentId);
        return tournament != null && tournament.participantUIDs.Contains(playerId);
    }

    // Get Tournament Statistics
    public TournamentStats GetTournamentStats(string tournamentId)
    {
        var tournament = GetTournament(tournamentId);
        if (tournament == null) return null;

        int totalParticipants = tournament.participantUIDs.Count;
        int totalPlayed = tournament.playersWhoPlayed.Count;
        int totalNotPlayed = totalParticipants - totalPlayed;
        double participationRate = totalParticipants > 0 
            ? (double)totalPlayed / totalParticipants * 100 
            : 0;
        
        int highestScore = 0;
        int lowestScore = 0;
        double averageScore = 0;
        
        if (tournament.localLeaderboard.Count > 0)
        {
            highestScore = tournament.localLeaderboard[0].score; // Already sorted
            lowestScore = tournament.localLeaderboard[tournament.localLeaderboard.Count - 1].score;
            averageScore = tournament.localLeaderboard.Average(e => e.score);
        }

        return new TournamentStats
        {
            totalParticipants = totalParticipants,
            totalPlayersPlayed = totalPlayed,
            totalPlayersNotPlayed = totalNotPlayed,
            participationRate = participationRate,
            totalScoresSubmitted = tournament.localLeaderboard.Count,
            highestScore = highestScore,
            lowestScore = lowestScore,
            averageScore = averageScore,
            tournamentDuration = tournament.endTime - tournament.startTime
        };
    }

    // ==================== SAVE/LOAD METHODS ====================

    // Save Tournaments
    private async Task SaveTournaments()
    {
        try
        {
            TournamentList wrapper = new TournamentList { tournaments = allTournaments };
            string json = JsonUtility.ToJson(wrapper);

            if (string.IsNullOrEmpty(json) || json == "{}")
            {
                Debug.LogError("Failed to serialize tournaments to JSON");
                return;
            }

            Debug.Log($"Saving {allTournaments.Count} tournaments");

            var result = await CloudCodeService.Instance.CallEndpointAsync<Dictionary<string, object>>(
                "SaveTournaments",
                new Dictionary<string, object>
                {
                    { "tournaments", json }
                }
            );

            bool success = result.ContainsKey("success") && (bool)result["success"];
            
            if (success)
            {
                Debug.Log("Tournaments saved successfully");
                
                // Check if any expired tournaments were deleted
                if (result.ContainsKey("deletedExpired"))
                {
                    int deleted = Convert.ToInt32(result["deletedExpired"]);
                    if (deleted > 0)
                    {
                        Debug.Log($"[SERVER-CLEANUP] Server deleted {deleted} expired tournament(s)");
                    }
                }
            }
            
            OnTournamentsUpdated?.Invoke(allTournaments);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save tournaments: {e.Message}");
        }
    }

    public async void LoadTournamentButton()
    {
        await LoadTournaments();
    }

    // Load Tournaments with auto-cleanup
   // Load Tournaments with auto-cleanup
public async Task LoadTournaments()
{
    try
    {
        Debug.Log("=== LOADING TOURNAMENTS ===");
        
        var response = await CloudCodeService.Instance
            .CallEndpointAsync<string>(
                "LoadTournaments",
                new Dictionary<string, object>()
            );

        Debug.Log($"Raw response from Cloud Code: {response}");
        Debug.Log($"Response length: {response?.Length ?? 0}");
        Debug.Log($"Response is null: {response == null}");
        Debug.Log($"Response is empty: {string.IsNullOrEmpty(response)}");

        if (string.IsNullOrEmpty(response) || response == "{}")
        {
            Debug.LogWarning("Cloud Code returned empty response - initializing empty tournament list");
            allTournaments = new List<TournamentData>();
            OnTournamentsUpdated?.Invoke(allTournaments);
            return;
        }

        // Try to parse the JSON
        try
        {
            // First, check if it's already a wrapped object
            TournamentList tournamentList = JsonUtility.FromJson<TournamentList>(response);
            
            if (tournamentList == null)
            {
                Debug.LogError("Failed to deserialize TournamentList - result is null");
                allTournaments = new List<TournamentData>();
            }
            else if (tournamentList.tournaments == null)
            {
                Debug.LogWarning("TournamentList.tournaments is null - initializing empty list");
                allTournaments = new List<TournamentData>();
            }
            else
            {
                allTournaments = tournamentList.tournaments;
                Debug.Log($"✓ Successfully loaded {allTournaments.Count} tournaments");
                
                // Log each tournament for debugging
                foreach (var t in allTournaments)
                {
                    Debug.Log($"  - {t.tournamentName} [{t.status}] (ID: {t.tournamentId})");
                }
            }
            
            // Client-side cleanup on load (backup)
            if (allTournaments.Count > 0)
            {
                bool cleaned = await CleanupExpiredTournamentsNow();
                if (cleaned)
                {
                    Debug.Log("[CLIENT-CLEANUP] Expired tournaments cleaned up on load");
                }
            }
        }
        catch (Exception parseEx)
        {
            Debug.LogError($"JSON Parse Error: {parseEx.Message}");
            Debug.LogError($"JSON that failed to parse: {response}");
            allTournaments = new List<TournamentData>();
        }

        OnTournamentsUpdated?.Invoke(allTournaments);
        Debug.Log($"=== LOAD COMPLETE: {allTournaments.Count} tournaments ===");
    }
    catch (Exception e)
    {
        Debug.LogError($"Failed to load tournaments: {e.Message}");
        Debug.LogError($"Stack trace: {e.StackTrace}");
        allTournaments = new List<TournamentData>();
        OnTournamentsUpdated?.Invoke(allTournaments);
    }
}


    // ==================== AUTO-DELETE SYSTEM ====================

    private void Update()
    {
        statusCheckTimer += Time.deltaTime;

        if (statusCheckTimer >= STATUS_CHECK_INTERVAL)
        {
            statusCheckTimer = 0f;
            CheckTournamentStatus();
        }
    }

    private void CheckTournamentStatus()
    {
        if (allTournaments == null || allTournaments.Count == 0) return;

        bool changed = false;
        DateTime currentTime = DateTime.UtcNow;

        foreach (var tournament in allTournaments.ToList())
        {
            // Auto-end tournaments that passed end time
            if (tournament.status == TournamentStatus.Active && currentTime >= tournament.endTime)
            {
                tournament.status = TournamentStatus.Ended;
                changed = true;
                Debug.Log(
                    $"Tournament '{tournament.tournamentName}' has ended automatically at {ConvertUTCToIST(currentTime):yyyy-MM-dd HH:mm:ss} IST");
                OnTournamentEnded?.Invoke(tournament.tournamentId);
            }

            // Auto-start tournaments that reached scheduled start time
            if (tournament.status == TournamentStatus.Upcoming &&
                currentTime >= tournament.startTime &&
                tournament.participantUIDs.Count > 0 &&
                !string.IsNullOrEmpty(tournament.selectedQuizPackName))
            {
                tournament.status = TournamentStatus.Active;
                changed = true;

                DateTime startIST = ConvertUTCToIST(currentTime);
                Debug.Log(
                    $"Tournament '{tournament.tournamentName}' auto-started at {startIST:yyyy-MM-dd HH:mm:ss} IST");

                OnTournamentStarted?.Invoke(tournament.tournamentId);
            }
        }

        if (changed)
        {
            _ = SaveTournaments();
        }
    }

    // Cleanup expired tournaments immediately
    private async Task<bool> CleanupExpiredTournamentsNow()
    {
        DateTime currentTime = DateTime.UtcNow;
        List<TournamentData> tournamentsToDelete = new List<TournamentData>();
        
        foreach (var tournament in allTournaments.ToList())
        {
            if (tournament.status == TournamentStatus.Ended)
            {
                TimeSpan timeSinceEnd = currentTime - tournament.endTime;
                
                if (timeSinceEnd.TotalHours >= AUTO_DELETE_HOURS)
                {
                    tournamentsToDelete.Add(tournament);
                    DateTime endIST = ConvertUTCToIST(tournament.endTime);
                    Debug.Log($"[AUTO-DELETE] Removing '{tournament.tournamentName}' (ended {timeSinceEnd.TotalHours:F1}h ago at {endIST:HH:mm} IST)");
                }
            }
        }
        
        if (tournamentsToDelete.Count > 0)
        {
            foreach (var tournament in tournamentsToDelete)
            {
                allTournaments.Remove(tournament);
            }
            
            await SaveTournaments();
            OnTournamentsUpdated?.Invoke(allTournaments);
            Debug.Log($"[AUTO-DELETE] ✓ Removed {tournamentsToDelete.Count} expired tournament(s)");
            return true;
        }
        
        return false;
    }

    // Manual method to check what will be deleted soon
    [ContextMenu("Debug: Check Tournaments Pending Deletion")]
    public void CheckPendingDeletion()
    {
        DateTime currentTime = DateTime.UtcNow;
        int pendingCount = 0;
        
        Debug.Log("=== TOURNAMENTS PENDING AUTO-DELETE ===");
        
        foreach (var tournament in allTournaments)
        {
            if (tournament.status == TournamentStatus.Ended)
            {
                TimeSpan timeSinceEnd = currentTime - tournament.endTime;
                TimeSpan timeUntilDelete = TimeSpan.FromHours(AUTO_DELETE_HOURS) - timeSinceEnd;
                
                if (timeUntilDelete.TotalSeconds > 0)
                {
                    pendingCount++;
                    Debug.Log($"'{tournament.tournamentName}' - Deletes in {timeUntilDelete.TotalHours:F1} hours");
                }
                else
                {
                    Debug.Log($"'{tournament.tournamentName}' - READY FOR DELETION (ended {timeSinceEnd.TotalHours:F1}h ago)");
                }
            }
        }
        
        if (pendingCount == 0)
        {
            Debug.Log("No tournaments pending deletion");
        }
    }

    // ==================== DEBUG METHODS ====================

    [ContextMenu("Debug: Print All Tournaments")]
    public void DebugPrintAllTournaments()
    {
        Debug.Log($"=== ALL TOURNAMENTS ({allTournaments.Count}) ===");
        foreach (var t in allTournaments)
        {
            Debug.Log($"• {t.tournamentName} [{t.status}] - {t.participantUIDs.Count} joined, {t.playersWhoPlayed.Count} played, {t.localLeaderboard.Count} scores");
        }
    }

    [ContextMenu("Debug: Force Reload Tournaments")]
    public async void DebugForceReload()
    {
        await LoadTournaments();
        Debug.Log("Tournaments reloaded from server");
    }

    // ==================== DATA CLASSES ====================

    [Serializable]
    public class TournamentList
    {
        public List<TournamentData> tournaments = new List<TournamentData>();
    }

    // Tournament Statistics
    [Serializable]
    public class TournamentStats
    {
        public int totalParticipants;
        public int totalPlayersPlayed;
        public int totalPlayersNotPlayed;
        public double participationRate;
        public int totalScoresSubmitted;
        public int highestScore;
        public int lowestScore;
        public double averageScore;
        public TimeSpan tournamentDuration;
    }
}
