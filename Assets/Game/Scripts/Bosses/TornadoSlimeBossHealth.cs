using UnityEngine;
using System.Collections;

// Health for the Tornado Slime Boss. Sole owner of its HP. Implements IDamageable
// for player hits and IBossHealth for the phase manager.
public class TornadoSlimeBossHealth : MonoBehaviour, IDamageable, IBossHealth
{
    [Header("Health")]
    public int maxHealth = 15;
    private int currentHealth;
    private bool isDead = false;
    private bool isInvulnerable = false;

    [Header("References")]
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public TornadoSlimeBossBehavior tornadoBehavior;
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
        Debug.Log("Tornado Slime took damage. Current HP: " + currentHealth);

        // The tornado controller has no hit state; the red flash is the hit
        // feedback.
        StartCoroutine(FlashRed());

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

        if (tornadoBehavior != null)
        {
            tornadoBehavior.isDead = true;
        }

        rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            // The controller has no death state; Sleep doubles as the
            // collapsed death pose.
            animator.Play("Sleep");
            Debug.Log("Tornado Slime death animation playing...");
        }

        yield return new WaitForSeconds(0.6f);

        if (tornadoBehavior != null)
        {
            tornadoBehavior.StopAllCoroutines();
            tornadoBehavior.enabled = false;
        }

        Destroy(gameObject);
    }
}
