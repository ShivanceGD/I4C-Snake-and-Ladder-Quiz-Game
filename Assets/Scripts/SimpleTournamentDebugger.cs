using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;

public class TournamentDebugger : MonoBehaviour
{
    [Header("Debugger Settings")]
    [SerializeField] private bool runOnStart = false;
    [SerializeField] private float delayBetweenSteps = 2f;
    [SerializeField] private bool verboseLogging = true;
    
    [Header("Test Configuration")]
    [SerializeField] private string testTournamentName = "Debug Test Tournament";
    [SerializeField] private int tournamentDurationMinutes = 60;
    [SerializeField] private int maxPlayers = 100;
    [SerializeField] private bool createPrivateTournament = false;
    [SerializeField] private string testPassword = "test123";
    [SerializeField] private string testQuizPackName = "TestQuizPack";
    
    [Header("Score Testing")]
    [SerializeField] private int testScore = 1000;
    [SerializeField] private string testPlayerName = "TestPlayer";
    
    [Header("Statistics")]
    public int testsPassed = 0;
    public int testsFailed = 0;
    public int testsSkipped = 0;
    
    private string createdTournamentId;

    private void Start()
    {
        if (runOnStart)
        {
            _ = RunCompleteTest();
        }
    }

    [ContextMenu("Run Complete Test Suite")]
    public async Task RunCompleteTest()
    {
        ResetStats();
        LogHeader("TOURNAMENT SYSTEM TEST (Local Leaderboard + Player Tracking)");
        
        await WaitForInitialization();
        await TestSystemChecks();
        await TestTournamentLifecycle();
        await TestLocalLeaderboard(); // Updated
        await TestPlayerTracking();
        await TestPlayerOperations();
        await TestEdgeCases();
        
        PrintFinalReport();
    }

    [ContextMenu("Test Tournament Creation")]
    public async void TestCreateOnly()
    {
        ResetStats();
        await TestTournamentCreation();
        PrintStats();
    }

    [ContextMenu("Test Local Leaderboard")]
    public async void TestLeaderboardOnly()
    {
        ResetStats();
        await TestLocalLeaderboard();
        PrintStats();
    }

    [ContextMenu("Test Player Tracking")]
    public async void TestPlayerTrackingOnly()
    {
        ResetStats();
        await TestPlayerTracking();
        PrintStats();
    }

    [ContextMenu("List All Tournaments")]
    public void ListAllTournaments()
    {
        var tournaments = TournamentManager.Instance.GetAllTournaments();
        LogInfo($"Total Tournaments: {tournaments.Count}");
        
        foreach (var t in tournaments)
        {
            var stats = TournamentManager.Instance.GetParticipationStats(t.tournamentId);
            LogInfo($"  • {t.tournamentName} [{t.status}] - {stats.joined} joined, {stats.played} played, {t.localLeaderboard.Count} scores");
        }
    }

    [ContextMenu("Cleanup Test Tournaments")]
    public async void CleanupTestTournaments()
    {
        var tournaments = TournamentManager.Instance.GetAllTournaments();
        int deleted = 0;
        
        foreach (var t in tournaments)
        {
            if ((t.tournamentName.Contains("Test") || t.tournamentName.Contains("Debug")) &&
                TournamentManager.Instance.IsCreatorOfTournament(t.tournamentId))
            {
                await TournamentManager.Instance.DeleteTournament(t.tournamentId);
                deleted++;
            }
        }
        
        LogSuccess($"Cleaned up {deleted} test tournaments");
    }

    // ==================== TEST SUITES ====================
    
    private async Task TestSystemChecks()
    {
        LogSection("SYSTEM CHECKS");
        
        TestCheck("TournamentManager Instance", TournamentManager.Instance != null);
        TestCheck("Unity Services Authenticated", AuthenticationService.Instance.IsSignedIn);
        
        if (AuthenticationService.Instance.IsSignedIn)
        {
            LogInfo($"Player ID: {AuthenticationService.Instance.PlayerId}");
        }
        
        bool isAdmin = TournamentManager.Instance.IsAdmin();
        LogInfo($"Admin Status: {isAdmin}");
        
        if (!isAdmin)
        {
            LogWarn("Running as regular player - some tests will be skipped");
        }
        
        // Test timezone functions
        DateTime currentIST = TournamentManager.GetCurrentISTTime();
        LogInfo($"Current IST Time: {currentIST:yyyy-MM-dd HH:mm:ss}");
        
        await Task.Yield();
    }

    private async Task TestTournamentLifecycle()
    {
        LogSection("TOURNAMENT LIFECYCLE");
        
        await TestTournamentCreation();
        await Delay();
        
        await TestQuizPackSelection();
        await Delay();
        
        await TestTournamentJoin();
        await Delay();
        
        await TestTournamentStart();
        await Delay();
        
        await TestScoreSubmission();
        await Delay();
        
        await TestTournamentEnd();
        await Delay();
        
        await TestTournamentDeletion();
    }

    private async Task TestLocalLeaderboard()
    {
        LogSection("LOCAL LEADERBOARD FEATURES");
        
        await TestLocalLeaderboardAccess();
        await TestPlayerRanking();
        await TestTopPlayers();
    }

    private async Task TestPlayerTracking()
    {
        LogSection("PLAYER TRACKING (playersWhoPlayed)");
        
        await TestHasPlayerPlayed();
        await TestParticipationStats();
        await TestPlayersWhoHaventPlayed();
    }

    private async Task TestPlayerOperations()
    {
        LogSection("PLAYER OPERATIONS");
        
        await TestPlayerJoinLeave();
        await TestPasswordProtection();
        await TestMaxPlayersLimit();
        await TestCreatorPermissions();
    }

    private async Task TestEdgeCases()
    {
        LogSection("EDGE CASES");
        
        await TestInvalidOperations();
        await TestDuplicateScoreSubmission();
    }

    // ==================== INDIVIDUAL TESTS ====================
    
    private async Task WaitForInitialization()
    {
        LogStep("Waiting for services...");
        
        int maxWait = 100;
        int waited = 0;
        
        while (waited < maxWait)
        {
            if (TournamentManager.Instance != null && 
                AuthenticationService.Instance.IsSignedIn)
            {
                TestPass("Services initialized");
                return;
            }
            
            await Task.Delay(100);
            waited++;
        }
        
        TestFail("Services initialization timeout");
    }

    private async Task TestTournamentCreation()
    {
        LogStep("Testing tournament creation");
        
        if (!TournamentManager.Instance.IsAdmin())
        {
            TestSkip("Not an admin");
            return;
        }
        
        DateTime startTimeIST = TournamentManager.GetCurrentISTTime().AddMinutes(2);
        DateTime endTimeIST = startTimeIST.AddMinutes(tournamentDurationMinutes);
        
        bool success = await TournamentManager.Instance.CreateTournament(
            testTournamentName,
            startTimeIST,
            endTimeIST,
            createPrivateTournament,
            testPassword,
            maxPlayers
        );
        
        if (success)
        {
            var tournaments = TournamentManager.Instance.GetActiveTournaments();
            var created = tournaments.Find(t => t.tournamentName == testTournamentName);
            
            if (created != null)
            {
                createdTournamentId = created.tournamentId;
                TestPass($"Tournament created: {createdTournamentId}");
                TestCheck("Initial status is Upcoming", created.status == TournamentStatus.Upcoming);
                TestCheck("No participants initially", created.participantUIDs.Count == 0);
                TestCheck("playersWhoPlayed initialized", created.playersWhoPlayed != null);
                TestCheck("playersWhoPlayed empty", created.playersWhoPlayed.Count == 0);
                TestCheck("localLeaderboard initialized", created.localLeaderboard != null);
                TestCheck("localLeaderboard empty", created.localLeaderboard.Count == 0);
            }
            else
            {
                TestFail("Tournament created but not found in list");
            }
        }
        else
        {
            TestFail("Tournament creation failed");
        }
    }

    private async Task TestQuizPackSelection()
    {
        LogStep("Testing quiz pack selection");
        
        if (string.IsNullOrEmpty(createdTournamentId))
        {
            TestSkip("No tournament available");
            return;
        }
        
        if (!TournamentManager.Instance.IsCreatorOfTournament(createdTournamentId))
        {
            TestSkip("Not the creator");
            return;
        }
        
        bool success = await TournamentManager.Instance.SetTournamentQuizPack(
            createdTournamentId, 
            testQuizPackName
        );
        
        if (success)
        {
            var tournament = TournamentManager.Instance.GetTournament(createdTournamentId);
            TestPass("Quiz pack set successfully");
            TestCheck("Quiz pack name saved", tournament.selectedQuizPackName == testQuizPackName);
        }
        else
        {
            TestFail("Failed to set quiz pack");
        }
    }

    private async Task TestTournamentJoin()
    {
        LogStep("Testing tournament join");
        
        if (string.IsNullOrEmpty(createdTournamentId))
        {
            TestSkip("No tournament to join");
            return;
        }
        
        string password = createPrivateTournament ? testPassword : "";
        bool success = await TournamentManager.Instance.JoinTournament(createdTournamentId, password);
        
        if (success)
        {
            var tournament = TournamentManager.Instance.GetTournament(createdTournamentId);
            TestPass("Joined tournament");
            TestCheck("Player added to participants", 
                tournament.participantUIDs.Contains(AuthenticationService.Instance.PlayerId));
            TestCheck("Player not in playersWhoPlayed yet", 
                !tournament.playersWhoPlayed.Contains(AuthenticationService.Instance.PlayerId));
        }
        else
        {
            TestFail("Failed to join tournament");
        }
    }

    private async Task TestTournamentStart()
    {
        LogStep("Testing tournament start");
        
        if (string.IsNullOrEmpty(createdTournamentId))
        {
            TestSkip("No tournament to start");
            return;
        }
        
        if (!TournamentManager.Instance.IsCreatorOfTournament(createdTournamentId))
        {
            TestSkip("Not the creator");
            return;
        }
        
        bool success = await TournamentManager.Instance.StartTournament(createdTournamentId);
        
        if (success)
        {
            var tournament = TournamentManager.Instance.GetTournament(createdTournamentId);
            TestPass("Tournament started");
            TestCheck("Status changed to Active", tournament.status == TournamentStatus.Active);
        }
        else
        {
            TestFail("Failed to start tournament");
        }
    }

    private async Task TestScoreSubmission()
    {
        LogStep("Testing score submission to local leaderboard");
        
        if (string.IsNullOrEmpty(createdTournamentId))
        {
            TestSkip("No active tournament");
            return;
        }
        
        string playerId = AuthenticationService.Instance.PlayerId;
        
        // Check if player can play
        bool canPlay = TournamentManager.Instance.CanPlayerPlayTournament(createdTournamentId, playerId);
        TestCheck("Player can play tournament", canPlay);
        
        if (!canPlay)
        {
            TestSkip("Player cannot play");
            return;
        }
        
        // Submit score to LOCAL leaderboard
        bool success = await TournamentManager.Instance.SubmitScore(
            createdTournamentId, 
            testScore, 
            testPlayerName
        );
        
        if (success)
        {
            TestPass($"Score {testScore} submitted to local leaderboard");
            
            var tournament = TournamentManager.Instance.GetTournament(createdTournamentId);
            TestCheck("Player marked as played", 
                tournament.playersWhoPlayed.Contains(playerId));
            TestCheck("Score added to local leaderboard",
                tournament.localLeaderboard.Count > 0);
            
            // Check HasPlayerPlayed method
            bool hasPlayed = TournamentManager.Instance.HasPlayerPlayed(createdTournamentId, playerId);
            TestCheck("HasPlayerPlayed returns true", hasPlayed);
            
            // Check player entry
            var playerEntry = TournamentManager.Instance.GetPlayerEntry(createdTournamentId, playerId);
            TestCheck("Player entry exists", playerEntry != null);
            if (playerEntry != null)
            {
                TestCheck("Player entry has correct score", playerEntry.score == testScore);
                TestCheck("Player entry has rank assigned", playerEntry.rank > 0);
                LogInfo($"Player rank: #{playerEntry.rank}, Score: {playerEntry.score}");
            }
        }
        else
        {
            TestFail("Failed to submit score");
        }
    }

    private async Task TestLocalLeaderboardAccess()
    {
        LogStep("Testing local leaderboard access");
        
        if (string.IsNullOrEmpty(createdTournamentId))
        {
            TestSkip("No tournament available");
            return;
        }
        
        var leaderboard = TournamentManager.Instance.GetLocalLeaderboard(createdTournamentId);
        
        TestPass("Local leaderboard retrieved");
        LogInfo($"Leaderboard entries: {leaderboard.Count}");
        
        if (leaderboard.Count > 0)
        {
            TestCheck("Leaderboard has entries", true);
            var topEntry = leaderboard[0];
            LogInfo($"Top score: {topEntry.score} by {topEntry.playerName} (Rank #{topEntry.rank})");
            
            // Verify ranks are sequential
            bool ranksCorrect = true;
            for (int i = 0; i < leaderboard.Count; i++)
            {
                if (leaderboard[i].rank != i + 1)
                {
                    ranksCorrect = false;
                    break;
                }
            }
            TestCheck("Ranks are correctly assigned", ranksCorrect);
        }
        
        await Task.Yield();
    }

    private async Task TestPlayerRanking()
    {
        LogStep("Testing player ranking");
        
        if (string.IsNullOrEmpty(createdTournamentId))
        {
            TestSkip("No tournament available");
            return;
        }
        
        string playerId = AuthenticationService.Instance.PlayerId;
        int rank = TournamentManager.Instance.GetPlayerRank(createdTournamentId, playerId);
        int score = TournamentManager.Instance.GetPlayerScore(createdTournamentId, playerId);
        
        if (rank > 0)
        {
            TestPass("Player rank retrieved");
            LogInfo($"Player rank: #{rank}");
            LogInfo($"Player score: {score}");
            
            var playerEntry = TournamentManager.Instance.GetPlayerEntry(createdTournamentId, playerId);
            TestCheck("Player entry matches rank", playerEntry != null && playerEntry.rank == rank);
            TestCheck("Player entry matches score", playerEntry != null && playerEntry.score == score);
        }
        else
        {
            TestWarn("Player has no rank (hasn't played)");
        }
        
        await Task.Yield();
    }

    private async Task TestTopPlayers()
    {
        LogStep("Testing top players retrieval");
        
        if (string.IsNullOrEmpty(createdTournamentId))
        {
            TestSkip("No tournament available");
            return;
        }
        
        var topPlayers = TournamentManager.Instance.GetTopPlayers(createdTournamentId, 5);
        
        TestPass($"Retrieved top {topPlayers.Count} players");
        
        // Verify they're sorted by score
        if (topPlayers.Count > 1)
        {
            bool sorted = true;
            for (int i = 1; i < topPlayers.Count; i++)
            {
                if (topPlayers[i].score > topPlayers[i - 1].score)
                {
                    sorted = false;
                    break;
                }
            }
            TestCheck("Top players correctly sorted by score", sorted);
        }
        
        await Task.Yield();
    }

    private async Task TestHasPlayerPlayed()
    {
        LogStep("Testing HasPlayerPlayed method");
        
        if (string.IsNullOrEmpty(createdTournamentId))
        {
            TestSkip("No tournament available");
            return;
        }
        
        string playerId = AuthenticationService.Instance.PlayerId;
        var tournament = TournamentManager.Instance.GetTournament(createdTournamentId);
        
        bool hasPlayed = TournamentManager.Instance.HasPlayerPlayed(createdTournamentId, playerId);
        bool inList = tournament.playersWhoPlayed.Contains(playerId);
        
        TestCheck("HasPlayerPlayed matches list", hasPlayed == inList);
        LogInfo($"Player has played: {hasPlayed}");
        
        await Task.Yield();
    }

    private async Task TestParticipationStats()
    {
        LogStep("Testing participation statistics");
        
        if (string.IsNullOrEmpty(createdTournamentId))
        {
            TestSkip("No tournament available");
            return;
        }
        
        var stats = TournamentManager.Instance.GetParticipationStats(createdTournamentId);
        
        TestPass("Participation stats retrieved");
        LogInfo($"Joined: {stats.joined}");
        LogInfo($"Played: {stats.played}");
        LogInfo($"Not Played: {stats.notPlayed}");
        
        TestCheck("Stats math correct", stats.joined == stats.played + stats.notPlayed);
        
        await Task.Yield();
    }

    private async Task TestPlayersWhoHaventPlayed()
    {
        LogStep("Testing GetPlayersWhoHaventPlayed");
        
        if (string.IsNullOrEmpty(createdTournamentId))
        {
            TestSkip("No tournament available");
            return;
        }
        
        var notPlayed = TournamentManager.Instance.GetPlayersWhoHaventPlayed(createdTournamentId);
        var played = TournamentManager.Instance.GetPlayersWhoPlayed(createdTournamentId);
        
        TestPass("Retrieved players who haven't played");
        LogInfo($"Haven't played: {notPlayed.Count}");
        LogInfo($"Have played: {played.Count}");
        
        string currentPlayer = AuthenticationService.Instance.PlayerId;
        bool currentHasPlayed = played.Contains(currentPlayer);
        bool currentHasntPlayed = notPlayed.Contains(currentPlayer);
        
        TestCheck("Player in exactly one list", currentHasPlayed != currentHasntPlayed);
        
        await Task.Yield();
    }

    private async Task TestDuplicateScoreSubmission()
    {
        LogStep("Testing duplicate score submission prevention");
        
        if (string.IsNullOrEmpty(createdTournamentId))
        {
            TestSkip("No tournament available");
            return;
        }
        
        string playerId = AuthenticationService.Instance.PlayerId;
        bool hasPlayed = TournamentManager.Instance.HasPlayerPlayed(createdTournamentId, playerId);
        
        if (!hasPlayed)
        {
            TestSkip("Player hasn't played yet - submit score first");
            return;
        }
        
        // Try to submit again - should fail
        bool success = await TournamentManager.Instance.SubmitScore(
            createdTournamentId, 
            testScore + 1000,
            testPlayerName
        );
        
        TestCheck("Duplicate submission prevented", !success);
        
        // Verify player still only in list once
        var tournament = TournamentManager.Instance.GetTournament(createdTournamentId);
        int count = tournament.playersWhoPlayed.FindAll(p => p == playerId).Count;
        TestCheck("Player listed only once in playersWhoPlayed", count == 1);
        
        int leaderboardCount = tournament.localLeaderboard.FindAll(e => e.playerId == playerId).Count;
        TestCheck("Player has only one leaderboard entry", leaderboardCount == 1);
        
        await Task.Yield();
    }

    private async Task TestPlayerJoinLeave()
    {
        LogStep("Testing join/leave operations");
        
        if (!TournamentManager.Instance.IsAdmin())
        {
            TestSkip("Not an admin");
            return;
        }
        
        DateTime startTimeIST = TournamentManager.GetCurrentISTTime().AddMinutes(5);
        DateTime endTimeIST = startTimeIST.AddMinutes(30);
        
        bool created = await TournamentManager.Instance.CreateTournament(
            "Join-Leave Test",
            startTimeIST,
            endTimeIST,
            false,
            "",
            10
        );
        
        if (!created)
        {
            TestFail("Could not create test tournament");
            return;
        }
        
        var tournaments = TournamentManager.Instance.GetActiveTournaments();
        var testTournament = tournaments.Find(t => t.tournamentName == "Join-Leave Test");
        
        if (testTournament != null)
        {
            bool joined = await TournamentManager.Instance.JoinTournament(testTournament.tournamentId);
            TestCheck("Can join tournament", joined);
            
            bool left = await TournamentManager.Instance.LeaveTournament(testTournament.tournamentId);
            TestCheck("Can leave tournament", left);
            
            await TournamentManager.Instance.DeleteTournament(testTournament.tournamentId);
        }
    }

    private async Task TestPasswordProtection()
    {
        LogStep("Testing password protection");
        
        if (!TournamentManager.Instance.IsAdmin())
        {
            TestSkip("Not an admin");
            return;
        }
        
        DateTime startTimeIST = TournamentManager.GetCurrentISTTime().AddMinutes(5);
        DateTime endTimeIST = startTimeIST.AddMinutes(30);
        
        bool created = await TournamentManager.Instance.CreateTournament(
            "Password Test",
            startTimeIST,
            endTimeIST,
            true,
            "secret123",
            10
        );
        
        if (created)
        {
            var tournaments = TournamentManager.Instance.GetActiveTournaments();
            var privateTournament = tournaments.Find(t => t.tournamentName == "Password Test");
            
            if (privateTournament != null)
            {
                bool joinedWrong = await TournamentManager.Instance.JoinTournament(
                    privateTournament.tournamentId, "wrong");
                TestCheck("Wrong password rejected", !joinedWrong);
                
                bool joinedCorrect = await TournamentManager.Instance.JoinTournament(
                    privateTournament.tournamentId, "secret123");
                TestCheck("Correct password accepted", joinedCorrect);
                
                await TournamentManager.Instance.DeleteTournament(privateTournament.tournamentId);
            }
        }
    }

    private async Task TestMaxPlayersLimit()
    {
        LogStep("Testing max players limit");
        
        var tournaments = TournamentManager.Instance.GetActiveTournaments();
        
        if (tournaments.Count > 0)
        {
            var tournament = tournaments[0];
            LogInfo($"Tournament: {tournament.tournamentName}");
            LogInfo($"Players: {tournament.participantUIDs.Count}/{tournament.maxPlayers}");
            TestCheck("Max players limit enforced", 
                tournament.participantUIDs.Count <= tournament.maxPlayers);
        }
        else
        {
            TestSkip("No tournaments to test");
        }
        
        await Task.Yield();
    }

    private async Task TestCreatorPermissions()
    {
        LogStep("Testing creator permissions");
        
        if (string.IsNullOrEmpty(createdTournamentId))
        {
            TestSkip("No tournament available");
            return;
        }
        
        bool isCreator = TournamentManager.Instance.IsCreatorOfTournament(createdTournamentId);
        LogInfo($"Is creator of test tournament: {isCreator}");
        
        var myCreatedTournaments = TournamentManager.Instance.GetMyCreatedTournaments();
        TestCheck("GetMyCreatedTournaments works", myCreatedTournaments != null);
        LogInfo($"My created tournaments: {myCreatedTournaments.Count}");
        
        await Task.Yield();
    }

    private async Task TestInvalidOperations()
    {
        LogStep("Testing invalid operations");
        
        var endedTournaments = TournamentManager.Instance.GetTournamentsByStatus(TournamentStatus.Ended);
        
        if (endedTournaments.Count > 0)
        {
            bool joined = await TournamentManager.Instance.JoinTournament(endedTournaments[0].tournamentId);
            TestCheck("Cannot join ended tournament", !joined);
        }
        
        var upcomingTournaments = TournamentManager.Instance.GetTournamentsByStatus(TournamentStatus.Upcoming);
        
        if (upcomingTournaments.Count > 0)
        {
            bool canPlay = TournamentManager.Instance.CanPlayerPlayTournament(
                upcomingTournaments[0].tournamentId);
            TestCheck("Cannot play upcoming tournament", !canPlay);
        }
        
        await Task.Yield();
    }

    private async Task TestTournamentEnd()
    {
        LogStep("Testing tournament end");
        
        if (string.IsNullOrEmpty(createdTournamentId))
        {
            TestSkip("No tournament to end");
            return;
        }
        
        if (!TournamentManager.Instance.IsCreatorOfTournament(createdTournamentId))
        {
            TestSkip("Not the creator");
            return;
        }
        
        bool success = await TournamentManager.Instance.EndTournament(createdTournamentId);
        
        if (success)
        {
            var tournament = TournamentManager.Instance.GetTournament(createdTournamentId);
            TestPass("Tournament ended");
            TestCheck("Status changed to Ended", tournament.status == TournamentStatus.Ended);
        }
        else
        {
            TestFail("Failed to end tournament");
        }
    }

    private async Task TestTournamentDeletion()
    {
        LogStep("Testing tournament deletion");
        
        if (string.IsNullOrEmpty(createdTournamentId))
        {
            TestSkip("No tournament to delete");
            return;
        }
        
        if (!TournamentManager.Instance.IsCreatorOfTournament(createdTournamentId))
        {
            TestSkip("Not the creator");
            return;
        }
        
        bool success = await TournamentManager.Instance.DeleteTournament(createdTournamentId);
        
        if (success)
        {
            var tournament = TournamentManager.Instance.GetTournament(createdTournamentId);
            TestPass("Tournament deleted");
            TestCheck("Tournament removed from list", tournament == null);
            createdTournamentId = null;
        }
        else
        {
            TestFail("Failed to delete tournament");
        }
    }

    // ==================== HELPER METHODS ====================
    
    private void TestCheck(string description, bool condition)
    {
        if (condition) TestPass(description);
        else TestFail(description);
    }

    private void TestPass(string message)
    {
        testsPassed++;
        Debug.Log($"[DEBUGGER] <color=green>✓ PASS</color> {message}");
    }
    
    private void TestWarn(string message)
    {
        Debug.LogWarning($"[DEBUGGER] <color=yellow>⚠ WARN</color> {message}");
    }
    
    private void TestFail(string message)
    {
        testsFailed++;
        Debug.LogError($"[DEBUGGER] <color=red>✗ FAIL</color> {message}");
    }

    private void TestSkip(string message)
    {
        testsSkipped++;
        Debug.Log($"[DEBUGGER] <color=yellow>⊝ SKIP</color> {message}");
    }

    private void LogStep(string message)
    {
        Debug.Log($"[DEBUGGER] \n┌─── {message}");
    }

    private void LogSection(string message)
    {
        Debug.Log($"[DEBUGGER] \n╔═══════════════════════════════════════╗\n║  {message}\n╚═══════════════════════════════════════╝");
    }

    private void LogHeader(string message)
    {
        Debug.Log($"[DEBUGGER] \n╔═══════════════════════════════════════════════════╗\n║  {message}\n╚═══════════════════════════════════════════════════╝");
    }

    private void LogInfo(string message)
    {
        Debug.Log($"[DEBUGGER]   ℹ {message}");
    }

    private void LogWarn(string message)
    {
        Debug.LogWarning($"[DEBUGGER] <color=yellow>  ⚠ {message}</color>");
    }

    private void LogSuccess(string message)
    {
        Debug.Log($"[DEBUGGER] <color=green>  ✓ {message}</color>");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[DEBUGGER] <color=red>  ✗ {message}</color>");
    }

    private void PrintStats()
    {
        Debug.Log($"[DEBUGGER] Passed: {testsPassed} | Failed: {testsFailed} | Skipped: {testsSkipped}");
    }

    private void PrintFinalReport()
    {
        LogSection("FINAL REPORT");
        LogInfo($"Tests Passed:  {testsPassed}");
        LogInfo($"Tests Failed:  {testsFailed}");
        LogInfo($"Tests Skipped: {testsSkipped}");
        LogInfo($"Total Tests:   {testsPassed + testsFailed + testsSkipped}");
        
        float passRate = testsPassed + testsFailed > 0 
            ? (float)testsPassed / (testsPassed + testsFailed) * 100 
            : 0;
        
        LogInfo($"Pass Rate:     {passRate:F1}%");
        
        if (testsFailed == 0 && testsPassed > 0)
        {
            LogSuccess("ALL TESTS PASSED!");
        }
        else if (testsFailed > 0)
        {
            LogError($"{testsFailed} TESTS FAILED");
        }
    }

    private void ResetStats()
    {
        testsPassed = 0;
        testsFailed = 0;
        testsSkipped = 0;
    }

    private async Task Delay()
    {
        await Task.Delay((int)(delayBetweenSteps * 1000));
    }
}
