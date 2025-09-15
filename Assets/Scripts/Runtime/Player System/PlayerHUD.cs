using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text stateText;
    public List<String> LadderTexts;
    public List<String> SnakeTexts;

    public void ShowTurn(string text)
    {
        if (turnText != null) turnText.text = text;
    }

    public void ShowState(string text)
    {
        if (stateText != null) stateText.text = text;
    }

    public void Clear()
    {
        if (turnText != null) turnText.text = "";
        if (stateText != null) stateText.text = "";
    }
    /// <summary>
    /// Picks a random text from LadderTexts and shows it in stateText
    /// </summary>
    public void ShowRandomLadderText()
    {
        if (LadderTexts == null || LadderTexts.Count == 0) return;
       
       
        ShowState(LadderTexts[UnityEngine.Random.Range(0, LadderTexts.Count)]);
    }

    /// <summary>
    /// Picks a random text from SnakeTexts and shows it in stateText
    /// </summary>
    public void ShowRandomSnakeText()
    {
        if (SnakeTexts == null || SnakeTexts.Count == 0) return;
        ShowState(SnakeTexts[UnityEngine.Random.Range(0, SnakeTexts.Count)]);
    }
}
