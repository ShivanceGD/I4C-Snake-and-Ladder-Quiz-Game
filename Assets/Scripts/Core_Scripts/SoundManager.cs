using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Components")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip stepSound;

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
}