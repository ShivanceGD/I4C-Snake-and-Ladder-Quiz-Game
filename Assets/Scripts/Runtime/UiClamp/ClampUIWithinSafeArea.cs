using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(RectTransform))]
public class ClampUIWithinSafeArea : NetworkBehaviour
{
    private void Awake()
    {
        ClampUi();
    }
    public void ClampUi()
    {
        var rectTransform = GetComponent<RectTransform>();

        var safeArea = Screen.safeArea;
        var anchorMin = safeArea.position;
        var anchorMax = anchorMin + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
    }

    public void LoadofflineScene(string sceneName)
    {
        LoadingSceneManager.Instance.LoadofflineScene(sceneName);
        LoadingSceneManager.Instance.SetLoadingScreenMessage("Loading...");
    }

    public void LoadonlineScene(string SceneName)
    {
        if (!NetworkManager.Singleton.IsServer)
            return; 
        NetworkManager.Singleton.SceneManager.LoadScene("Multiplayer_Level_New", LoadSceneMode.Single);
        /*LoadingSceneManager.Instance.LoadOnlineScene(SceneName);
        LoadingSceneManager.Instance.SetLoadingScreenMessage("Loading...");*/
    }
}