using UnityEngine;
using System.Collections;

public class MartialHeroHealth : MonoBehaviour, IDamageable
{
    public int maxHealth = 100;
    public int currentHealth;
    private Animator animator;
    private SpriteRenderer sr;
public bool isDead { get; private set; } = false;
    private bool hasTransitioned = false;

private MartialHeroBehavior phase1;
private MartialHeroPhase2 phase2;




    private void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        phase1 = GetComponent<MartialHeroBehavior>();
        phase2 = GetComponent<MartialHeroPhase2>();

        if (phase2 != null)
            phase2.enabled = false; 
        
        }

 public void TakeDamage(int amount)
{
    if (isDead || !gameObject.activeInHierarchy)
    {
        Debug.Log("[MartialHeroHealth] Ignoring damage because Samurai is inactive or already dead.");
        return;
    }

    // Safely assign missing components (failsafe)
    if (animator == null)
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("[MartialHeroHealth] Animator still null — skipping animation.");
        }
    }

    if (sr == null)
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogWarning("[MartialHeroHealth] SpriteRenderer still null — skipping flash.");
        }
    }

    Debug.Log("[MartialHeroHealth] Took damage: " + amount);

    if (sr != null && sr.color != Color.blue)
    {
        StartCoroutine(FlashColor(Color.red));
    }

    if (animator != null)
    {
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
    }

    int oldHealth = currentHealth;
    currentHealth -= amount;
    Debug.Log($"[MartialHeroHealth] Took {amount} damage. HP: {oldHealth} → {currentHealth}");

    if (!hasTransitioned && currentHealth <= maxHealth / 2)
    {
        hasTransitioned = true;
        StartCoroutine(StartPhase2Transition());
    }

    if (currentHealth <= 0)
    {
        Die();
    }
}


private IEnumerator StartPhase2Transition()
{
    Debug.Log("[MartialHero] Transitioning to Phase 2");

    if (phase1 != null) phase1.StopAllCoroutines();

    yield return new WaitForSeconds(1.2f); // Adjust based on your animation

    if (phase1 != null) phase1.enabled = false;
    if (phase2 != null) phase2.enabled = true;
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
if (phase1 != null)
    phase1.StopAllCoroutines();
if (phase2 != null)
    phase2.StopAllCoroutines();


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

if (phase1 != null)
    phase1.enabled = false;
if (phase2 != null)
    phase2.enabled = false;

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
