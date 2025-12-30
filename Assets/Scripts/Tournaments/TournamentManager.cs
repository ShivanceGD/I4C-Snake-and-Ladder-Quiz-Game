using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // make sure this is correct, not UIElements

public class TournamentManager : MonoBehaviour
{
    [Header("Tournament Settings References")]
    public TMP_InputField tournamentNameInput;
    public TMP_InputField NumberOfTimesPlayerCanPlay;
    
    [Header("Duration Settings")]
    public TMP_InputField durationInput; // in minutes
    public int minDuration;

    [Header("Private Tournament Settings")]
    public Toggle privateToggle;
    public GameObject accessKeyPanel; // parent or input field object to show/hide
    public TMP_InputField accessKeyInput;

    [Header("Other References")]
    public QuizPackSO quizPack;

    private void Start()
    {
        // Start hidden
        accessKeyPanel?.SetActive(false);

        // Subscribe to toggle instantly show/hide on click
        privateToggle?.onValueChanged.AddListener(isOn =>
        {
            accessKeyPanel?.SetActive(isOn);

            // Optionally clear the key when toggled off
            if (!isOn && accessKeyInput != null)
                accessKeyInput.text = string.Empty;
        });
    }

    public void OnCreateTournamentClicked()
    {
        string name = tournamentNameInput.text;
        // Duration in minutes
        int durationMinutes = 30; // default
        if (int.TryParse(durationInput.text, out int result))
            durationMinutes = Mathf.Max(result, minDuration); // min 5 mins
        // ✅ Number of Plays (default 1)
        int numberOfPlays = 1;
        if (int.TryParse(NumberOfTimesPlayerCanPlay.text, out int plays) && plays > 0)
            numberOfPlays = plays;
        // Try parsing user inputs, fallback to defaults
        DateTime startTime = DateTime.Now;
        DateTime endTime = startTime.AddMinutes(durationMinutes);
        
        bool isPrivate = privateToggle != null && privateToggle.isOn;
        string accessKey = isPrivate && accessKeyInput != null ? accessKeyInput.text : null;

        // Create the tournament
        Tournament newTournament = new Tournament(
            name,
            name, // placeholder leaderboardId (replace with actual later)
            AuthExtensions.GetCachedPlayerName(),
            isPrivate,
            accessKey,
            numberOfPlays,
            startTime,
            endTime,
            quizPack
        );

        Debug.Log($"[Tournament Created] {newTournament.tournamentname} | Private: {isPrivate} | Key: {accessKey ?? "None"} | {newTournament.StartTime} → {newTournament.EndTime}");
    }
    public void GenerateQuizPackButtons(QuizPackSO[] quizPacks,GameObject quizPackButtonPrefab,Transform quizPackButtonParent)
    {
        // Clear old ones
        foreach (Transform child in quizPackButtonParent)
            Destroy(child.gameObject);

        // Spawn new quiz pack buttons
        foreach (var pack in quizPacks)
        {
            GameObject btnObj = Instantiate(quizPackButtonPrefab, quizPackButtonParent);
            btnObj.GetComponentInChildren<TMP_Text>().text = pack.name;
            btnObj.GetComponent<Button>().onClick.AddListener(() => quizPack=pack);
        }
    }
}
