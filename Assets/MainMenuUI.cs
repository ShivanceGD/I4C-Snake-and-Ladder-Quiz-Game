using System;
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
   public GameObject ChooseQuizPanel;
   public GameObject StoryModeButton;
   public GameObject MultiplayerButton;
   public Color LockColor;
   public TournamentManager tournamentManager;
   [Header("Scenes")]
   public string practiceSceneName = "PracticeScene";
   public string passNPlaySceneName = "PassNPlayScene";

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

   public void Start()
   {
      username.text = AuthExtensions.GetCachedPlayerName();
      placeholderUsername.text = AuthExtensions.GetCachedPlayerName();
      id.text = AuthExtensions.GetPlayerID();

      if (IsInternetReachable())
      {
         LockModes(StoryModeButton);
         LockModes(MultiplayerButton);
      }
   }
   public void OnCreateTournamentMenuOpened()
   {
      tournamentManager.GenerateQuizPackButtons(availableQuizPacks,quizPackButtonPrefab,quizPackButtonParent);
   }
   public void OnPracticeClicked()
   {
      currentMode = GameModeUIManager.Practice;
      GameModeManager.Instance.NumberOfPlayersToBeSpawned = 1;
      ShowQuizPackButtons();
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
   private void ShowQuizPackButtons()
   {
      // Clear old buttons
      foreach (Transform child in quizPackButtonParent)
         Destroy(child.gameObject);

      ChooseQuizPanel.SetActive(true);
      // Spawn new ones
      foreach (var pack in availableQuizPacks)
      {
         GameObject btnObj = Instantiate(quizPackButtonPrefab, quizPackButtonParent);
         btnObj.GetComponentInChildren<TMP_Text>().text = pack.name;

         Button btn = btnObj.GetComponent<Button>();
         btn.onClick.AddListener(() =>
         {
            GameModeManager.Instance.QuizPack = pack;

            if (currentMode == GameModeUIManager.Practice)
               LoadingSceneManager.Instance.LoadofflineScene(practiceSceneName);
            else if (currentMode == GameModeUIManager.PassNPlay)
               LoadingSceneManager.Instance.LoadofflineScene(passNPlaySceneName);
         });
      }
   }
   
   public void SignOut()
   {
      LoadingSceneManager.Instance.LoadofflineScene("SignUp_SignIn");
      AuthExtensions.SignOut();
   }

   public async void LeaderBoardSetUp()
   {
      var playerScore = await Leaderboard.Instance.GetPlayerScore();
      LeaderboardItemSetUp(CurrentPlayerLeaderBoardItem,playerScore.Rank+1,(int)playerScore.Score,playerScore.PlayerName);
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