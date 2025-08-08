/*using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class QuizTimer : MonoBehaviour
{
    [Header("Time")]
    public float ElapsedTime { get; private set; } = 0f;
    public bool IsTimeRunning { get; private set; } = false;

    [Header("Events")]
    public UnityEvent<float> OnTimeUp = new();
    private Coroutine timerCoroutine;


    public void StartTimerWithLimit(float timeLimit)
    {
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);

        ElapsedTime = 0f;
        IsTimeRunning = true;
        timerCoroutine = StartCoroutine(TimerCoroutine(QuizManager.Instance.timerText, timeLimit));
    }

    private IEnumerator TimerCoroutine(TMP_Text TimerText, float timeLimit)
    {
        while (ElapsedTime < timeLimit && IsTimeRunning)
        {
            ElapsedTime += Time.deltaTime;
            TimerText.text = $"Time: {ElapsedTime:F1}s";
            yield return null;
        }

        if (IsTimeRunning)
        {
            IsTimeRunning = false;
            OnTimeUp?.Invoke(ElapsedTime);
        }
    }

    public void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        IsTimeRunning = false;
    }
}
*/

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class QuizTimer : MonoBehaviour
{
    [Header("Time")]
    public float ElapsedTime { get; private set; } = 0f;
    public bool IsTimeRunning { get; private set; } = false;

    [Header("Events")]
    public UnityEvent<float> OnTimeUp = new();

    private Coroutine timerCoroutine;

    public void StartTimerWithLimit(float timeLimit)
    {
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        ElapsedTime = 0f;
        IsTimeRunning = true;
        timerCoroutine = StartCoroutine(TimerCoroutine(QuizManager.Instance.timerText, timeLimit));
    }

    private IEnumerator TimerCoroutine(TMP_Text TimerText, float timeLimit)
    {
        while (ElapsedTime < timeLimit && IsTimeRunning)
        {
            ElapsedTime += Time.deltaTime;
            TimerText.text = $"Time: {ElapsedTime:F1}s";
            yield return null;
        }

        if (IsTimeRunning)
        {
            IsTimeRunning = false;
            OnTimeUp?.Invoke(ElapsedTime);
        }
    }

    public void StopTimer()
    {
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        IsTimeRunning = false;
    }
}
