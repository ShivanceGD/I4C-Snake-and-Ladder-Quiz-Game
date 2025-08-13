using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private LevelSettingsScriptableObject currentLevel;
    public LevelSettingsScriptableObject CurrentLevel => currentLevel;

    private void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
    }

    public void CacheCurrentLevelIfNeeded()
    {
        if (currentLevel == null) Debug.LogError("LevelManager: CurrentLevel not assigned.");
        // other caching if needed
    }
}
