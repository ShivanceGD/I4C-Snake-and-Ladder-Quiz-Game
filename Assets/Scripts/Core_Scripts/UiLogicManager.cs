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
        RemoveAllChildInsideParent(UIManager.Instance.OfflineLevelsButtonParentTransform);
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
            if (OfflineLevel.LevelStars > 0)
            {
                ShowLevelStars(button.gameObject.GetComponent<LevelDataHolder>());
            }
        }
        else
        {
            Button button = Instantiate(OfflineLevel.LevelUIElements.LevelLockedButtonPrefab,ButtonParent);
            button.gameObject.GetComponentInChildren<TMP_Text>().text = OfflineLevel.LevelNumber.ToString();
            button.interactable = true; // Level is Locked Panel in UI Manager
            button.onClick.AddListener(() => (OfflineLevel.IsUnlockable ? (Action)LockedButtonPanel : ComingSoonPanel)());
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

    private void ShowLevelStars(LevelDataHolder LevelDataHolder)
    {
        for (int i = 0; i < LevelDataHolder.levelData.LevelStars; i++)
        {
            LevelDataHolder.stars[i].gameObject.SetActive(true);
        }
    }
}
