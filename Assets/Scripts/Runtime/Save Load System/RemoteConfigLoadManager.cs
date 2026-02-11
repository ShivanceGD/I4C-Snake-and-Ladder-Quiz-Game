using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.RemoteConfig;
using UnityEngine;

public class RemoteConfigLoadManager : MonoBehaviour
{
    public static RemoteConfigLoadManager Instance;
    [Header("Remote Config Key")]
    public string DefaultUnlockedLevelKey = "LevelsToBeUnlocked";

    public string TournamentAdminsKey = "TournamentHostsId";

    
    //public List<int> unlockedLevels;
    //public struct userAttribute { }
    //public struct appAttribute { }

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

    }
    /*[ContextMenu("Load Remote Config")]
    public void OnFetched()
    {
        
        unlockedLevels = GetDefaultUnlockedLevels();
        Debug.Log("Unlocked Levels: " + string.Join(",", unlockedLevels));
    }*/

    public Task<List<int>> GetDefaultUnlockedLevels()
    {
        string result = RemoteConfigService.Instance.appConfig.GetJson(DefaultUnlockedLevelKey);

        if (!string.IsNullOrEmpty(result))
        {
            // Deserialize JSON into wrapper
            LevelsToBeUnlockedConfigWrapper wrapper = JsonUtility.FromJson<LevelsToBeUnlockedConfigWrapper>(result);
            return Task.FromResult(wrapper.levelNumbersToUnlock);
        }

        return Task.FromResult(new List<int>()); // return empty if not found
    }

    public Task<List<string>> GetTournamentAdmins()
    {
        string result = RemoteConfigService.Instance.appConfig.GetJson(TournamentAdminsKey);
        Debug.Log(result);
        if (!string.IsNullOrEmpty(result))
        {
            TorunamentAdminsConfigWrapper wrapper = JsonUtility.FromJson<TorunamentAdminsConfigWrapper>(result);
            Debug.Log(wrapper.TournamentHostsId.Count);
            return Task.FromResult(wrapper.TournamentHostsId);
        }
        Debug.Log("EmptyList");
        return Task.FromResult(new List<string>());
    }
}

[Serializable]
public class TorunamentAdminsConfigWrapper
{
    public List<string> TournamentHostsId = new List<string>();
}
[Serializable]
public class LevelsToBeUnlockedConfigWrapper
{
    public List<int> levelNumbersToUnlock = new List<int>();
}

