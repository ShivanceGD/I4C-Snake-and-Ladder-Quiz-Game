using System.Collections;
using EasyTransition;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadingSceneManager : MonoBehaviour
{
    public static LoadingSceneManager Instance;

    [Header("Loading Screen UI")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Image progressBar;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TransitionSettings transition;

    [Header("Loading Speed")]
    [SerializeField] private float fillSpeed = 0.3f; // speed of bar (smaller = slower)

    private float fakeProgress = 0f;
    private bool sceneReady = false;

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
        }

        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }

    [ContextMenu("Loading Scene")]
    public void LoadScene(string  sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    public void SetLoadingScreenMessage(string message)
    {
        messageText.text = message;
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        loadingScreen.SetActive(true);
        progressBar.fillAmount = 0;
        progressText.text = "0%";

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        // Scene loads in background, but UI moves at our speed
        while (!operation.isDone)
        {
            // Unity load progress (0 to 0.9)
            if (operation.progress >= 0.9f)
                sceneReady = true;

            // Gradually increase fake progress
            if (!sceneReady)
            {
                // Before scene ready, cap at 90%
                fakeProgress = Mathf.MoveTowards(fakeProgress, 0.9f, fillSpeed * Time.deltaTime);
            }
            else
            {
                // Once ready, let it go to 100%
                fakeProgress = Mathf.MoveTowards(fakeProgress, 1f, fillSpeed * Time.deltaTime);
            }

            UpdateLoadingUI(fakeProgress);

            // When bar reaches 100% AND scene is ready → activate
            if (fakeProgress >= 1f && sceneReady)
            {
                yield return new WaitForSeconds(0.5f); // short pause at 100%
                TransitionManager.Instance().Transition(transition, 0);
                yield return new WaitForSeconds(1f);
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private void UpdateLoadingUI(float progress)
    {
        progressBar.fillAmount = progress;
        progressText.text = Mathf.RoundToInt(progress * 100f) + "%";
    }
}
