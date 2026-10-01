using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum GameModeUIManager
{
   None ,
   Practice ,
   PassNPlay 
}
public class MainMenuUI : MonoBehaviour
{
   [Header("References")]
   public Transform quizPackButtonParent;    // Where quiz pack buttons will spawn
   public GameObject quizPackButtonPrefab;   // A button prefab with TMP_Text + Button
   public QuizPackSO[] availableQuizPacks;   // Assign in inspector
   public OnlineQuizConfigSO onlineQuizConfig;
   public GameObject ChooseQuizPanel;
   public GameObject StoryModeButton;
   public GameObject MultiplayerButton;
   public Color LockColor;
   public TournamentManager tournamentManager;
   [Header("Scenes")]
   public string practiceSceneName = "PracticeScene";
   public string passNPlaySceneName = "PassNPlayScene";
   public string TournamentScene;

   private GameModeUIManager currentMode = GameModeUIManager.None;

   [Header("Leaderboard References")]
   public GameObject LeaderBoardItemPrefab;
   public Transform LeaderBoardItemParent;
   public GameObject CurrentPlayerLeaderBoardItem;
   // Called when Practice button is clicked
   
   [Header("Profile References")]
   public TMP_InputField username;
   public TMP_Text placeholderUsername;
   public TMP_Text id;
   public Button updateNameButton;
   [Header("HyperLinks")] 
   [SerializeField]private string InstagramHyperLink;
   [SerializeField]private string YoutubeHyperLink;
   [SerializeField]private string FaceBookHyperLink;
   [SerializeField]private string CyberCrimePortalHyperLink;

   
   

   private string currentSelectingTournamentId;
   private string currentPlayingTournamentId;
   private OnlineQuizRepository onlineQuizRepository;
   private CancellationTokenSource onlineQuizCts;
   
   public void Start()
   {
      onlineQuizRepository = new OnlineQuizRepository(onlineQuizConfig, availableQuizPacks);
      username.text = AuthExtensions.GetCachedPlayerName();
      placeholderUsername.text = AuthExtensions.GetCachedPlayerName();
      id.text = AuthExtensions.GetPlayerID();

      if (IsInternetReachable())
      {
         LockModes(StoryModeButton);
         LockModes(MultiplayerButton);
      }
     
   }

   private void OnDestroy()
   {
      ResetOnlineQuizRequest();
   }
   public void OnCreateTournamentMenuOpened()
   {
      //tournamentManager.GenerateQuizPackButtons(availableQuizPacks,quizPackButtonPrefab,quizPackButtonParent);
   }
   public void OnPracticeClicked()
   {
      currentMode = GameModeUIManager.Practice;
      GameModeManager.Instance.NumberOfPlayersToBeSpawned = 1;
      ShowQuizPackButtons();
   }
// Called by TournamentUI when admin clicks "Select Quiz Pack"
public void ShowTournamentQuizPackSelection(string tournamentId)
{
    if (!TournamentManager.Instance.IsAdmin())
    {
        Debug.LogError("Only admins can select quiz packs!");
        return;
    }

    var tournament = TournamentManager.Instance.GetTournament(tournamentId);
    
    if (tournament == null)
    {
        Debug.LogError("Tournament not found!");
        return;
    }

    if (tournament.status != TournamentStatus.Upcoming)
    {
        Debug.LogError("Cannot change quiz pack for active/ended tournament!");
        return;
    }

    currentSelectingTournamentId = tournamentId;
    //tournamentQuizPackPanelTitle.text = $"Select Quiz Pack for: {tournament.tournamentName}";
    
    GenerateTournamentQuizPackButtons();
    ChooseQuizPanel.SetActive(true);
}

private void GenerateTournamentQuizPackButtons()
{
    _ = GenerateTournamentQuizPackButtonsAsync();
}

// Update StartTournamentGame method in MainMenuUI

public void StartTournamentGame(string tournamentId)
{
    if (!TournamentManager.Instance.CanPlayerPlayTournament(tournamentId))
    {
        Debug.LogError("Cannot start tournament game!");
        return;
    }

    var tournament = TournamentManager.Instance.GetTournament(tournamentId);
    
    if (tournament == null)
    {
        Debug.LogError("Tournament not found!");
        return;
    }

    _ = StartTournamentGameAsync(tournamentId, tournament);
}

private async System.Threading.Tasks.Task StartTournamentGameAsync(string tournamentId, TournamentData tournament)
{
    QuizPackSO selectedQuizPack = null;
    EnsureOnlineRepository();

    if (tournament.usesOnlineQuizPack)
    {
        selectedQuizPack = await onlineQuizRepository.LoadRuntimeQuizPackAsync(tournament.quizPackId);
        if (selectedQuizPack == null)
        {
            Debug.LogError($"Online quiz pack '{tournament.quizPackId}' not found!");
            return;
        }

        GameModeManager.Instance.SetSelectedRuntimeOnlineQuizPack(tournament.quizPackId, selectedQuizPack);
        GameModeManager.Instance.SelectedOnlineQuizPackName = tournament.quizPackDisplayName;
        GameModeManager.Instance.SelectedOnlineQuizCategoryName = tournament.quizCategoryName;
    }
    else
    {
        foreach (var pack in availableQuizPacks)
        {
            if (pack.name == tournament.selectedQuizPackName)
            {
                selectedQuizPack = pack;
                break;
            }
        }

        if (selectedQuizPack == null)
        {
            Debug.LogError($"Quiz pack '{tournament.selectedQuizPackName}' not found!");
            return;
        }

        GameModeManager.Instance.SetSelectedLocalQuizPack(selectedQuizPack);
    }

    // Set up game mode manager
    GameModeManager.Instance.NumberOfPlayersToBeSpawned = 1;
    GameModeManager.Instance.IsTournamentMode = true;
    GameModeManager.Instance.CurrentTournamentId = tournamentId; // STORE TOURNAMENT ID
    
    Debug.Log($"Starting tournament: {tournament.tournamentName}");
    Debug.Log($"Tournament ID: {tournamentId}");
    Debug.Log($"Quiz Pack: {tournament.selectedQuizPackName}");
    
    // Load tournament scene
    LoadingSceneManager.Instance.LoadofflineScene(TournamentScene);
}

// Remove this method - no longer needed
/*
public string GetCurrentPlayingTournamentId()
{
    return currentPlayingTournamentId;
}
*/

// Call this from your game scene when game ends
public async void SubmitTournamentScore(int score)
{
    if (string.IsNullOrEmpty(currentPlayingTournamentId))
    {
        Debug.LogError("Not playing a tournament!");
        return;
    }

    Debug.Log($"Submitting tournament score: {score}");
    
    bool success = await TournamentManager.Instance.SubmitScore(currentPlayingTournamentId, score);
    
    if (success)
    {
        Debug.Log("Tournament score submitted successfully!");
    }
    else
    {
        Debug.LogError("Failed to submit tournament score!");
    }

    currentPlayingTournamentId = "";
}

public string GetCurrentPlayingTournamentId()
{
    return currentPlayingTournamentId;
}
   // Called when PassNPlay button is clicked
   public void OnPassNPlayClicked()
   {
      currentMode = GameModeUIManager.PassNPlay;
      //ShowPlayerSelectionUI();
   }

   // Called when a player number button is clicked
   public void OnPlayersChosen(int count)
   {
      GameModeManager.Instance.NumberOfPlayersToBeSpawned = count;
      ShowQuizPackButtons();
   }

   public async void ApplyName()
   {
      try
      {
         await AuthExtensions.UpdatePlayerNameAsync(username.text);
      }
      catch (Exception e)
      {
         Debug.Log(e.Message);
      }
   }

   public void OpenLinks(string Link)
   {
      Application.OpenURL(Link);
   }
   private async void ShowQuizPackButtons()
   {
      EnsureOnlineRepository();
      ResetOnlineQuizRequest();
      onlineQuizCts = new CancellationTokenSource();
      
      ChooseQuizPanel.SetActive(true);
      ClearQuizButtons();
      SetStatusButton("Loading quiz packs...", false);

      try
      {
         List<QuizPackManifestEntryDTO> entries = await onlineQuizRepository.GetSortedActiveEntriesAsync(onlineQuizCts.Token);
         ClearQuizButtons();

         if (entries.Count > 0)
         {
            foreach (var entry in entries)
            {
               GenerateOnlineQuizPackButton(entry, false);
            }
            return;
         }

         SetStatusButton("No online quiz packs available.", false);
      }
      catch (OperationCanceledException)
      {
         return;
      }
      catch (Exception e)
      {
         Debug.LogWarning($"Online quiz list unavailable: {e.Message}");
      }

      GenerateLocalQuizPackButtons();
   }

   private async System.Threading.Tasks.Task GenerateTournamentQuizPackButtonsAsync()
   {
      EnsureOnlineRepository();
      ResetOnlineQuizRequest();
      onlineQuizCts = new CancellationTokenSource();

      ClearQuizButtons();
      SetStatusButton("Loading quiz packs...", false);

      try
      {
         List<QuizPackManifestEntryDTO> entries = await onlineQuizRepository.GetSortedActiveEntriesAsync(onlineQuizCts.Token);
         ClearQuizButtons();

         if (entries.Count > 0)
         {
            foreach (var entry in entries)
            {
               GenerateOnlineQuizPackButton(entry, true);
            }
            return;
         }

         SetStatusButton("No online quiz packs available.", false);
      }
      catch (OperationCanceledException)
      {
         return;
      }
      catch (Exception e)
      {
         Debug.LogWarning($"Online tournament quiz list unavailable: {e.Message}");
      }

      GenerateLocalTournamentQuizPackButtons();
   }

   private void GenerateLocalQuizPackButtons()
   {
      ClearQuizButtons();

      foreach (var pack in availableQuizPacks)
      {
         if (pack == null) continue;
         GameObject btnObj = Instantiate(quizPackButtonPrefab, quizPackButtonParent);
         btnObj.GetComponentInChildren<TMP_Text>().text = pack.name;

         Button btn = btnObj.GetComponent<Button>();
         btn.onClick.AddListener(() =>
         {
            GameModeManager.Instance.SetSelectedLocalQuizPack(pack);

            if (currentMode == GameModeUIManager.Practice)
               LoadingSceneManager.Instance.LoadofflineScene(practiceSceneName);
            else if (currentMode == GameModeUIManager.PassNPlay)
               LoadingSceneManager.Instance.LoadofflineScene(passNPlaySceneName);
         });
      }
   }

   private void GenerateLocalTournamentQuizPackButtons()
   {
      ClearQuizButtons();

      foreach (var pack in availableQuizPacks)
      {
         if (pack == null) continue;
         GameObject btnObj = Instantiate(quizPackButtonPrefab, quizPackButtonParent);
         btnObj.GetComponentInChildren<TMP_Text>().text = pack.name;

         Button btn = btnObj.GetComponent<Button>();
         btn.onClick.AddListener(async () =>
         {
            bool success = await TournamentManager.Instance.SetTournamentQuizPack(
               currentSelectingTournamentId,
               pack.name
            );

            if (success)
            {
               ChooseQuizPanel.SetActive(false);
               Debug.Log($"Quiz pack '{pack.name}' selected for tournament");
            }
         });
      }
   }

   private void GenerateOnlineQuizPackButton(QuizPackManifestEntryDTO entry, bool isTournamentSelection)
   {
      GameObject btnObj = Instantiate(quizPackButtonPrefab, quizPackButtonParent);
      btnObj.GetComponentInChildren<TMP_Text>().text = FormatManifestButtonText(entry);

      Button btn = btnObj.GetComponent<Button>();
      btn.onClick.AddListener(async () =>
      {
         btn.interactable = false;
         GameModeManager.Instance.SetSelectedOnlineQuizPackEntry(entry);

         if (isTournamentSelection)
         {
            bool success = await TournamentManager.Instance.SetTournamentQuizPack(currentSelectingTournamentId, entry);
            if (success)
            {
               ChooseQuizPanel.SetActive(false);
               Debug.Log($"Online quiz pack '{entry.displayName}' selected for tournament");
            }
            else
            {
               btn.interactable = true;
            }

            return;
         }

         QuizPackSO runtimePack = await onlineQuizRepository.LoadRuntimeQuizPackAsync(entry.packId);
         if (runtimePack == null)
         {
            Debug.LogError($"Failed to load online quiz pack '{entry.packId}'.");
            btn.interactable = true;
            return;
         }

         GameModeManager.Instance.SetSelectedRuntimeOnlineQuizPack(entry.packId, runtimePack);
         GameModeManager.Instance.SelectedOnlineQuizPackName = entry.displayName;
         GameModeManager.Instance.SelectedOnlineQuizCategoryName = entry.categoryName;

         if (currentMode == GameModeUIManager.Practice)
            LoadingSceneManager.Instance.LoadofflineScene(practiceSceneName);
         else if (currentMode == GameModeUIManager.PassNPlay)
            LoadingSceneManager.Instance.LoadofflineScene(passNPlaySceneName);
      });
   }

   private string FormatManifestButtonText(QuizPackManifestEntryDTO entry)
   {
      string displayName = !string.IsNullOrWhiteSpace(entry.displayName) ? entry.displayName : entry.packId;
      string category = !string.IsNullOrWhiteSpace(entry.categoryName) ? entry.categoryName : onlineQuizConfig != null ? onlineQuizConfig.defaultCategoryName : "General";
      return $"{displayName}\n{category} - {entry.questionCount} Questions";
   }

   private void SetStatusButton(string message, bool interactable)
   {
      GameObject btnObj = Instantiate(quizPackButtonPrefab, quizPackButtonParent);
      btnObj.GetComponentInChildren<TMP_Text>().text = message;
      Button btn = btnObj.GetComponent<Button>();
      if (btn != null) btn.interactable = interactable;
   }

   private void ClearQuizButtons()
   {
      foreach (Transform child in quizPackButtonParent)
         Destroy(child.gameObject);
   }

   private void EnsureOnlineRepository()
   {
      if (onlineQuizRepository == null)
      {
         onlineQuizRepository = new OnlineQuizRepository(onlineQuizConfig, availableQuizPacks);
      }
      else
      {
         onlineQuizRepository.SetLocalFallbackPacks(availableQuizPacks);
      }
   }

   private void ResetOnlineQuizRequest()
   {
      onlineQuizCts?.Cancel();
      onlineQuizCts?.Dispose();
      onlineQuizCts = null;
   }
   
   public void SignOut()
   {
      LoadingSceneManager.Instance.LoadofflineScene("SignUp_SignIn");
      AuthExtensions.SignOut();
   }

   public async void LeaderBoardSetUp()
   {
      var playerScore = await Leaderboard.Instance.GetPlayerScore();
      LeaderboardItemSetUp(CurrentPlayerLeaderBoardItem,playerScore.Rank,(int)playerScore.Score,playerScore.PlayerName);
      var Scores = await Leaderboard.Instance.GetPaginatedScores();
      foreach(var entry in Scores.Results)
      {
         GameObject scoreEntry = Instantiate(LeaderBoardItemPrefab, LeaderBoardItemParent);
         LeaderboardItemSetUp(scoreEntry,entry.Rank+1, (int)entry.Score,entry.PlayerName);
      }
   }

   public void LeaderboardItemSetUp(GameObject item, int rank, int score, string name)
   {
      item.transform.Find("PlayerName").GetComponent<TMP_Text>().text = name;
      item.transform.GetChild(0).Find("Rankings").GetComponent<TMP_Text>().text = rank.ToString();
      item.transform.Find("Score_Text").GetComponent<TMP_Text>().text = score.ToString();
   }

   private void LockModes(GameObject obj)
   {
      obj.GetComponent<Image>().color = LockColor;
      obj.GetComponent<Button>().interactable = false;
      obj.transform.Find("Icon").GetComponent<Image>().color = LockColor;
      obj.transform.Find("Text").GetComponent<TMP_Text>().color = Color.black;
      obj.transform.Find("Lock_Icon").gameObject.SetActive(true);
   }
   private bool IsInternetReachable() => Application.internetReachability == NetworkReachability.NotReachable;


}
