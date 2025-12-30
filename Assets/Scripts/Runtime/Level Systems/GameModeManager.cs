using System;
using System.Collections.Generic;
using System.Net;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.UI;

public class GameModeManager : MonoBehaviour
{
       public static GameModeManager Instance;
       public int NumberOfPlayersToBeSpawned = 1;
       public QuizPackSO QuizPack;
       public LevelDataSO level;

       [Header("All Levels (assign in inspector)")]
       public List<LevelDataSO> AllLevels;

       [Header("Unlock System")] public Dictionary<int, int> CachedLevelStarsData;
       public List<int> LevelsToBeUnlocked;
       public List<string> TournamentAdminsUID;

       [Header("Game Mode Buttons")] [SerializeField]
       private GameObject StoryMode;

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

       /*public void ChooseNumberOfPlayersForPassNPlayMode(int NumberOfPlayers)
       {
              NumberOfPlayersToBeSpawned = NumberOfPlayers;
              Debug.Log($"[GameModeManager] NumberOfPlayers set to {NumberOfPlayers}");
       }

       public void ChooseModusOperandiToOfflineLevel(QuizPackSO quizPack)
       {
              QuizPack = quizPack;
              Debug.Log($"[GameModeManager] QuizPack set to {quizPack?.name}");
       }*/

       [ContextMenu("Generate All Offline Buttons")]
       public async void GenerateAllOfflineLevels()
       {
             

              // Load unlocked levels
                     LevelsToBeUnlocked = await RemoteConfigLoadManager.Instance.GetDefaultUnlockedLevels();

                     // Clear old buttons
                     UiLogicManager.Instance.RemoveAllChildInsideParent(UIManager.Instance.OfflineLevelsButtonParentTransform);

                     // Load cached stars
                     CachedLevelStarsData = await SaveAndLoadManager.Instance.ReturnLevelAndStars();

                     // Iterate levels
                     for (int i = 0; i < AllLevels.Count; i++)
                     {
                            if (AllLevels[i].GameMode != GameMode.StoryMode)
                                   continue;
                            if (AllLevels[i].IsUnlockable==false)
                            {
                                   Debug.Log("Coming Soon Button");
                                   UiLogicManager.Instance.GenerateOfflineLevelButton(
                                          AllLevels[i],
                                          //GameManager.Instance.LoadSinglePlayerLevel, // uses GameManager’s method
                                          UiLogicManager.Instance.ComingSoonPanel,
                                          UIManager.Instance.OfflineLevelsButtonParentTransform,
                                          false
                                   );
                                   continue;
                            }

                            bool isUnlocked = false;

                            // Rule 1: Always unlock first level
                            if (i == 0)
                            {
                                   isUnlocked = true;
                            }
                            // Rule 2: Check RemoteConfig
                            else if (LevelsToBeUnlocked != null)
                            {
                                   if (LevelsToBeUnlocked != null && LevelsToBeUnlocked.Contains(AllLevels[i].LevelNumber))
                                   {
                                          Debug.Log($"[Unlock] Level {AllLevels[i].LevelNumber} unlocked via RemoteConfig ✅");
                                          isUnlocked = true;
                                   }
                                   else
                                   {
                                          Debug.Log($"[Lock] Level {AllLevels[i].LevelNumber} not in RemoteConfig. Checking stars...");
                                          int prevLevelStars = SaveAndLoadManager.Instance.GetSavedStarsForLevel(
                                                 CachedLevelStarsData,
                                                 AllLevels[i - 1].LevelNumber
                                          );

                                          Debug.Log($"[Stars] Previous level {AllLevels[i - 1].LevelNumber} has {prevLevelStars} stars. Needs {AllLevels[i].StarsToUnlockLevel}.");
                                          isUnlocked = UiLogicManager.Instance.CheckIfLevelIsCompleted(AllLevels[i].StarsToUnlockLevel, prevLevelStars);
                                   }
                            }
                            
                            // Rule 3: Check star requirement
                            else
                            {
                                   int prevLevelStars = SaveAndLoadManager.Instance.GetSavedStarsForLevel(
                                          CachedLevelStarsData,
                                          AllLevels[i - 1].LevelNumber
                                   );

                                   isUnlocked =
                                          UiLogicManager.Instance.CheckIfLevelIsCompleted(
                                                 AllLevels[i].StarsToUnlockLevel, prevLevelStars);
                            }

                            // ✅ Generate button
                            UiLogicManager.Instance.GenerateOfflineLevelButton(
                                   AllLevels[i],
                                   //GameManager.Instance.LoadSinglePlayerLevel, // uses GameManager’s method
                                   UiLogicManager.Instance.StoryModeButton,
                                   UIManager.Instance.OfflineLevelsButtonParentTransform,
                                   isUnlocked
                            );
                     }

              }
       
[ContextMenu("Create Tournament Button")]
       public async void SetTournamentAdmins()
       {
              TournamentAdminsUID = await RemoteConfigLoadManager.Instance.GetTournamentAdmins();
              if (TournamentAdminsUID.Contains(AuthenticationService.Instance.PlayerId))
              {
                     Tournament.interactable = true;
                     Tournament.onClick.AddListener(()=>Debug.Log("Create Button Clicked"));
              }
       }

              [ContextMenu("SaveGameData")]
              public void SaveGameData()
              {
                     /*UnityServices.InitializeAsync();
                     AuthExtensions.SignInAnonymouslyAsync();*/
                     SaveAndLoadManager.Instance.SavePlayerCommonData(AuthExtensions.GetCachedPlayerName(), 5);
                     SaveAndLoadManager.Instance.SaveLevelData(AllLevels);
              }
       }

