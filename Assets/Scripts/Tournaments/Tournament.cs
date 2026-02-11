using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TournamentData
{
    public string tournamentId;
    public string tournamentName;
    public string startTimeString;
    public string endTimeString;
    public bool isPrivate;
    public string password;
    public string creatorUID;
    public int maxPlayers;
    public TournamentStatus status;
    public List<string> participantUIDs = new List<string>();
    public List<string> playersWhoPlayed = new List<string>();
    
    // LOCAL LEADERBOARD - Saved with tournament
    public List<CustomLeaderboardEntry> localLeaderboard = new List<CustomLeaderboardEntry>();
    
    public string selectedQuizPackName = "";
    
    // Helper properties for DateTime access
    public DateTime startTime
    {
        get => string.IsNullOrEmpty(startTimeString) ? DateTime.UtcNow : DateTime.Parse(startTimeString);
        set => startTimeString = value.ToString("o");
    }

    public DateTime endTime
    {
        get => string.IsNullOrEmpty(endTimeString) ? DateTime.UtcNow.AddHours(1) : DateTime.Parse(endTimeString);
        set => endTimeString = value.ToString("o");
    }

    public TournamentData()
    {
        tournamentId = Guid.NewGuid().ToString();
        status = TournamentStatus.Upcoming;
        playersWhoPlayed = new List<string>();
        localLeaderboard = new List<CustomLeaderboardEntry>();
    }
}

[Serializable]
public enum TournamentStatus
{
    Upcoming,    // 0
    Active,      // 1
    Ended        // 2
}

// Local Leaderboard Entry
[Serializable]
public class CustomLeaderboardEntry
{
    public string playerId;
    public string playerName;
    public int score;
    public string timestamp;
    public int rank; // Changed to lowercase to match C# naming conventions
    
    public CustomLeaderboardEntry(string id, string name, int scoreValue)
    {
        playerId = id;
        playerName = name;
        score = scoreValue;
        timestamp = DateTime.UtcNow.ToString("o");
        rank = 0; // Will be set when leaderboard is sorted
    }
}
