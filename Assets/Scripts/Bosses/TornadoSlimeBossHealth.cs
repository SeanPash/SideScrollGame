using UnityEngine;
using System.Collections;

public class TornadoSlimeBossHealth : MonoBehaviour, IDamageable
{
    public int maxHealth = 15;
    private int currentHealth;

    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public TornadoSlimeBossBehavior tornadoBehavior;

    public Color flashColor = Color.red;
    public float flashDuration = 0.1f;

    private Color originalColor;
    private bool isDead = false;
    public Rigidbody2D rb;

    void Start()
    {
        currentHealth = maxHealth;
        originalColor = spriteRenderer.color;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log("Tornado Slime took damage. Current HP: " + currentHealth);

        StartCoroutine(FlashRed());

        if (!tornadoBehavior.isAttacking)
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

    private IEnumerator FlashRed()
    {
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }

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
            animator.Play("Enemy Death");
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
