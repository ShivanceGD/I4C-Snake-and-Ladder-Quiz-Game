using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
   public void LoadScene(string scene)
   {
      LoadingSceneManager.Instance.LoadScene(scene);
      LoadingSceneManager.Instance.SetLoadingScreenMessage("Loading...");
   }
}
