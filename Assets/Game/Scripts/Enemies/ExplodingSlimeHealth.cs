using UnityEngine;
using System.Collections;

// Health for the exploding mini slime. Implements IDamageable so player attacks
// register. A killing blow detonates the slime immediately.
public class ExplodingSlimeHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public int maxHealth = 2;
    private int currentHealth;
    private bool isDead = false;

    [Header("Damage Flash")]
    public Color flashColor = Color.red;
    public float flashDuration = 0.1f;
    private Color originalColor;

    private ExplodingSlimeBehavior behavior;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    void Awake()
    {
        behavior = GetComponent<ExplodingSlimeBehavior>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    // IDamageable: player hits. Killing the slime detonates it on the spot.
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            isDead = true;
            if (behavior != null) behavior.ExplodeImmediately();
            return;
        }

        StartCoroutine(FlashRed());
        if (animator != null) animator.Play("Enemy Hit");
    }

    // Brief red flash on hit.
    private IEnumerator FlashRed()
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }
}
