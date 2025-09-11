using TMPro;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text stateText;

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
}
