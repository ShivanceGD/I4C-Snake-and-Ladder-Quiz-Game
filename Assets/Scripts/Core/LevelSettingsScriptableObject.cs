using AYellowpaper.SerializedCollections;
using UnityEngine;

[System.Serializable]
public class DifficultyStepRange : SerializedDictionary<Difficulty, Vector2Int> {}

[CreateAssetMenu(fileName = "NewLevelSettings", menuName = "Level/Create Level Settings")]
public class LevelSettingsScriptableObject : ScriptableObject
{
    public int LevelNumber;
    public string LevelName;
    public int LevelStars;
    public int LevelPoints;
    public bool isLevelUnlocked;
    public int RequiredPointsToUnlockLevel;
    public bool isLevelCompleted;

    public QuizScriptableObject LevelQuizSCO;
    public BoardScriptableObect BoardJointsSCO;

    [Header("Step Range for Difficulty")]
    public DifficultyStepRange DiceRollRangePerQuizDifficulty;
}
