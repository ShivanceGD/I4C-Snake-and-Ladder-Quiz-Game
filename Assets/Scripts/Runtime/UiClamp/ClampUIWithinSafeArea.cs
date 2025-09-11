using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ClampUIWithinSafeArea : NetworkBehaviour
{
    public GameObject[] foundButtons;
     private void Awake()
    {
        ClampUi();
    }

    public void FindButtons()
    {
        foundButtons = GameObject.FindGameObjectsWithTag("CloseButton");
        foreach (GameObject obj in foundButtons)
        {
                obj.GetComponent<Button>().onClick.AddListener(()=> SoundManager.Instance.PlayCloseSound());
        }
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

    public void LoadScene(string sceneName)
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
    
    public string multiplayerSceneName = "Multiplayer_Level_New";

    private ISession currentSession;

    // Hook this up in the Inspector to "Joined Session (ISession)"
    public void OnJoinedSession(ISession session)
    {
        currentSession = session;

        if (session.IsHost)
        {
            NetworkManager.Singleton.StartHost();
            session.PlayerJoined += (player) =>
            {
                //Debug.Log($"SDK Player joined: {player.Id}");
                CheckPlayers();
            };
        }
        else
        {
            NetworkManager.Singleton.StartClient();
        }
    }

    private void CheckPlayers()
    {
        if (currentSession != null && currentSession.Players.Count >= 2 && NetworkManager.Singleton.IsHost)
        {
            Debug.Log("Two players are in session! Loading multiplayer scene...");
            NetworkManager.Singleton.SceneManager.LoadScene(multiplayerSceneName, LoadSceneMode.Single);
        }
    }
}