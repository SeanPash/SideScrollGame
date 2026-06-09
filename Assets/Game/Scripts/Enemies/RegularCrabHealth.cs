using UnityEngine;
using System.Collections;

public class RegularCrabHealth : MonoBehaviour, IDamageable
{
    public int maxHealth = 15;
    private int currentHealth;

    private Animator animator;
    private Color originalColor;

    private SpriteRenderer sr;
    private RegularCrabBehavior behavior;

    public bool isDead { get; private set; } = false;

    private void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        behavior = GetComponent<RegularCrabBehavior>();
        
         if (sr != null)
        originalColor = sr.color;
    }
    

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        Debug.Log($"[RegularCrabHealth] Took {amount} damage. Current health: {currentHealth - amount}");


        currentHealth -= amount;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        bool isAttackingAnim = state.IsName("Enemy Attack 1") || state.IsName("Enemy Attack 2") || state.IsName("Enemy Attack 3");

        if (isAttackingAnim)
        {
        StartCoroutine(FlashWhiteOnly());
        }
        else
        {
        StartCoroutine(FlashWhiteAndHit());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        if (behavior != null) behavior.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        GetComponent<Collider2D>().enabled = false;

        animator.Play("Enemy Die");
        Destroy(gameObject, 2f); 
    }

  private IEnumerator FlashWhiteOnly()
{
    if (sr == null) yield break;
    sr.color = Color.white;
    yield return new WaitForSeconds(0.1f);
    sr.color = originalColor;
}

   private IEnumerator FlashWhiteAndHit()
{
    if (sr == null) yield break;
    sr.color = Color.white;
    animator.Play("Enemy Hit");
    yield return new WaitForSeconds(0.2f);
    sr.color = originalColor;
}
}
