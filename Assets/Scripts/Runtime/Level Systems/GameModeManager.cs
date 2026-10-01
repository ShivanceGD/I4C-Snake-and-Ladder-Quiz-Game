using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance;
    public int NumberOfPlayersToBeSpawned = 1;
    public QuizPackSO QuizPack;
    public bool UseOnlineQuizPack;
    public string SelectedOnlineQuizPackId;
    public string SelectedOnlineQuizPackName;
    public string SelectedOnlineQuizCategoryName;
    public QuizPackSO SelectedRuntimeQuizPack;
    public QuizPackSO SelectedLocalQuizPack;
    public LevelDataSO level;
    public bool IsTournamentMode { get; set; } = false;
    public string CurrentTournamentId { get; set; } = "";
    [Header("All Levels (assign in inspector)")]
    public List<LevelDataSO> AllLevels;

    [Header("Unlock System")] public Dictionary<int, int> CachedLevelStarsData;
    public List<int> LevelsToBeUnlocked;
    public List<string> TournamentAdminsUID;

    [Header("Game Mode Buttons")]
    [SerializeField] private GameObject StoryMode;
    [SerializeField] private GameObject Multiplayer;

    public Button Tournament;
    public Button Test;

    public bool IsPrivateRoom;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ResetTournamentMode()
    {
        IsTournamentMode = false;
    }

    public void SetSelectedOnlineQuizPackEntry(QuizPackManifestEntryDTO entry)
    {
        if (entry == null) return;

        UseOnlineQuizPack = true;
        SelectedOnlineQuizPackId = entry.packId;
        SelectedOnlineQuizPackName = entry.displayName;
        SelectedOnlineQuizCategoryName = entry.categoryName;
        SelectedLocalQuizPack = null;
        OnlineQuizEvents.QuizPackSelected(entry);
        OnlineQuizEvents.OnlineQuizModeChanged(true);
    }

    public void SetSelectedRuntimeOnlineQuizPack(string packId, QuizPackSO runtimePack)
    {
        UseOnlineQuizPack = true;
        SelectedOnlineQuizPackId = packId;
        SelectedRuntimeQuizPack = runtimePack;
        SelectedLocalQuizPack = null;
        QuizPack = runtimePack;
        OnlineQuizEvents.OnlineQuizModeChanged(true);
    }

    public void SetSelectedLocalQuizPack(QuizPackSO localPack)
    {
        UseOnlineQuizPack = false;
        SelectedOnlineQuizPackId = string.Empty;
        SelectedOnlineQuizPackName = string.Empty;
        SelectedOnlineQuizCategoryName = string.Empty;
        SelectedRuntimeQuizPack = null;
        SelectedLocalQuizPack = localPack;
        QuizPack = localPack;
        OnlineQuizEvents.OnlineQuizModeChanged(false);
    }

    public void ClearQuizPackSelection()
    {
        UseOnlineQuizPack = false;
        SelectedOnlineQuizPackId = string.Empty;
        SelectedOnlineQuizPackName = string.Empty;
        SelectedOnlineQuizCategoryName = string.Empty;
        SelectedRuntimeQuizPack = null;
        SelectedLocalQuizPack = null;
        QuizPack = null;
        OnlineQuizEvents.OnlineQuizModeChanged(false);
    }

    private void Start()
    {
        MainMenuUI mainMenuUI = GameObject.FindFirstObjectByType<MainMenuUI>();
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            DisableGameModeButtons(mainMenuUI.StoryModeButton);
            DisableGameModeButtons(mainMenuUI.MultiplayerButton);
        }
    }

    private void DisableGameModeButtons(GameObject gameModeButton)
    {
        gameModeButton.GetComponent<Button>().interactable = false;
        gameModeButton.GetComponent<Image>().color = Color.grey;
        gameModeButton.GetComponentInChildren<Image>().color = Color.grey;
        gameModeButton.GetComponentInChildren<TMP_Text>().color = Color.black;
        gameModeButton.transform.Find("Lock_Image").gameObject.SetActive(true);
    }

    [ContextMenu("Generate All Offline Buttons")]
    public async void GenerateAllOfflineLevels()
    {
        LevelsToBeUnlocked = await RemoteConfigLoadManager.Instance.GetDefaultUnlockedLevels();
        UiLogicManager.Instance.RemoveAllChildInsideParent(UIManager.Instance.OfflineLevelsButtonParentTransform);
        CachedLevelStarsData = await SaveAndLoadManager.Instance.ReturnLevelAndStars();

        int lastUnlockableLevelIndex = -1;

        for (int i = 0; i < AllLevels.Count; i++)
        {
            if (AllLevels[i].GameMode != GameMode.StoryMode)
                continue;

            if (!AllLevels[i].IsUnlockable)
            {
                UiLogicManager.Instance.GenerateOfflineLevelButton(
                    AllLevels[i],
                    UiLogicManager.Instance.ComingSoonPanel,
                    UIManager.Instance.OfflineLevelsButtonParentTransform,
                    false
                );
                continue;
            }

            bool isUnlocked = false;

            if (i == 0)
            {
                isUnlocked = true;
            }
            else if (LevelsToBeUnlocked != null && LevelsToBeUnlocked.Contains(AllLevels[i].LevelNumber))
            {
                isUnlocked = true;
            }
            else
            {
                int prevLevelIndex = lastUnlockableLevelIndex >= 0 ? lastUnlockableLevelIndex : i - 1;
                int prevLevelStars = SaveAndLoadManager.Instance.GetSavedStarsForLevel(
                    CachedLevelStarsData,
                    AllLevels[prevLevelIndex].LevelNumber
                );

                isUnlocked = UiLogicManager.Instance.CheckIfLevelIsCompleted(
                    AllLevels[i].StarsToUnlockLevel, prevLevelStars);
            }

            UiLogicManager.Instance.GenerateOfflineLevelButton(
                AllLevels[i],
                UiLogicManager.Instance.StoryModeButton,
                UIManager.Instance.OfflineLevelsButtonParentTransform,
                isUnlocked
            );

            lastUnlockableLevelIndex = i;
        }
    }

    [ContextMenu("Create Tournament Button")]
    public async void SetTournamentAdmins()
    {
        TournamentAdminsUID = await RemoteConfigLoadManager.Instance.GetTournamentAdmins();
        if (TournamentAdminsUID.Contains(AuthenticationService.Instance.PlayerId))
        {
            Tournament.interactable = true;
            Tournament.onClick.AddListener(() => Debug.Log("Create Button Clicked"));
        }
    }

    [ContextMenu("SaveGameData")]
    public void SaveGameData()
    {
        SaveAndLoadManager.Instance.SavePlayerCommonData(AuthExtensions.GetCachedPlayerName(), 5);
        SaveAndLoadManager.Instance.SaveLevelData(AllLevels);
    }
}
