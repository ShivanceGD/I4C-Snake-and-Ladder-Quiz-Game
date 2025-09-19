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
   
   [Header("Scenes")]
   public string practiceSceneName = "PracticeScene";
   public string passNPlaySceneName = "PassNPlayScene";

   private GameModeUIManager currentMode = GameModeUIManager.None;

   // Called when Practice button is clicked
   public void OnPracticeClicked()
   {
      currentMode = GameModeUIManager.Practice;
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
         btnObj.GetComponentInChildren<TMPro.TMP_Text>().text = pack.name;

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
      AuthExtensions.SignOut();
   }

   /*public async void Wait(float seconds)
   {
      int milliseconds = (int)(seconds * 1000);
      await Task.Delay(milliseconds);
   }*/

  
}
