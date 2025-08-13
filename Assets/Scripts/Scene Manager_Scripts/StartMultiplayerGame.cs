using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class StartMultiplayerGame : NetworkBehaviour
{
    [SerializeField] private UnityEvent onCannotStartGame;

    [Header("References and Setting")]
    [SerializeField] private string multiplayerSceneName;
    [SerializeField] private int minimumClients;
    [SerializeField] private Button startButton;

    private void OnEnable()
    {
        if(startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(()=> StartMultiplayerSceneGame(multiplayerSceneName, minimumClients));
        }
        else { Debug.LogError("Assign Start Button"); }
    }
    private void StartMultiplayerSceneGame(string SceneName, int clients)
    {
        if(NetworkManager.Singleton.ConnectedClients.Count >= clients)
        {
            NetworkManager.SceneManager.LoadScene(SceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        else
        {
            onCannotStartGame?.Invoke();
        }
    }
}
