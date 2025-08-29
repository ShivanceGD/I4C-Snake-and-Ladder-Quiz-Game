using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Step Movement")]
    public float stepDuration = 0.18f;
    public LeanTweenType stepEase = LeanTweenType.linear;

    [Header("Landing Bounce")]
    [Range(1f, 1.6f)] public float landingScale = 1.12f;
    public float landingDuration = 0.12f;
    public bool usePunchEase = true; // set true for quick juicy bounce

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    public IEnumerator MovePlayerTileByTile(int steps)
    {
        if (steps <= 0) yield break;

        // Make sure we don't stack tweens on the same object
        LeanTween.cancel(gameObject);

        for (int i = 0; i < steps; i++)
        {
            int currentIdx = BoardLogicManager.GetTileIndexFromPosition(transform.position);
            int nextIdx = Mathf.Min(BoardLogicManager.GetWinningTileIndex, currentIdx + 1);
            Vector3 target = BoardLogicManager.GetTilePosition(nextIdx);

            yield return MoveTo(target, stepDuration);
            yield return PlayLandingBounce();
        }
    }

    public IEnumerator MovePlayerDirectlyToTile(int targetIndex)
    {
        Vector3 target = BoardLogicManager.GetTilePosition(targetIndex);
        float duration = Mathf.Max(0.12f, stepDuration * 1.2f);

        LeanTween.cancel(gameObject);
        yield return MoveTo(target, duration);
        yield return PlayLandingBounce();
    }

    private IEnumerator MoveTo(Vector3 target, float duration)
    {
        bool done = false;
        LeanTween.move(gameObject, target, duration)
                 .setEase(stepEase)
                 .setOnComplete(() => done = true);

        while (!done) yield return null;
    }

    private IEnumerator PlayLandingBounce()
    {
        // Reset scale before bounce to avoid cumulative growth
        transform.localScale = baseScale;

        bool finished = false;

        if (usePunchEase)
        {
            // Overshoot and return to baseScale
            LeanTween.scale(gameObject, baseScale * landingScale, landingDuration)
                     .setEasePunch()
                     .setOnComplete(() => finished = true);
        }
        else
        {
            // Manual up then back for a softer bounce
            float half = landingDuration * 0.5f;
            LeanTween.scale(gameObject, baseScale * landingScale, half)
                     .setEase(LeanTweenType.easeOutCubic)
                     .setOnComplete(() =>
                     {
                         LeanTween.scale(gameObject, baseScale, half)
                                  .setEase(LeanTweenType.easeOutBack)
                                  .setOnComplete(() => finished = true);
                     });
        }

        while (!finished) yield return null;
    }
}
