using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : NetworkBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private LevelSettingsScriptableObject currentLevel;
    public LevelSettingsScriptableObject CurrentLevel => currentLevel;
    public TurnManager turn;
    public GameObject PlayerPrefab;
    public Transform PlayerPanelTransform;
    
    

    private void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
    }
    private void Start()
    {
        
    }
    public void CacheCurrentLevelIfNeeded()
    {
        if (currentLevel == null) Debug.LogError("LevelManager: CurrentLevel not assigned.");
        // other caching if needed
    }
    public void SetPlayersNames()
    {
        int i = 0;
        foreach(PlayerManager player in turn.players)
        {
            
            GameObject obj = Instantiate(PlayerPrefab, PlayerPanelTransform);
            obj.GetComponentInChildren<TMP_Text>().text = $"Player {i+1}";
            obj.transform.GetChild(0).GetComponentInChildren<Image>().color = player.spriteRenderer.color;
            obj.transform.GetChild(0).GetChild(2).GetComponentInChildren<TMP_Text>().text = (i + 1).ToString();
            i++;
        }
    }
    public void PauseGame()
    {
        Time.timeScale = 0f;
    }
    public void ResumeGame()
    {
        Time.timeScale = 1f;
    }

 
}
