using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
   public void LoadScene(string scene)
   {
      LoadingSceneManager.Instance.LoadofflineScene(scene);
      LoadingSceneManager.Instance.SetLoadingScreenMessage("Loading...");
   }
}
