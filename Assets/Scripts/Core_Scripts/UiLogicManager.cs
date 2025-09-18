using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class UiLogicManager : MonoBehaviour
{
    public static UiLogicManager Instance { get; private set; }
    


    private void Start()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        GameModeManager.Instance.GenerateAllOfflineLevels();
    }

    public void RemoveAllChildInsideParent(Transform parent)
    {
        if (parent.childCount <= 0) return;
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }
    public void GenerateOfflineLevelButton(LevelDataSO OfflineLevel,Action OnClickAction,Transform ButtonParent,bool IsUnlocked)
    {
        if (IsUnlocked)
        {
            Button button = Instantiate(OfflineLevel.LevelUIElements.LevelUnlockedButtonPrefab,ButtonParent);
            button.gameObject.GetComponentInChildren<TMP_Text>().text = OfflineLevel.LevelNumber.ToString();
            button.interactable = true;
            button.onClick.AddListener(() =>
            {
                OnClickAction?.Invoke();
                SetLevelData(OfflineLevel);
                
            });
            button.gameObject.GetComponent<LevelDataHolder>().levelData = OfflineLevel;
           
        }
        else
        {
            Button button = Instantiate(OfflineLevel.LevelUIElements.LevelLockedButtonPrefab,ButtonParent);
            button.gameObject.GetComponentInChildren<TMP_Text>().text = OfflineLevel.LevelNumber.ToString();
            button.interactable = true; // Level is Locked Panel in UI Manager
            button.onClick.AddListener(LockedButtonPanel);
        }

    }

    public bool CheckIfLevelIsCompleted(int LevelStarsToUnlock,int Stars)
    {
        
        if (Stars >= LevelStarsToUnlock)
        {
            return true;
        }
        else
        {
           return false;
        }
    }

    public void ComingSoonPanel()
    {
        UIManager.Instance.ComingSoon.SetActive(true);
    }

    private void LockedButtonPanel()
    {
        UIManager.Instance.LockedLevel.SetActive(true);
    }

    public void StoryModeButton()
    {
        /*GameModeManager.Instance.NumberOfPlayersToBeSpawned = 1;
        LoadingSceneManager.Instance.LoadofflineScene("Offline_Practice_Level");
        LoadingSceneManager.Instance.SetLoadingScreenMessage("Loading...");
        UIManager.Instance.offlineFlowManager.CurrentLevelData = OfflineLevel;*/
        Debug.Log("Clicked");
        LoadingSceneManager.Instance.LoadofflineScene("Offline_Practice_Level");
        GameModeManager.Instance.NumberOfPlayersToBeSpawned = 1;
        LoadingSceneManager.Instance.SetLoadingScreenMessage("Loading...");
       
    }

    private void SetLevelData(LevelDataSO levelData)
    {
       GameModeManager.Instance.level = levelData;
       GameModeManager.Instance.QuizPack = levelData.LevelQuizSCO;
    }
}
/*using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiLogicManager : MonoBehaviour
{
    public static UiLogicManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    /// <summary>
    /// Creates a level button depending on whether the level is unlocked.
    /// </summary>
    public void GenerateOfflineLevelButton(LevelDataSO level, Action onClickAction, Transform buttonParent)
    {
        Button button;

        if (level.isLevelUnlocked)
        {
            button = Instantiate(level.LevelUIElements.LevelUnlockedButtonPrefab, buttonParent);
            button.interactable = true;
            button.onClick.AddListener(() => onClickAction?.Invoke());
        }
        else
        {
            button = Instantiate(level.LevelUIElements.LevelLockedButtonPrefab, buttonParent);
            button.interactable = false;
        }

        button.GetComponentInChildren<TMP_Text>().text = level.LevelNumber.ToString();
    }
}*/