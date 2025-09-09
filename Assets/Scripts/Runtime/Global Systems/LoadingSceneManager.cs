using System.Collections;
using System.Collections.Generic;
using EasyTransition;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class LoadingSceneManager : NetworkBehaviour
{
    public static LoadingSceneManager Instance;

    [Header("Loading Screen UI")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Image progressBar;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TransitionSettings transition;

    [Header("Loading Speed")]
    [SerializeField] private float fillSpeed = 0.3f;

    private float fakeProgress = 0f;
    private bool sceneReady = false;
    private bool firstLoad = true;

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadComplete;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadComplete;
    }

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        ResetLoadingUI();

        // Hide on very first boot
        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }

    [ContextMenu("Loading Scene")]
    public void LoadofflineScene(string sceneName)
    {
        StartCoroutine(LoadOfflineScene(sceneName));
    }

    public void LoadOnlineScene(string sceneName)
    {
        StartCoroutine(LoadMultiplayerScene(sceneName));
    }

    public void SetLoadingScreenMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }

    private IEnumerator LoadOfflineScene(string sceneName)
    {
        ResetLoadingUI();
        loadingScreen.SetActive(true);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            if (operation.progress >= 0.9f)
                sceneReady = true;

            // Progress target
            float targetProgress = sceneReady ? 1f : 0.9f;
            fakeProgress = Mathf.MoveTowards(fakeProgress, targetProgress, fillSpeed * Time.deltaTime);

            UpdateLoadingUI(fakeProgress);

            if (fakeProgress >= 1f && sceneReady)
            {
                yield return new WaitForSeconds(0.5f); // Pause at 100%
                TransitionManager.Instance().Transition(transition, 0);
                yield return new WaitForSeconds(1f);
                operation.allowSceneActivation = true;
                loadingScreen.SetActive(false);
            }

            yield return null;
        }

        firstLoad = false;
    }
    private IEnumerator LoadMultiplayerScene(string sceneName)
    {
        
        if (!NetworkManager.Singleton.IsServer)
            yield break; // Only server loads the scene

        ResetLoadingUI();
        loadingScreen.SetActive(true);

        var status = NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogError($"Failed to start loading scene {sceneName}. Status: {status}");
            yield break;
        }

        // Fake progress until NGO tells us scene is loaded
        fakeProgress = 0f;
        sceneReady = false;

        while (!sceneReady)
        {
            fakeProgress = Mathf.MoveTowards(fakeProgress, 0.9f, fillSpeed * Time.deltaTime);
            UpdateLoadingUI(fakeProgress);
            yield return null;
        }

        // Once OnSceneLoadComplete is called, finish progress
        while (fakeProgress < 1f)
        {
            fakeProgress = Mathf.MoveTowards(fakeProgress, 1f, fillSpeed * Time.deltaTime);
            UpdateLoadingUI(fakeProgress);
            yield return null;
        }

        yield return new WaitForSeconds(0.5f); // pause at 100%
        TransitionManager.Instance().Transition(transition, 0);
        yield return new WaitForSeconds(1f);
        loadingScreen.SetActive(false);
        GetComponent<ClampUIWithinSafeArea>().FindButtons();

        firstLoad = false;
    }
    private void OnSceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode,
        List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        Debug.Log($"Scene {sceneName} loaded. Clients completed: {clientsCompleted.Count}");
        sceneReady = true;
        
    }

    private void UpdateLoadingUI(float progress)
    {
        if (progressBar != null)
        {
            // Cancel old tween if still running
            LeanTween.cancel(progressBar.gameObject);

            // Tween fillAmount toward target
            LeanTween.value(progressBar.gameObject, progressBar.fillAmount, progress, 0.4f)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnUpdate((float val) =>
                {
                    progressBar.fillAmount = val;
                });
        }

        if (progressText != null)
        {
            int percent = Mathf.RoundToInt(progress * 100f);
            progressText.text = percent + "%";
        }
    }


    private void ResetLoadingUI()
    {
        fakeProgress = 0f;
        sceneReady = false;

        if (progressBar != null)
            progressBar.fillAmount = 0f;

        if (progressText != null)
            progressText.text = "0%";
    }
}
