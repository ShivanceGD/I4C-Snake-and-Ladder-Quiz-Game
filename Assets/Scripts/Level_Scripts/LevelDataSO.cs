using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "New Level", menuName = "Shivance Games/Create New Level")]
public class LevelDataSO : ScriptableObject
{
    [Header("Level Information")]
    public int LevelNumber;
    public string LevelName;
    
    [Header("Level Settings")] 
    public int TotalAvailableMoves;
    public GameMode GameMode; 
    public int LevelStars;
    [Range(0,3)]public int StarsToUnlockLevel;
    
    [Header("Quiz Settings")]
    public QuizScriptableObject LevelQuizSCO;
    public List<Difficulty> NoHintDifficulty;
    public int OptionsToRemoveOnHint;

    [Header("Board")]
    public BoardScriptableObect Board;

    [Header("Player And CPU Prefabs")] 
    public GameObject PlayerPrefab;
    public GameObject CPUPrefab;
    
    [Header("Step Range for Difficulty")]
    public DifficultyStepRange DiceRollRangePerQuizDifficulty;
    
    [Header("Level UI Elements")]
    public LevelUIElements LevelUIElements;
}

[Serializable]
public class LevelUIElements
{
    [Header("Level Button Elements")]
    public Button LevelLockedButtonPrefab;
    public Button LevelUnlockedButtonPrefab;
}

[Serializable]
public enum GameMode
{
    Default,
    SinglePlayer,
    MultiPlayer
}
[Serializable]
public class DifficultyStepRange : SerializedDictionary<Difficulty, Vector2Int> {}