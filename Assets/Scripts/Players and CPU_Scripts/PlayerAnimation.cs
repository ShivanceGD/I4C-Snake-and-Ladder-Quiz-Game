using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    public enum PlayerState
    {
        Idle, Walking
    }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Switch animation and rotate sprite depending on state and board index.
    /// </summary>
    public void SetPlayerAnimation(PlayerState state, int currentIndex)
    {
        // --- Set animation state
        animator.SetBool("isWalking", state == PlayerState.Walking);

        // --- Rotation based on row direction (snakes & ladders zig-zag)
        int tilesPerRow = 10;
        int row = currentIndex / tilesPerRow;

        bool facingLeft = (row % 2 == 0);

        // Rotate 180° around Y for left, 0° for right
        transform.localRotation = Quaternion.Euler(0f, facingLeft ? 180f : 0f, 0f);
    }
}
