using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelManager : MonoBehaviour
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
        SetPlayersNames();
    }
    public void CacheCurrentLevelIfNeeded()
    {
        if (currentLevel == null) Debug.LogError("LevelManager: CurrentLevel not assigned.");
        // other caching if needed
    }
    public void SetPlayersNames()
    {
        for(int i = 0;i<turn.players.Count;i++)
        {
            GameObject obj = Instantiate(PlayerPrefab, PlayerPanelTransform);
            obj.GetComponentInChildren<TMP_Text>().text = turn.players[i].name;
            obj.transform.GetChild(0).GetChild(2).GetComponentInChildren<TMP_Text>().text = (i + 1).ToString();
        }
    }
}
