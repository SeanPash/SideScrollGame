using UnityEngine;

public class RegularGolemHealth : MonoBehaviour
{
    private int maxHealth;
    private int currentHealth;

    private bool isDead;
    private Animator animator;
    private bool isBoss;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;

        if (currentHealth == 0)
        {
            animator.Play("Hurt");
        }
        else
        {
            Die();
        }

    }
    void Die()
    {
        isDead = true;
        animator.Play("Enemy Death");
        Destroy(gameObject, 2f);
    }
}
