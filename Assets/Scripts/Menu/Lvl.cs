using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Lvl : MonoBehaviour
{
    [Header("Button Settings")]
    
    public string SceneName;
    public LevelSettingsScriptableObject LevelSCO;
    public GameObject LockImage;
    public GameObject LevelUnlocked;
    public GameObject LevelCompleted;
    private Button LevelButton;

 
    
    // Start is called before the first frame update

    void Start()
    {
       
        LevelButton = GetComponent<Button>();
        GetComponentInChildren<TMP_Text>().text = LevelSCO.LevelNumber.ToString();
        LevelCheck();
    }

    // Update is called once per frame
    void Update()
    {
       /* //if Level Completed
        LevelCompleted.gameObject.SetActive(true);
        LevelButton.interactable = false;*/
        
    }
    private void LevelCheck()
    {
        if (!LevelSCO.isLevelUnlocked)
        {
            LockImage.SetActive(false);
            
        }
        else
        {
            LockImage.SetActive(true);
            
        }
        LevelButton.onClick.AddListener(onButtonClick);
    }
    public void onButtonClick()
    {
       
        //SceneManager.LoadScene(SceneName);
        Debug.Log("Scene Loaded");
    }

    


  
}
