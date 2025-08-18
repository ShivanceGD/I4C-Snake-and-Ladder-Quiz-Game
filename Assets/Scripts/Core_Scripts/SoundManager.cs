using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Components")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip stepSound;
    [SerializeField] private AudioClip LadderSound;
    [SerializeField] private AudioClip SnakeSound;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlayStepSound()
    {
        {
            PlayOneShotAudio(stepSound);
        }
    }

    public void PlayOneShotAudio(AudioClip stepSound)
    {
        if (audioSource != null)
        {
            audioSource.PlayOneShot(stepSound);
        }
        else
        {
            Debug.LogWarning("Missing AudioSource in SoundManager.");
        }
    }
    public void PlayLadderSound()
    {
        PlayOneShotAudio(LadderSound);
    }
    public void PlaySnakeSound()
    {
        PlayOneShotAudio(SnakeSound);
    }
}