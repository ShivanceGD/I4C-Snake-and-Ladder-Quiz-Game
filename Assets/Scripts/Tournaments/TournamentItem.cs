using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class TournamentItem : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI playersText;
    public Button joinButton;
    public Button leaveButton;
    public Button startButton;
    public Button endButton;
    public Button deleteButton;
    public Button viewLeaderboardButton;
    public TMP_InputField passwordInput;
    public GameObject passwordPanel;
    
    [Header("Quiz Pack & Play")]
    public TextMeshProUGUI quizPackText;
    public Button selectQuizPackButton; // Creator only
    public Button playButton; // Participants only
    
    [Header("Creator Indicator")]
    public TextMeshProUGUI creatorText; // Shows if you're the creator

    [Header("Main Menu Reference")]
    public MainMenuUI mainMenuUI; // Set via Setup or find in scene

    private TournamentData tournamentData;
    private TournamentUI tournamentUI;
    private float updateTimer = 0f;
    private const float UPDATE_INTERVAL = 0.5f; // Update every 0.5 seconds for smooth countdown

    public void Setup(TournamentData data, TournamentUI ui)
    {
        tournamentData = data;
        tournamentUI = ui;
    
        // Find MainMenuUI if not set
        if (mainMenuUI == null)
        {
            mainMenuUI = FindObjectOfType<MainMenuUI>();
        }
    
        UpdateDisplay();
        SetupButtons();
    }

    // NEW: Public method to get tournament ID
    public string GetTournamentId()
    {
        return tournamentData?.tournamentId;
    }

    private void Update()
    {
        // Update time display continuously for live countdown
        updateTimer += Time.deltaTime;
        
        if (updateTimer >= UPDATE_INTERVAL)
        {
            updateTimer = 0f;
            UpdateTimeDisplay();
        }
    }

    private void OnDestroy()
    {
        // Clean up button listeners
        if (joinButton != null) joinButton.onClick.RemoveAllListeners();
        if (leaveButton != null) leaveButton.onClick.RemoveAllListeners();
        if (startButton != null) startButton.onClick.RemoveAllListeners();
        if (endButton != null) endButton.onClick.RemoveAllListeners();
        if (deleteButton != null) deleteButton.onClick.RemoveAllListeners();
        if (viewLeaderboardButton != null) viewLeaderboardButton.onClick.RemoveAllListeners();
        if (selectQuizPackButton != null) selectQuizPackButton.onClick.RemoveAllListeners();
        if (playButton != null) playButton.onClick.RemoveAllListeners();
    }

    private void UpdateDisplay()
    {
        nameText.text = tournamentData.tournamentName;
        
        // Update status text with color
        switch (tournamentData.status)
        {
            case TournamentStatus.Upcoming:
                statusText.text = tournamentData.isPrivate ? "UPCOMING (Private)" : "UPCOMING";
                statusText.color = Color.yellow;
                break;
            case TournamentStatus.Active:
                statusText.text = "ACTIVE";
                statusText.color = Color.green;
                break;
            case TournamentStatus.Ended:
                statusText.text = "ENDED";
                statusText.color = Color.gray;
                break;
            default:
                statusText.text = "UNKNOWN";
                statusText.color = Color.white;
                break;
        }
        
        UpdateTimeDisplay();
        playersText.text = $"{tournamentData.participantUIDs.Count}/{tournamentData.maxPlayers}";

        // Show/hide password panel
        if (passwordPanel != null)
        {
            passwordPanel.SetActive(tournamentData.isPrivate);
        }

        // Show creator indicator
        bool isCreator = TournamentManager.Instance.IsCreatorOfTournament(tournamentData.tournamentId);
        if (creatorText != null)
        {
            creatorText.gameObject.SetActive(isCreator);
            creatorText.text = "Created by You";
            creatorText.color = Color.cyan;
        }

        // Quiz Pack Display - VISIBLE TO ALL
        if (quizPackText != null)
        {
            if (!string.IsNullOrEmpty(tournamentData.selectedQuizPackName))
            {
                quizPackText.text = $"Quiz Pack: {tournamentData.selectedQuizPackName}";
                quizPackText.color = Color.white;
            }
            else
            {
                quizPackText.text = "No Quiz Pack Selected";
                quizPackText.color = Color.yellow;
            }
            
            quizPackText.gameObject.SetActive(true);
        }
    }

    private void UpdateTimeDisplay()
    {
        if (tournamentData == null || timeText == null) return;
        
        // Get current time in UTC (for accurate comparison)
        DateTime currentUTC = DateTime.UtcNow;
        
        // Convert stored UTC times to IST for display
        DateTime startTimeIST = TournamentManager.ConvertUTCToIST(tournamentData.startTime);
        DateTime endTimeIST = TournamentManager.ConvertUTCToIST(tournamentData.endTime);
        
        string timeDisplay = $"{startTimeIST:MM/dd HH:mm} - {endTimeIST:HH:mm} IST";
        
        // Show countdown or status based on tournament status
        if (tournamentData.status == TournamentStatus.Upcoming)
        {
            // Calculate time until start using UTC times for accuracy
            TimeSpan timeUntilStart = tournamentData.startTime - currentUTC;
            
            if (timeUntilStart.TotalSeconds > 0)
            {
                if (timeUntilStart.TotalHours < 1)
                {
                    // Less than 1 hour - show minutes and seconds
                    timeDisplay += $"\nStarts in {timeUntilStart.Minutes}m {timeUntilStart.Seconds}s";
                }
                else if (timeUntilStart.TotalHours < 24)
                {
                    // Less than 24 hours - show hours and minutes
                    timeDisplay += $"\nStarts in {(int)timeUntilStart.TotalHours}h {timeUntilStart.Minutes}m";
                }
                else
                {
                    // More than 24 hours - show days
                    int days = (int)timeUntilStart.TotalDays;
                    timeDisplay += $"\nStarts in {days} day{(days > 1 ? "s" : "")}";
                }
            }
            else
            {
                // Past scheduled start time but not started yet
                timeDisplay += "\nReady to Auto-Start!";
            }
        }
        else if (tournamentData.status == TournamentStatus.Active)
        {
            // Calculate time until end using UTC times for accuracy
            TimeSpan timeUntilEnd = tournamentData.endTime - currentUTC;
            
            if (timeUntilEnd.TotalSeconds > 0)
            {
                if (timeUntilEnd.TotalMinutes < 5)
                {
                    // Less than 5 minutes - URGENT with seconds
                    timeDisplay += $"\n Ends in {(int)timeUntilEnd.TotalMinutes}m {timeUntilEnd.Seconds}s";
                }
                else if (timeUntilEnd.TotalHours < 1)
                {
                    // Less than 1 hour - show minutes and seconds
                    timeDisplay += $"\nEnds in {(int)timeUntilEnd.TotalMinutes}m {timeUntilEnd.Seconds}s";
                }
                else
                {
                    // More than 1 hour - show hours and minutes
                    timeDisplay += $"\n>Ends in {(int)timeUntilEnd.TotalHours}h {timeUntilEnd.Minutes}m";
                }
            }
            else
            {
                // Time passed but status not updated yet
                timeDisplay += "\n>Ending...";
            }
        }
        else if (tournamentData.status == TournamentStatus.Ended)
        {
            // Calculate time since end using UTC times
            TimeSpan timeSinceEnd = currentUTC - tournamentData.endTime;
            
            if (timeSinceEnd.TotalHours < 1)
            {
                // Ended less than 1 hour ago
                timeDisplay += $"\nEnded {(int)timeSinceEnd.TotalMinutes}m ago";
            }
            else if (timeSinceEnd.TotalDays < 1)
            {
                // Ended less than 24 hours ago
                timeDisplay += $"\nEnded {(int)timeSinceEnd.TotalHours}h ago";
            }
            else
            {
                // Ended more than 24 hours ago
                int days = (int)timeSinceEnd.TotalDays;
                timeDisplay += $"\nEnded {days} day{(days > 1 ? "s" : "")} ago";
            }
        }
        
        timeText.text = timeDisplay;
    }

    private void SetupButtons()
    {
        // Check if creator instead of admin
        bool isCreator = TournamentManager.Instance.IsCreatorOfTournament(tournamentData.tournamentId);
        string playerId = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;
        bool isParticipant = tournamentData.participantUIDs.Contains(playerId);

        // Join button - visible if not joined and tournament is upcoming
        if (joinButton != null)
        {
            joinButton.gameObject.SetActive(
                !isParticipant && 
                tournamentData.status == TournamentStatus.Upcoming &&
                tournamentData.participantUIDs.Count < tournamentData.maxPlayers
            );
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(OnJoinTournament);
        }

        // Leave button - visible if joined and tournament is upcoming
        if (leaveButton != null)
        {
            leaveButton.gameObject.SetActive(
                isParticipant && 
                tournamentData.status == TournamentStatus.Upcoming
            );
            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(OnLeaveTournament);
        }

        // Start button - CREATOR ONLY, upcoming tournaments with participants and quiz pack
        bool canManuallyStart = isCreator && 
                                tournamentData.status == TournamentStatus.Upcoming &&
                                tournamentData.participantUIDs.Count > 0 &&
                                !string.IsNullOrEmpty(tournamentData.selectedQuizPackName);
        
        if (startButton != null)
        {
            startButton.gameObject.SetActive(canManuallyStart);
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartTournament);
            
            // Update start button appearance based on timing
            if (canManuallyStart)
            {
                var startButtonText = startButton.GetComponentInChildren<TextMeshProUGUI>();
                TimeSpan timeUntilStart = tournamentData.startTime - DateTime.UtcNow;
                
                if (timeUntilStart.TotalSeconds <= 0)
                {
                    // Past scheduled time - should auto-start soon
                    if (startButtonText != null) startButtonText.text = "Start Now";
                    var img = startButton.GetComponent<Image>();
                    if (img != null) img.color = new Color(1f, 0.5f, 0f); // Orange
                }
                else
                {
                    // Before scheduled time - creator can start early
                    if (startButtonText != null) startButtonText.text = "Start Early";
                    var img = startButton.GetComponent<Image>();
                    if (img != null) img.color = Color.green;
                }
            }
        }

        // End button - CREATOR ONLY, active tournaments
        if (endButton != null)
        {
            endButton.gameObject.SetActive(
                isCreator && 
                tournamentData.status == TournamentStatus.Active
            );
            endButton.onClick.RemoveAllListeners();
            endButton.onClick.AddListener(OnEndTournament);
        }

        // Delete button - CREATOR ONLY
        if (deleteButton != null)
        {
            deleteButton.gameObject.SetActive(isCreator);
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteTournament);
        }

        // View leaderboard button - participants or creator only
        bool canViewLeaderboard = isParticipant || isCreator;
        if (viewLeaderboardButton != null)
        {
            viewLeaderboardButton.gameObject.SetActive(canViewLeaderboard);
            viewLeaderboardButton.onClick.RemoveAllListeners();
            viewLeaderboardButton.onClick.AddListener(OnViewLeaderboard);
            
            // Update button text for creator viewing as spectators
            if (isCreator && !isParticipant && viewLeaderboardButton.gameObject.activeSelf)
            {
                var buttonText = viewLeaderboardButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = "View (Creator)";
                }
            }
        }

        // Select Quiz Pack Button - CREATOR ONLY, upcoming tournaments
        if (selectQuizPackButton != null)
        {
            selectQuizPackButton.gameObject.SetActive(
                isCreator && 
                tournamentData.status == TournamentStatus.Upcoming
            );
            selectQuizPackButton.onClick.RemoveAllListeners();
            selectQuizPackButton.onClick.AddListener(OnSelectQuizPack);
        }

        // Play Button - PARTICIPANTS ONLY, active tournaments with quiz pack selected
        bool canPlay = isParticipant && 
                       tournamentData.status == TournamentStatus.Active &&
                       !string.IsNullOrEmpty(tournamentData.selectedQuizPackName);

        if (playButton != null)
        {
            playButton.gameObject.SetActive(canPlay);
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayTournament);
            
            // Check if player already played using tracking system
            if (canPlay)
            {
                bool hasPlayedAlready = TournamentManager.Instance.HasPlayerPlayed(
                    tournamentData.tournamentId, 
                    playerId
                );
                
                if (hasPlayedAlready)
                {
                    // Player has already submitted score
                    playButton.interactable = false;
                    var playButtonText = playButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (playButtonText != null)
                    {
                        playButtonText.text = "Already Played";
                    }
                    var img = playButton.GetComponent<Image>();
                    if (img != null) img.color = Color.gray;
                }
                else
                {
                    // Player hasn't played yet - allow them to play
                    playButton.interactable = true;
                    var playButtonText = playButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (playButtonText != null)
                    {
                        playButtonText.text = "PLAY NOW";
                    }
                    var img = playButton.GetComponent<Image>();
                    if (img != null) img.color = Color.green;
                }
            }
        }
    }

    // ==================== BUTTON HANDLERS ====================

    private void OnSelectQuizPack()
    {
        // Verify creator status
        if (!TournamentManager.Instance.IsCreatorOfTournament(tournamentData.tournamentId))
        {
            Debug.LogError("Only the tournament creator can select quiz packs!");
            return;
        }
        
        if (tournamentUI != null)
        {
            tournamentUI.ShowQuizPackSelection(tournamentData.tournamentId);
        }
    }

    private void OnPlayTournament()
    {
        if (mainMenuUI != null)
        {
            mainMenuUI.StartTournamentGame(tournamentData.tournamentId);
        }
        else
        {
            Debug.LogError("MainMenuUI reference not found!");
        }
    }

    private async void OnJoinTournament()
    {
        Debug.Log($"[TournamentItem] Join button clicked");
    
        // Disable button to prevent double-clicks
        if (joinButton != null)
        {
            joinButton.interactable = false;
            var buttonText = joinButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = "Joining...";
        }
    
        string password = tournamentData.isPrivate && passwordInput != null ? passwordInput.text : "";
        bool success = await TournamentManager.Instance.JoinTournament(tournamentData.tournamentId, password);
    
        if (success)
        {
            Debug.Log($"✓ Join successful!");
        
            // Get fresh data (already loaded in JoinTournament)
            var freshData = TournamentManager.Instance.GetTournament(tournamentData.tournamentId);
            if (freshData != null)
            {
                Debug.Log($"Refreshing UI with {freshData.participantUIDs.Count} participants");
                Setup(freshData, tournamentUI);
            }
        }
        else
        {
            Debug.LogError("✗ Join failed!");
        
            // Re-enable join button on failure
            if (joinButton != null)
            {
                joinButton.interactable = true;
                var buttonText = joinButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null) buttonText.text = "Join";
            }
        }
    }


    private async void OnLeaveTournament()
    {
        bool success = await TournamentManager.Instance.LeaveTournament(tournamentData.tournamentId);
        
        if (success)
        {
            Debug.Log($"Left tournament: {tournamentData.tournamentName}");
            
            // FIXED: Get fresh data from TournamentManager
            var freshData = TournamentManager.Instance.GetTournament(tournamentData.tournamentId);
            if (freshData != null)
            {
                Setup(freshData, tournamentUI);
            }
        }
    }

    private async void OnStartTournament()
    {
        // Verify creator status
        if (!TournamentManager.Instance.IsCreatorOfTournament(tournamentData.tournamentId))
        {
            Debug.LogError("Only the tournament creator can start this tournament!");
            return;
        }
        
        // Validation: Can't start without quiz pack
        if (string.IsNullOrEmpty(tournamentData.selectedQuizPackName))
        {
            Debug.LogError("Cannot start tournament without selecting a quiz pack!");
            return;
        }

        bool success = await TournamentManager.Instance.StartTournament(tournamentData.tournamentId);
        
        if (success)
        {
            Debug.Log($"Started tournament manually: {tournamentData.tournamentName}");
            
            // FIXED: Get fresh data from TournamentManager
            var freshData = TournamentManager.Instance.GetTournament(tournamentData.tournamentId);
            if (freshData != null)
            {
                Setup(freshData, tournamentUI);
            }
        }
    }

    private async void OnEndTournament()
    {
        // Verify creator status
        if (!TournamentManager.Instance.IsCreatorOfTournament(tournamentData.tournamentId))
        {
            Debug.LogError("Only the tournament creator can end this tournament!");
            return;
        }
        
        bool success = await TournamentManager.Instance.EndTournament(tournamentData.tournamentId);
        
        if (success)
        {
            Debug.Log($"Ended tournament: {tournamentData.tournamentName}");
            
            // FIXED: Get fresh data from TournamentManager
            var freshData = TournamentManager.Instance.GetTournament(tournamentData.tournamentId);
            if (freshData != null)
            {
                Setup(freshData, tournamentUI);
            }
        }
    }

    private async void OnDeleteTournament()
    {
        // Verify creator status
        if (!TournamentManager.Instance.IsCreatorOfTournament(tournamentData.tournamentId))
        {
            Debug.LogError("Only the tournament creator can delete this tournament!");
            return;
        }
        
        bool success = await TournamentManager.Instance.DeleteTournament(tournamentData.tournamentId);
        
        if (success)
        {
            Debug.Log($"Deleted tournament: {tournamentData.tournamentName}");
            // Tournament is deleted, so destroy this UI item
            Destroy(gameObject);
        }
    }

    private void OnViewLeaderboard()
    {
        if (tournamentUI != null)
        {
            tournamentUI.ShowLeaderboard(tournamentData.tournamentId);
        }
    }
}
