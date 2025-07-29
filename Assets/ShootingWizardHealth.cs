using UnityEngine;
using System.Collections;

public class ShootingWizardHealth : MonoBehaviour, IDamageable
{
    public int maxHealth = 6;
    private int currentHealth;
    private Animator animator;
    private SpriteRenderer sr;

    public bool isDead { get; private set; } = false;

    private void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        StartCoroutine(FlashColor(Color.red));

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        animator.Play("Death", 0, 0f);

        ShootingWizardBehavior behavior = GetComponent<ShootingWizardBehavior>();
        if (behavior != null)
            behavior.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        GetComponent<Collider2D>().enabled = false;

        Destroy(gameObject, 2f);


    }

    private IEnumerator FlashColor(Color color)
    {
        sr.color = color;
        yield return new WaitForSeconds(0.1f);
        sr.color = Color.white;
    }
}
