using UnityEngine;
using System.Collections;

public class MartialHeroHealth : MonoBehaviour, IDamageable
{
    public int maxHealth = 100;
    private int currentHealth;
    private Animator animator;
    private SpriteRenderer sr;
public bool isDead { get; private set; } = false;
    private MartialHeroBehavior behavior;



    private void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        behavior = GetComponent<MartialHeroBehavior>();
    }

  public void TakeDamage(int amount)
{
    if (isDead) return;

    Debug.Log("[MartialHeroHealth] Took damage: " + amount);

    if (sr.color != Color.blue)
    {
        StartCoroutine(FlashColor(Color.red));
    }

    AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
    if (!state.IsName("Attack1") &&
        !state.IsName("Attack2") &&
        !state.IsName("Attack3") &&
        !state.IsName("ChargeAttack") &&
        !state.IsName("Parried") &&
        !state.IsName("Death"))
    {
        StartCoroutine(PlayTakeHitThenIdle());
    }

    currentHealth -= amount;

    if (currentHealth <= 0)
    {
        Die();
    }
}


private IEnumerator PlayTakeHitThenIdle()
{
    animator.Play("TakeHit");
    yield return new WaitForSeconds(0.25f); 
    if (!isDead) 
    animator.Play("Idle");
}





    private void Die()
{
    isDead = true;
        if (sr != null)
            sr.color = Color.white;
if (behavior != null)
            behavior.StopAllCoroutines(); // Force stop ongoing attack coroutines

if (animator != null)
    animator.Play("Death");

Rigidbody2D rb = GetComponent<Rigidbody2D>();
if (rb != null)
{
    rb.linearVelocity = Vector2.zero;
    rb.bodyType = RigidbodyType2D.Static;
}

Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
foreach (var c in colliders) c.enabled = false;

if (behavior != null)
    behavior.enabled = false;

Destroy(gameObject, 1.5f);

}



    private IEnumerator FlashColor(Color color)
    {
        if (sr != null)
        {
            sr.color = color;
            yield return new WaitForSeconds(0.2f);
            sr.color = Color.white;
        }
    }

}
