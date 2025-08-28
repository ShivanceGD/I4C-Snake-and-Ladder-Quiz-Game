using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class QuizTimer : MonoBehaviour
{
    public float ElapsedTime { get; private set; }
    public bool TimeBackward;
    public UnityEvent<float> OnTimeUp = new UnityEvent<float>();

    private Coroutine routine;

    public void StartTimer(float duration, System.Action<float> onUpdate)
    {
        StopTimer();
        routine = StartCoroutine(Run(duration, onUpdate));
    }

    private IEnumerator Run(float duration, System.Action<float> onUpdate)
    {
        if (TimeBackward)
        {
            ElapsedTime = duration;
            while (ElapsedTime > 0f)
            {
                ElapsedTime -= Time.deltaTime;
                if (ElapsedTime < 0) ElapsedTime = 0;
                onUpdate?.Invoke(ElapsedTime);
                yield return null;
            }
        }
        else
        {
            ElapsedTime = 0f;
            while (ElapsedTime < duration)
            {
                ElapsedTime += Time.deltaTime;
                if (ElapsedTime > duration) ElapsedTime = duration;
                onUpdate?.Invoke(ElapsedTime);
                yield return null;
            }
        }
        OnTimeUp?.Invoke(ElapsedTime);
    }

    public void StopTimer()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
    }
}
