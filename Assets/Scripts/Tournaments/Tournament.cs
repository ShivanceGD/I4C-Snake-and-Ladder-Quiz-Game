using System;

[System.Serializable]
public class Tournament
{
    //public string tournamentId;
    public string tournamentname;
    public string leaderboardId;
    public string HostedBy;
    public bool isPrivate;
    public string AccessKey; // Only Required if The Tournament is Private
    public QuizPackSO quizPack;
    public int NumberOfTimesPlayerCanPlay;
    public DateTime StartTime;
    public DateTime EndTime;

    public Tournament(string name, string leaderboardis, string hostName, bool isprivate = false, string accessKey = null, int numberOfTimesPlayerCanPlay = 1, DateTime? startTime = null, DateTime? endTime = null, QuizPackSO quizpack = null)
    {
        tournamentname = name;
        leaderboardId = leaderboardis;
        HostedBy = hostName;
        isPrivate = isprivate;
        AccessKey = accessKey;
        NumberOfTimesPlayerCanPlay = numberOfTimesPlayerCanPlay;
        StartTime = startTime ?? DateTime.Now.AddMinutes(2);
        EndTime = endTime ?? StartTime.AddMinutes(30);
        quizPack = quizpack;
    }
}
