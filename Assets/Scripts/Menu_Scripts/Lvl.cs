/*
using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Lvl : MonoBehaviour
{
    [Header("Button Settings")]
    
    public string SceneName;
    public LevelDataSO LevelSCO;
    public GameObject LockImage;
    public GameObject LevelUnlocked;
    public GameObject LevelCompleted;
    private Button LevelButton;
    public List<GameObject> StarsList = new List<GameObject>();
    public TMP_Text Number;
    
    // Start is called before the first frame update

    void Start()
    {
       
        LevelButton = GetComponent<Button>();
        Number.text = LevelSCO.LevelNumber.ToString();
        LevelCheck();
    }

    // Update is called once per frame
    void Update()
    {
       /* //if Level Completed
        LevelCompleted.gameObject.SetActive(true);
        LevelButton.interactable = false;#1#
        
    }
    private void LevelCheck()
    {
        if (!LevelSCO.isLevelUnlocked)
        {
            LockImage.SetActive(true);
            LevelUnlocked.SetActive(false);
        }
        else
        {
            LockImage.SetActive(false);
            if (LevelSCO.isLevelCompleted)
            {
                LevelCompleted.SetActive(true);
                StarsDisplay();
            }

            else
            {
                LevelUnlocked.SetActive(true);
            }
            
        }
        LevelButton.onClick.AddListener(onButtonClick);
    }
    private void onButtonClick()
    {
       
        //SceneManager.LoadScene(SceneName);
        Debug.Log("Scene Loaded");
    }
    private void StarsDisplay()
    { 
        for(int i=0; i<LevelSCO.LevelStars; i++)
        {
            StarsList[i].SetActive(true);
        }
    }




}
*/
