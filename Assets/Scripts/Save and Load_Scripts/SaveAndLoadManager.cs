using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


public class SaveAndLoadManager : MonoBehaviour
{
     public static SaveAndLoadManager Instance{get; private set;}
    [SerializeField]private string PlayerCommonDataKey,SinglePlayerDataKey,MultiPlayerDataKey,LevelDataKey;

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

    public async void SavePlayerCommonData(string Name,int hints)
    {
        PlayerCommonData saveCommonPlayerData = new PlayerCommonData
        {
            PlayerName = Name,
            //PlayerName = "Krithik",
            PlayerHints = hints
        };
        await OfflineUGSSaveManager.SaveAsync(PlayerCommonDataKey,JsonUtility.ToJson(saveCommonPlayerData));
    }

    public async void SaveLevelData(List<LevelDataSO> LevelsSO)
    {
        LevelData saveData = new LevelData();
        
        foreach (var level in LevelsSO)
        {
            LevelProgress saveLevelData = new LevelProgress
            {
                LevelNumber = level.LevelNumber,
                LevelStars = level.LevelStars
            };
            saveData.Levels.Add(saveLevelData);
        }

        await OfflineUGSSaveManager.SaveAsync(LevelDataKey,JsonUtility.ToJson(saveData));
    }
    /*public async Task<LevelData> LoadLevelData()
    {
        var keys = new HashSet<string> { LevelDataKey };
        var result = await OfflineUGSSaveManager.LoadBatchAsync(keys);

// Get the JSON string back from result
        if (result.TryGetValue(LevelDataKey, out var item))
        {
            return item.value; // or item.GetAsString() depending on your API
        }
        return null;
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("[SaveSystem] No LevelData found, returning empty.");
            return new LevelData(); // empty container
        }
        return JsonUtility.FromJson<LevelData>(json);
    }*/
    public async Task<LevelData> LoadLevelData()
    {
        var keys = new HashSet<string> { LevelDataKey };
        var result = await OfflineUGSSaveManager.LoadBatchAsync(keys);

        if (result.TryGetValue(LevelDataKey, out var jsonObj))
        {
            string json = jsonObj as string; // convert from object to string
            if (!string.IsNullOrEmpty(json))
            {
                return JsonUtility.FromJson<LevelData>(json);
            }
        }
    
        Debug.LogWarning("[SaveSystem] No LevelData found, returning empty.");
        return new LevelData(); // fallback empty container
    }
    public async Task<Dictionary<int,int>> ReturnLevelAndStars()
    {
        LevelData savedData = await SaveAndLoadManager.Instance.LoadLevelData();

        Dictionary<int, int> levelStarsCache = new();
        if (savedData != null && savedData.Levels != null)
        {
            foreach (var level in savedData.Levels)
            {
                levelStarsCache[level.LevelNumber] = level.LevelStars;
            }
        }
        Debug.Log("[GameManager] Cached stars for " + levelStarsCache.Count + " levels.");
        return levelStarsCache;
       
    }
    public int GetSavedStarsForLevel(Dictionary<int,int>LevelStarsData, int levelNumber)
    {
        return LevelStarsData.TryGetValue(levelNumber, out int stars) ? stars : 0;
    }
}
