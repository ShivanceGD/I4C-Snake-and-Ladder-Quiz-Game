using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TournamentUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject tournamentListPanel;
    public GameObject createTournamentPanel;
    public GameObject leaderboardPanel;
    public GameObject accessDeniedPanel;
    
    [Header("Tournament List")]
    public Transform tournamentContainer;
    public GameObject tournamentItemPrefab;
    public Button refreshButton;
    public TextMeshProUGUI refreshStatusText; // Shows "Loading..." status
    
    [Header("Create Tournament UI")]
    public TMP_InputField nameInput;
    
    // SIMPLIFIED TIME SELECTION
    public TMP_Dropdown startTimeDropdown;
    public TMP_Dropdown durationDropdown;
    public TextMeshProUGUI previewTimeText;
    
    // QUIZ PACK SELECTION IN CREATE PANEL
    public TMP_Dropdown quizPackDropdown;
    public TextMeshProUGUI quizPackInfoText;
    
    public Toggle privateToggle;
    public TMP_InputField passwordInput;
    public TMP_InputField maxPlayersInput;
    public Button createButton;
    public Button cancelCreateButton;
    
    [Header("Admin UI")]
    public GameObject adminPanel;
    public Button showCreatePanelButton;
    public TextMeshProUGUI adminStatusText;
    
    [Header("Leaderboard UI")]
    public Transform leaderboardContainer;
    public GameObject leaderboardEntryPrefab;
    public TextMeshProUGUI leaderboardTitleText;
    public Button closeLeaderboardButton;
    public Button refreshLeaderboardButton;
    public TextMeshProUGUI playerRankText;
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI participantStatusText;
    
    [Header("Access Denied UI")]
    public TextMeshProUGUI accessDeniedMessageText;
    public Button closeAccessDeniedButton;
    
    [Header("References")]
    public MainMenuUI mainMenuUI;
    
    [Header("Auto-Refresh Settings")]
    public float autoRefreshInterval = 60f; // Refresh every 60 seconds
    public TextMeshProUGUI autoRefreshTimerText; // Optional: Shows countdown
    
    private string currentLeaderboardTournamentId;
    private bool isInitialized = false;
    private bool isRefreshing = false;
    private Coroutine autoRefreshCoroutine;
    private float nextRefreshTime;

    private void Start()
    {
        // Find MainMenuUI if not set
        if (mainMenuUI == null)
        {
            mainMenuUI = FindObjectOfType<MainMenuUI>();
        }
        
        // Subscribe to events
        TournamentManager.Instance.OnTournamentsUpdated += RefreshTournamentList;
        TournamentManager.Instance.OnScoreSubmitted += OnScoreSubmitted;
        
        // Subscribe to individual tournament events for live updates
        TournamentManager.Instance.OnTournamentJoined += OnTournamentUpdated;
        TournamentManager.Instance.OnTournamentStarted += OnTournamentUpdated;
        TournamentManager.Instance.OnTournamentEnded += OnTournamentUpdated;
        
        // Setup buttons
        createButton.onClick.AddListener(OnCreateTournament);
        cancelCreateButton.onClick.AddListener(() => createTournamentPanel.SetActive(false));
        showCreatePanelButton.onClick.AddListener(ShowCreatePanel);
        refreshButton.onClick.AddListener(RefreshTournamentsFromServer);
        closeLeaderboardButton.onClick.AddListener(() => leaderboardPanel.SetActive(false));
        refreshLeaderboardButton.onClick.AddListener(RefreshLeaderboard);
        closeAccessDeniedButton.onClick.AddListener(() => accessDeniedPanel.SetActive(false));
        
        // Setup dropdowns
        SetupTimeDropdowns();
        
        // Setup admin UI
        bool isAdmin = TournamentManager.Instance.IsAdmin();

        if (!isAdmin)
        {
            showCreatePanelButton.gameObject.SetActive(false);
        }
        if (adminPanel != null)
        {
            adminPanel.SetActive(isAdmin);
        }
        
        if (adminStatusText != null)
        {
            adminStatusText.text = isAdmin ? "Admin Mode" : "Player Mode";
        }
        
        // Hide all panels initially
        if (tournamentListPanel != null)
        {
            tournamentListPanel.SetActive(false);
        }
        
        if (createTournamentPanel != null)
        {
            createTournamentPanel.SetActive(false);
        }
        
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }
        
        if (accessDeniedPanel != null)
        {
            accessDeniedPanel.SetActive(false);
        }
        
        // Hide refresh status text
        if (refreshStatusText != null)
        {
            refreshStatusText.gameObject.SetActive(false);
        }
        
        // Hide auto-refresh timer initially
        if (autoRefreshTimerText != null)
        {
            autoRefreshTimerText.gameObject.SetActive(false);
        }
        
        isInitialized = true;
        
        // Start auto-refresh
        StartAutoRefresh();
        
        Debug.Log($"TournamentUI initialized. Admin: {isAdmin}");
    }

    private void Update()
    {
        // Update countdown timer display (optional)
        if (autoRefreshTimerText != null && tournamentListPanel != null && tournamentListPanel.activeSelf)
        {
            float timeUntilRefresh = nextRefreshTime - Time.time;
            
            if (timeUntilRefresh > 0)
            {
                int seconds = Mathf.CeilToInt(timeUntilRefresh);
                autoRefreshTimerText.text = $"Auto-refresh in {seconds}s";
                autoRefreshTimerText.color = new Color(1f, 1f, 1f, 0.6f); // Semi-transparent white
                autoRefreshTimerText.gameObject.SetActive(true);
            }
            else if (!isRefreshing)
            {
                autoRefreshTimerText.text = "Refreshing...";
                autoRefreshTimerText.color = Color.yellow;
            }
        }
        else if (autoRefreshTimerText != null)
        {
            autoRefreshTimerText.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Stop auto-refresh
        StopAutoRefresh();
        
        if (TournamentManager.Instance != null)
        {
            TournamentManager.Instance.OnTournamentsUpdated -= RefreshTournamentList;
            TournamentManager.Instance.OnScoreSubmitted -= OnScoreSubmitted;
            
            // Unsubscribe from individual tournament events
            TournamentManager.Instance.OnTournamentJoined -= OnTournamentUpdated;
            TournamentManager.Instance.OnTournamentStarted -= OnTournamentUpdated;
            TournamentManager.Instance.OnTournamentEnded -= OnTournamentUpdated;
        }
    }

    // ==================== AUTO-REFRESH METHODS ====================
    
    private void StartAutoRefresh()
    {
        StopAutoRefresh(); // Stop any existing coroutine
        
        autoRefreshCoroutine = StartCoroutine(AutoRefreshCoroutine());
        nextRefreshTime = Time.time + autoRefreshInterval;
        Debug.Log($"🔄 Auto-refresh started (every {autoRefreshInterval} seconds)");
    }
    
    private void StopAutoRefresh()
    {
        if (autoRefreshCoroutine != null)
        {
            StopCoroutine(autoRefreshCoroutine);
            autoRefreshCoroutine = null;
            Debug.Log("Auto-refresh stopped");
        }
    }
    
    private IEnumerator AutoRefreshCoroutine()
    {
        while (true)
        {
            // Wait for the interval
            yield return new WaitForSeconds(autoRefreshInterval);
            
            // Only refresh if tournament panel is active
            if (tournamentListPanel != null && tournamentListPanel.activeSelf && !isRefreshing)
            {
                Debug.Log("🔄 AUTO-REFRESH: Loading tournaments from server...");
                _ = RefreshTournamentsFromServerAsync();
            }
            
            nextRefreshTime = Time.time + autoRefreshInterval;
        }
    }

    // ==================== TOURNAMENT UPDATE METHODS ====================

    // Refresh specific tournament when it's updated
    private void OnTournamentUpdated(string tournamentId)
    {
        Debug.Log($"Tournament updated event received: {tournamentId}");
        
        // Find the tournament item and refresh it with fresh data
        foreach (Transform child in tournamentContainer)
        {
            var item = child.GetComponent<TournamentItem>();
            if (item != null && item.GetTournamentId() == tournamentId)
            {
                var freshData = TournamentManager.Instance.GetTournament(tournamentId);
                if (freshData != null)
                {
                    Debug.Log($"Refreshing tournament item: {freshData.tournamentName}");
                    item.Setup(freshData, this);
                }
                break;
            }
        }
    }

    // ==================== TIME DROPDOWN METHODS ====================

    private void SetupTimeDropdowns()
    {
        // Start time options
        if (startTimeDropdown != null)
        {
            startTimeDropdown.ClearOptions();
            startTimeDropdown.AddOptions(new List<string>
            {
                "Start Now",
                "In 5 minutes",
                "In 15 minutes",
                "In 30 minutes",
                "In 1 hour",
                "In 2 hours",
                "In 6 hours",
                "In 12 hours",
                "In 24 hours"
            });
            startTimeDropdown.value = 3; // Default: In 30 minutes
            startTimeDropdown.onValueChanged.AddListener((value) => UpdateTimePreview());
        }
        
        // Duration options
        if (durationDropdown != null)
        {
            durationDropdown.ClearOptions();
            durationDropdown.AddOptions(new List<string>
            {
                "15 minutes",
                "30 minutes",
                "1 hour",
                "2 hours",
                "3 hours",
                "6 hours",
                "12 hours",
                "24 hours",
                "48 hours"
            });
            durationDropdown.value = 2; // Default: 1 hour
            durationDropdown.onValueChanged.AddListener((value) => UpdateTimePreview());
        }
    }

    private void SetupQuizPackDropdown()
    {
        if (quizPackDropdown == null || mainMenuUI == null || mainMenuUI.availableQuizPacks == null)
        {
            Debug.LogError("Quiz pack dropdown or MainMenuUI reference missing!");
            return;
        }
        
        quizPackDropdown.ClearOptions();
        
        if (mainMenuUI.availableQuizPacks.Length == 0)
        {
            quizPackDropdown.AddOptions(new List<string> { "No Quiz Packs Available" });
            quizPackDropdown.interactable = false;
            
            if (quizPackInfoText != null)
            {
                quizPackInfoText.text = "⚠ Create quiz packs first!";
                quizPackInfoText.color = Color.red;
            }
            return;
        }
        
        // Add quiz pack names from MainMenuUI
        List<string> packNames = new List<string>();
        foreach (var pack in mainMenuUI.availableQuizPacks)
        {
            packNames.Add(pack.name);
        }
        
        quizPackDropdown.AddOptions(packNames);
        quizPackDropdown.interactable = true;
        quizPackDropdown.onValueChanged.RemoveAllListeners();
        quizPackDropdown.onValueChanged.AddListener((value) => UpdateQuizPackInfo());
        
        // Update info for first pack
        UpdateQuizPackInfo();
    }

    private void UpdateQuizPackInfo()
    {
        if (quizPackInfoText == null || quizPackDropdown == null || mainMenuUI == null) return;
        
        if (mainMenuUI.availableQuizPacks.Length == 0)
        {
            quizPackInfoText.text = "No quiz packs available";
            quizPackInfoText.color = Color.red;
            return;
        }
        
        int selectedIndex = quizPackDropdown.value;
        if (selectedIndex < 0 || selectedIndex >= mainMenuUI.availableQuizPacks.Length)
        {
            quizPackInfoText.text = "Invalid selection";
            quizPackInfoText.color = Color.red;
            return;
        }
        
        QuizPackSO selectedPack = mainMenuUI.availableQuizPacks[selectedIndex];
        
        if (selectedPack != null && selectedPack.questions != null)
        {
            int questionCount = selectedPack.questions.Count;
            quizPackInfoText.text = $"✓ {selectedPack.name} ({questionCount} questions)";
            quizPackInfoText.color = Color.green;
        }
        else
        {
            quizPackInfoText.text = $"Selected: {selectedPack.name}";
            quizPackInfoText.color = Color.white;
        }
    }

    private int GetStartDelayMinutes()
    {
        if (startTimeDropdown == null) return 30;
        
        switch (startTimeDropdown.value)
        {
            case 0: return 0;      // Start Now
            case 1: return 5;      // In 5 minutes
            case 2: return 15;     // In 15 minutes
            case 3: return 30;     // In 30 minutes
            case 4: return 60;     // In 1 hour
            case 5: return 120;    // In 2 hours
            case 6: return 360;    // In 6 hours
            case 7: return 720;    // In 12 hours
            case 8: return 1440;   // In 24 hours
            default: return 30;
        }
    }

    private int GetDurationMinutes()
    {
        if (durationDropdown == null) return 60;
        
        switch (durationDropdown.value)
        {
            case 0: return 15;     // 15 minutes
            case 1: return 30;     // 30 minutes
            case 2: return 60;     // 1 hour
            case 3: return 120;    // 2 hours
            case 4: return 180;    // 3 hours
            case 5: return 360;    // 6 hours
            case 6: return 720;    // 12 hours
            case 7: return 1440;   // 24 hours
            case 8: return 2880;   // 48 hours
            default: return 60;
        }
    }

    private void UpdateTimePreview()
    {
        if (previewTimeText == null) return;
        
        DateTime currentIST = TournamentManager.GetCurrentISTTime();
        int startDelayMinutes = GetStartDelayMinutes();
        int durationMinutes = GetDurationMinutes();
        
        DateTime startTimeIST = currentIST.AddMinutes(startDelayMinutes);
        DateTime endTimeIST = startTimeIST.AddMinutes(durationMinutes);
        
        previewTimeText.text = $"Start: {startTimeIST:MMM dd, HH:mm}\nEnd: {endTimeIST:MMM dd, HH:mm} IST";
    }

    // ==================== PANEL CONTROL METHODS ====================

    public void OpenTournamentPanel()
    {
        if (tournamentListPanel != null)
        {
            tournamentListPanel.SetActive(true);
            
            // Load fresh data from server when opening panel
            _ = RefreshTournamentsFromServerAsync();
            
            // Reset auto-refresh timer
            nextRefreshTime = Time.time + autoRefreshInterval;
        }
    }

    public void CloseTournamentPanel()
    {
        if (tournamentListPanel != null)
        {
            tournamentListPanel.SetActive(false);
        }
    }

    private void ShowCreatePanel()
    {
        if (!TournamentManager.Instance.IsAdmin())
        {
            Debug.LogError("Only admins can create tournaments!");
            return;
        }
    
        createTournamentPanel.SetActive(true);
    
        // Set default values
        nameInput.text = "";
        passwordInput.text = "";
        maxPlayersInput.text = "100";
        privateToggle.isOn = false;
        
        // Reset dropdowns to default
        if (startTimeDropdown != null) startTimeDropdown.value = 3; // In 30 minutes
        if (durationDropdown != null) durationDropdown.value = 2;   // 1 hour
        
        // Setup and select default quiz pack
        SetupQuizPackDropdown();
        
        UpdateTimePreview();
    
        Debug.Log("Create panel opened with quiz pack selection");
    }

    // ==================== REFRESH METHODS ====================

    // Synchronous wrapper for button onClick
    private void RefreshTournamentsFromServer()
    {
        // Call the async method without awaiting
        _ = RefreshTournamentsFromServerAsync();
        
        // Reset auto-refresh timer when manually refreshing
        nextRefreshTime = Time.time + autoRefreshInterval;
    }

    // Async method with proper name
    private async System.Threading.Tasks.Task RefreshTournamentsFromServerAsync()
    {
        if (isRefreshing)
        {
            Debug.Log("Already refreshing tournaments...");
            return;
        }
        
        isRefreshing = true;
        
        // Show loading status
        if (refreshStatusText != null)
        {
            refreshStatusText.gameObject.SetActive(true);
            refreshStatusText.text = "Loading from server...";
            refreshStatusText.color = Color.yellow;
        }
        
        // Disable refresh button
        if (refreshButton != null)
        {
            refreshButton.interactable = false;
        }
        
        Debug.Log("=== REFRESHING TOURNAMENTS FROM SERVER ===");
        
        try
        {
            // Load fresh data from Cloud Save
            await TournamentManager.Instance.LoadTournaments();
            
            // Get the fresh data
            var tournaments = TournamentManager.Instance.GetActiveTournaments();
            
            Debug.Log($"✓ Loaded {tournaments.Count} tournaments from server");
            
            // Update UI with fresh data
            RefreshTournamentList(tournaments);
            
            // Show success status
            if (refreshStatusText != null)
            {
                refreshStatusText.text = $"✓ Loaded {tournaments.Count} tournaments";
                refreshStatusText.color = Color.green;
                
                // Hide after 2 seconds
                await System.Threading.Tasks.Task.Delay(2000);
                if (refreshStatusText != null)
                {
                    refreshStatusText.gameObject.SetActive(false);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to refresh tournaments: {e.Message}");
            
            // Show error status
            if (refreshStatusText != null)
            {
                refreshStatusText.text = "✗ Failed to load";
                refreshStatusText.color = Color.red;
                
                // Hide after 3 seconds
                await System.Threading.Tasks.Task.Delay(3000);
                if (refreshStatusText != null)
                {
                    refreshStatusText.gameObject.SetActive(false);
                }
            }
        }
        finally
        {
            // Re-enable refresh button
            if (refreshButton != null)
            {
                refreshButton.interactable = true;
            }
            
            isRefreshing = false;
        }
    }

    private void RefreshTournamentList(List<TournamentData> tournaments)
    {
        if (!isInitialized) return;
        
        Debug.Log($"Refreshing tournament list UI with {tournaments.Count} tournaments");
        
        // Clear existing items
        foreach (Transform child in tournamentContainer)
        {
            Destroy(child.gameObject);
        }

        // Create new items with fresh data
        foreach (var tournament in tournaments)
        {
            GameObject item = Instantiate(tournamentItemPrefab, tournamentContainer);
            TournamentItem itemScript = item.GetComponent<TournamentItem>();
            
            if (itemScript != null)
            {
                itemScript.Setup(tournament, this);
                Debug.Log($"  - Created item for: {tournament.tournamentName} (Status: {tournament.status})");
            }
        }
        
        Debug.Log($"Tournament list refreshed: {tournaments.Count} tournaments displayed");
    }

    // ==================== CREATE TOURNAMENT METHODS ====================

    private async void OnCreateTournament()
    {
        if (!TournamentManager.Instance.IsAdmin())
        {
            Debug.LogError("Only admins can create tournaments!");
            return;
        }
        
        if (string.IsNullOrEmpty(nameInput.text))
        {
            Debug.LogError("Tournament name is required!");
            return;
        }
        
        // Check if quiz packs are available
        if (mainMenuUI == null || mainMenuUI.availableQuizPacks == null || mainMenuUI.availableQuizPacks.Length == 0)
        {
            Debug.LogError("No quiz packs available! Create a quiz pack first.");
            return;
        }
        
        try
        {
            // Calculate times based on dropdown selections
            DateTime currentIST = TournamentManager.GetCurrentISTTime();
            int startDelayMinutes = GetStartDelayMinutes();
            int durationMinutes = GetDurationMinutes();
            
            DateTime startTime = currentIST.AddMinutes(startDelayMinutes);
            DateTime endTime = startTime.AddMinutes(durationMinutes);
            
            int maxPlayers = int.Parse(maxPlayersInput.text);

            if (maxPlayers <= 0)
            {
                Debug.LogError("Max players must be greater than 0!");
                return;
            }

            Debug.Log($"Creating tournament:");
            Debug.Log($"  Name: {nameInput.text}");
            Debug.Log($"  Start: {startTime:yyyy-MM-dd HH:mm} IST");
            Debug.Log($"  End: {endTime:yyyy-MM-dd HH:mm} IST");
            Debug.Log($"  Duration: {durationMinutes} minutes");

            bool success = await TournamentManager.Instance.CreateTournament(
                nameInput.text,
                startTime,
                endTime,
                privateToggle.isOn,
                passwordInput.text,
                maxPlayers
            );

            if (success)
            {
                // Get the newly created tournament and set its quiz pack
                var tournaments = TournamentManager.Instance.GetActiveTournaments();
                var newTournament = tournaments.Find(t => t.tournamentName == nameInput.text);
                
                if (newTournament != null && quizPackDropdown != null)
                {
                    int selectedIndex = quizPackDropdown.value;
                    
                    if (selectedIndex >= 0 && selectedIndex < mainMenuUI.availableQuizPacks.Length)
                    {
                        string selectedQuizPackName = mainMenuUI.availableQuizPacks[selectedIndex].name;
                        
                        // Set quiz pack for the tournament
                        bool quizPackSet = await TournamentManager.Instance.SetTournamentQuizPack(
                            newTournament.tournamentId,
                            selectedQuizPackName
                        );
                        
                        if (quizPackSet)
                        {
                            Debug.Log($"✓ Quiz pack '{selectedQuizPackName}' assigned to tournament");
                        }
                        else
                        {
                            Debug.LogWarning("Failed to set quiz pack");
                        }
                    }
                }
                
                createTournamentPanel.SetActive(false);
                ClearInputs();
                Debug.Log("Tournament created successfully with quiz pack!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating tournament: {e.Message}");
        }
    }

    private void ClearInputs()
    {
        nameInput.text = "";
        passwordInput.text = "";
        maxPlayersInput.text = "100";
        privateToggle.isOn = false;
        
        if (startTimeDropdown != null) startTimeDropdown.value = 3;
        if (durationDropdown != null) durationDropdown.value = 2;
        if (quizPackDropdown != null) quizPackDropdown.value = 0;
        
        UpdateTimePreview();
    }

    // ==================== LEADERBOARD METHODS ====================

    public void ShowLeaderboard(string tournamentId)
    {
        string playerId = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;
        bool isParticipant = TournamentManager.Instance.IsPlayerInTournament(tournamentId, playerId);
        bool isCreator = TournamentManager.Instance.IsCreatorOfTournament(tournamentId);

        if (!isParticipant && !isCreator)
        {
            ShowAccessDenied(tournamentId);
            return;
        }

        currentLeaderboardTournamentId = tournamentId;
        leaderboardPanel.SetActive(true);
        UpdateLeaderboardDisplay();
    }

    private void ShowAccessDenied(string tournamentId)
    {
        var tournament = TournamentManager.Instance.GetTournament(tournamentId);
        
        if (tournament != null)
        {
            accessDeniedMessageText.text = $"Access Denied!\n\nYou must join the tournament '{tournament.tournamentName}' to view its leaderboard.";
        }
        else
        {
            accessDeniedMessageText.text = "Access Denied!\n\nTournament not found.";
        }
        
        accessDeniedPanel.SetActive(true);
        Debug.LogWarning("Leaderboard access denied - player is not a tournament participant");
    }

    private void UpdateLeaderboardDisplay()
    {
        if (string.IsNullOrEmpty(currentLeaderboardTournamentId)) return;

        var tournament = TournamentManager.Instance.GetTournament(currentLeaderboardTournamentId);
        if (tournament == null) return;

        leaderboardTitleText.text = $"{tournament.tournamentName} - Leaderboard";

        string playerId = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;
        bool isParticipant = tournament.participantUIDs.Contains(playerId);
        bool isCreator = TournamentManager.Instance.IsCreatorOfTournament(currentLeaderboardTournamentId);
        
        if (isCreator)
        {
            participantStatusText.text = "Creator";
            participantStatusText.color = Color.cyan;
        }
        else if (isParticipant)
        {
            participantStatusText.text = "Participant";
            participantStatusText.color = Color.green;
        }
        else
        {
            participantStatusText.text = "Spectator";
            participantStatusText.color = Color.yellow;
        }

        foreach (Transform child in leaderboardContainer)
        {
            Destroy(child.gameObject);
        }

        var scores = TournamentManager.Instance.GetLocalLeaderboard(currentLeaderboardTournamentId);

        if (scores.Count == 0)
        {
            GameObject emptyEntry = Instantiate(leaderboardEntryPrefab, leaderboardContainer);
            var rankText = emptyEntry.transform.GetChild(0).Find("Rankings").GetComponent<TextMeshProUGUI>();
            var playerText = emptyEntry.transform.Find("PlayerName").GetComponent<TextMeshProUGUI>();
            var scoreText = emptyEntry.transform.Find("Score_Text").GetComponent<TextMeshProUGUI>();
            
            rankText.text = "-";
            playerText.text = "No scores yet";
            scoreText.text = "-";
        }
        else
        {
            for (int i = 0; i < scores.Count; i++)
            {
                GameObject entry = Instantiate(leaderboardEntryPrefab, leaderboardContainer);
                
                var rankText = entry.transform.GetChild(0).Find("Rankings").GetComponent<TextMeshProUGUI>();
                var playerText = entry.transform.Find("PlayerName").GetComponent<TextMeshProUGUI>();
                var scoreText = entry.transform.Find("Score_Text").GetComponent<TextMeshProUGUI>();

                rankText.text = $"#{scores[i].rank}";
                
                bool isCurrentPlayer = scores[i].playerId == playerId;
                if (isCurrentPlayer)
                {
                    rankText.color = Color.yellow;
                    playerText.color = Color.yellow;
                    scoreText.color = Color.yellow;
                    rankText.fontStyle = FontStyles.Bold;
                    playerText.fontStyle = FontStyles.Bold;
                    scoreText.fontStyle = FontStyles.Bold;
                }
                else
                {
                    rankText.color = Color.white;
                    playerText.color = Color.white;
                    scoreText.color = Color.white;
                    rankText.fontStyle = FontStyles.Normal;
                    playerText.fontStyle = FontStyles.Normal;
                    scoreText.fontStyle = FontStyles.Normal;
                }
                
                if (scores[i].rank == 1)
                {
                    rankText.text = "🥇 #1";
                    rankText.color = new Color(1f, 0.84f, 0f);
                }
                else if (scores[i].rank == 2)
                {
                    rankText.text = "🥈 #2";
                    rankText.color = new Color(0.75f, 0.75f, 0.75f);
                }
                else if (scores[i].rank == 3)
                {
                    rankText.text = "🥉 #3";
                    rankText.color = new Color(0.8f, 0.5f, 0.2f);
                }
                
                playerText.text = isCurrentPlayer ? "You" : scores[i].playerName;
                scoreText.text = scores[i].score.ToString();
            }
        }

        var playerEntry = TournamentManager.Instance.GetPlayerEntry(currentLeaderboardTournamentId, playerId);

        if (playerEntry != null)
        {
            playerRankText.text = $"Your Rank: #{playerEntry.rank}";
            playerScoreText.text = $"Your Score: {playerEntry.score}";
        }
        else
        {
            playerRankText.text = "Not Ranked Yet";
            playerScoreText.text = "No Score Yet";
        }
    }

    public void ShowQuizPackSelection(string tournamentId)
    {
        if (mainMenuUI != null)
        {
            mainMenuUI.ShowTournamentQuizPackSelection(tournamentId);
        }
    }

    private void RefreshLeaderboard()
    {
        if (string.IsNullOrEmpty(currentLeaderboardTournamentId)) return;
        
        UpdateLeaderboardDisplay();
        Debug.Log("Leaderboard refreshed");
    }

    private void OnScoreSubmitted(string tournamentId, string playerId)
    {
        if (currentLeaderboardTournamentId == tournamentId)
        {
            UpdateLeaderboardDisplay();
        }
    }
}
