using UnityEngine;
using System.Collections;

public class SlimeBossHealth : MonoBehaviour, IDamageable
{
    public int maxHealth = 15;
    private int currentHealth;

    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public SlimeBossBehavior slimeBossBehavior;

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
    Debug.Log("Slime took damage. Current HP: " + currentHealth);

    // Always flash red
    StartCoroutine(FlashRed());

    // Only play hurt animation if NOT attacking
    if (!slimeBossBehavior.isAttacking)
    {
        // Only play Hurt if not already about to attack
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

    // Tell SlimeBehavior to stop logic (but don't disable it yet)
    if (slimeBossBehavior != null)
    {
        slimeBossBehavior.isDead = true;
    }

    // Stop movement
    rb.linearVelocity = Vector2.zero;

    // Play death animation
    if (animator != null)
    {
        animator.Play("Enemy Death");
        Debug.Log("Playing death animation...");
    }

    // Wait for death animation duration (adjust as needed)
    yield return new WaitForSeconds(.6f);

    // Now stop slime behavior and destroy object
    if (slimeBossBehavior != null)
    {
        slimeBossBehavior.StopAllCoroutines();
        slimeBossBehavior.enabled = false;
    }

    Destroy(gameObject);
}

}
