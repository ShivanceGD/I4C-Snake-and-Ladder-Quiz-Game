using System;
using Ricimi;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class StartScene : NetworkBehaviour
{
    
    //[SerializeField] private UnityEvent onCannotStartGame;
    
    [Header("References and Setting")]
    [SerializeField] private string sceneName;
    [SerializeField] private int minimumClients;
    [SerializeField] private Button startButton;
    [SerializeField] private bool isOffline;
    

    public float duration = 1.0f;
    public Color color = Color.black;

    public void PerformTransition(String SceneName)
    {
        Transition.LoadLevel(SceneName, duration, color);
    }
    private void OnEnable()
    {
        if(startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            if(!isOffline)
            {
                startButton.onClick.AddListener(()=> StartMultiplayerSceneGame(sceneName, minimumClients));
            }
            if(isOffline)
            {
                startButton.onClick.AddListener(() => StartOfflineSceneGame(sceneName));
            }
        }
        else { Debug.LogError("Assign Start Button"); }
    }
    private void StartMultiplayerSceneGame(string sceneName, int clients)
    {
        if(NetworkManager.Singleton.ConnectedClients.Count >= clients && sceneName != null)
        {
            NetworkManager.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            GameManager.Instance.isOfflineMode = isOffline;
        }
        else
        {
            //onCannotStartGame?.Invoke();
        }
    }
   
    public void StartOfflineSceneGame(string sceneName)
    {
        if(this.sceneName != null)
        {
            //SceneManager.LoadScene(sceneName);
            GameManager.Instance.isOfflineMode = isOffline;
        }
        else
        {
            Debug.Log("Enter scene name");
        }
        
    }
}
