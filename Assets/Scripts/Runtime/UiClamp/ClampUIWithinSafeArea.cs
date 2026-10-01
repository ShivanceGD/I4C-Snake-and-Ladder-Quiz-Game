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
    public static ClampUIWithinSafeArea Instance { get; private set; }

    public GameObject[] foundButtons;
    public bool isPrivateRoom = false;
    public InputField sessionNameInput;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ClampUi();
    }

    private void Start()
    {
        WireCreateButton();
    }

    private void WireCreateButton()
    {
        var go = GameObject.Find("CreateSession_Button");
        if (go != null)
        {
            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => CreatePrivateSession(GetSessionName()));
            }
        }
    }

    private string GetSessionName()
    {
        if (sessionNameInput != null && !string.IsNullOrEmpty(sessionNameInput.text))
            return sessionNameInput.text;
        return "MyRoom";
    }

    public void FindButtons()
    {
        foundButtons = GameObject.FindGameObjectsWithTag("CloseButton");
        foreach (GameObject obj in foundButtons)
        {
            obj.GetComponent<Button>().onClick.AddListener(() => SoundManager.Instance.PlayCloseSound());
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
    }

    public string multiplayerSceneName = "Multiplayer_Level_New";

    private ISession currentSession;

    public void OnJoinedSession(ISession session)
    {
        currentSession = session;

        if (session.IsHost)
        {
            session.PlayerJoined += (player) =>
            {
                CheckPlayers();
            };
        }
    }

    private void CheckPlayers()
    {
        if (currentSession == null || !NetworkManager.Singleton.IsHost)
            return;

        if (!isPrivateRoom)
        {
            if (currentSession.Players.Count >= 2)
            {
                Debug.Log("Two players are in session! Auto-loading multiplayer scene...");
                NetworkManager.Singleton.SceneManager.LoadScene(multiplayerSceneName, LoadSceneMode.Single);
            }
        }
        else
        {
            Debug.Log("Private room detected. Waiting for host to manually start.");
        }
    }

    public void StartPrivateRoomGame()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("Host manually starting game...");
            NetworkManager.Singleton.SceneManager.LoadScene(multiplayerSceneName, LoadSceneMode.Single);
        }
    }

    public void SetPrivateRoom(bool isPrivate)
    {
        isPrivateRoom = isPrivate;
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.IsPrivateRoom = isPrivate;
        }
    }

    public async void CreatePrivateSession(string sessionName)
    {
        EnsureCustomSessionController();
        if (CustomSessionController.Instance != null)
        {
            await CustomSessionController.Instance.CreatePrivateSessionAsync(sessionName);
        }
    }

    public async void QuickJoin()
    {
        EnsureCustomSessionController();
        if (CustomSessionController.Instance != null)
        {
            await CustomSessionController.Instance.QuickJoinAsync();
        }
    }

    private void EnsureCustomSessionController()
    {
        if (CustomSessionController.Instance == null)
        {
            var go = new GameObject("CustomSessionController");
            go.AddComponent<CustomSessionController>();
            DontDestroyOnLoad(go);
        }
    }
}
