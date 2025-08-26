using System;
using System.Collections.Generic;

[Serializable]
public class PlayerCommonData
{
    public string PlayerName;
    public int PlayerHints;
}
[Serializable]
public class SinglePlayerData
{
    
}
[Serializable]
public class MultiPlayerData
{
    public int PlayerPoints;
}
[Serializable]
public class LevelData
{
    public List<LevelProgress> Levels = new List<LevelProgress>();
}
[Serializable]
public class LevelProgress
{
    public int LevelNumber;
    public int LevelStars;
}
