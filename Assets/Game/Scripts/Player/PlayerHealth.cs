using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth;
    public int currentHealth;

    public bool isInvulnerable;
    public bool isDead;
    private Animator animator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = PlayerStats.Instance.maxHealth;
        animator = GetComponent<Animator>();

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void TakeDamage(int damage)
    {
        if (isDead || isInvulnerable) return;

        currentHealth -= damage;
        // Use <= so overkill damage still triggers death.
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            animator.Play("Hurt");
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        animator.Play("Death");
        StartCoroutine(RespawnRoutine());
    }

    // Let the death animation play, then return to the last checkpoint.
    private System.Collections.IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        GameFlow.RespawnAtCheckpoint();
    }

    // Restore the player to full health and clear the dead state (used on respawn).
    public void ReviveFull()
    {
        isDead = false;
        currentHealth = PlayerStats.Instance != null ? PlayerStats.Instance.maxHealth : maxHealth;
        if (animator != null) animator.Play("Idle");
    }
}
