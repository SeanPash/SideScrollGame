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
        if (currentHealth == 0)
        {
            Die();
        }
        else
        {
            animator.Play("Hurt");
        }
    }

    public void Die()
    {
        isDead = true;
        animator.Play("Death");
    }
}
