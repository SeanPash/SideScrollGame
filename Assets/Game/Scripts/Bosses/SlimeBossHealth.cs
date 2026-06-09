using UnityEngine;
using System.Collections;

// Health for the Slime Boss. Sole owner of its HP. Implements IDamageable for
// player hits and IBossHealth for the phase manager.
public class SlimeBossHealth : MonoBehaviour, IDamageable, IBossHealth
{
    [Header("Health")]
    public int maxHealth = 15;
    private int currentHealth;
    private bool isDead = false;
    private bool isInvulnerable = false;

    [Header("References")]
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public SlimeBossBehavior slimeBossBehavior;
    public Rigidbody2D rb;

    [Header("Damage Flash")]
    public Color flashColor = Color.red;
    public float flashDuration = 0.1f;
    private Color originalColor;

    // IBossHealth: health fraction for phase threshold checks.
    public float HealthPercent => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

    // IBossHealth: true once the death sequence has started.
    public bool IsDead => isDead;

    void Start()
    {
        currentHealth = maxHealth;
        originalColor = spriteRenderer.color;
    }

    // IBossHealth: toggles damage immunity while the boss is benched or hidden.
    public void SetInvulnerable(bool value)
    {
        isInvulnerable = value;
    }

    // IDamageable: applies player damage, flashes red, and dies at zero HP.
    public void TakeDamage(int amount)
    {
        if (isDead || isInvulnerable) return;

        currentHealth -= amount;
        Debug.Log("Slime took damage. Current HP: " + currentHealth);

        StartCoroutine(FlashRed());

        // Only play the hurt animation when not mid-attack.
        if (!slimeBossBehavior.isAttacking)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName("Enemy Attack 1") && !stateInfo.IsTag("Attack"))
            {
                animator.Play("Enemy Hit");
            }
        }

        if (currentHealth <= 0)
        {
            StartCoroutine(Die());
        }
    }

    // Brief red flash on hit.
    private IEnumerator FlashRed()
    {
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }

    // Stops the behavior, plays the death animation, then destroys the boss.
    private IEnumerator Die()
    {
        isDead = true;

        if (slimeBossBehavior != null)
        {
            slimeBossBehavior.isDead = true;
        }

        rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.Play("Enemy Death");
            Debug.Log("Playing death animation...");
        }

        yield return new WaitForSeconds(.6f);

        if (slimeBossBehavior != null)
        {
            slimeBossBehavior.StopAllCoroutines();
            slimeBossBehavior.enabled = false;
        }

        Destroy(gameObject);
    }
}
