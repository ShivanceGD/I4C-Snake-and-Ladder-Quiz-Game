/*
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : NetworkBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private LevelDataSO currentLevel;
    public LevelDataSO CurrentLevel => currentLevel;
    //public TurnManager turn;
    public GameObject PlayerPrefab;
    public Transform PlayerPanelTransform;

    public GameObject startGamePannel;
    public Button startGame;
    public Button CloseByutton;

    private void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
    }
    private void Start()
    {
        SetPlayersNames();
    }
    public void CacheCurrentLevelIfNeeded()
    {
        if (currentLevel == null) Debug.LogError("LevelManager: CurrentLevel not assigned.");
        // other caching if needed
    }
    public void SetPlayersNames()
    {
        int count = turn.players.Count;

        for (int i = 0; i < count; i++)
        {
            var p = turn.players[i];
            // send info to all clients
            SetPlayerNameClientRpc(i, p.spriteRenderer.color);
        }
    }

    [ClientRpc]
    private void SetPlayerNameClientRpc(int playerIndex, Color color)
    {
        GameObject obj = Instantiate(PlayerPrefab, PlayerPanelTransform);
        obj.transform.GetChild(1).GetComponent<TMP_Text>().text = $"Player {playerIndex + 1}";
        obj.transform.GetChild(0).GetComponentInChildren<Image>().color = color;
        obj.transform.GetChild(0).GetChild(1).GetComponentInChildren<TMP_Text>().text = (playerIndex + 1).ToString();
    }
    public void PauseGame() => Time.timeScale = 0f;



    public void ResumeGame() => Time.timeScale = 1f;
    

 
}
*/
