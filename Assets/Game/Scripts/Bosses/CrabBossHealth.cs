using UnityEngine;
using System.Collections;

public class CrabBossHealth : MonoBehaviour, IDamageable
{
    private CrabBossBehavior bossController;

    public int maxHealth = 100;
    private int currentHealth;
    private Animator animator;
    private SpriteRenderer sr;
    private CrabBossBehaviorPhase2 behavior;

    public bool isDead { get; private set; } = false;

    // Optional: hook into immunity from phase script
    public bool IsImmune => behavior != null && behavior.isImmune;

    private void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        behavior = GetComponent<CrabBossBehaviorPhase2>();
        bossController = GetComponent<CrabBossBehavior>();

    }

    public void TakeDamage(int amount)
    {
        if (isDead || IsImmune) return;

        Debug.Log("[CrabBossPhase2] Took damage: " + amount);

        if (sr.color != Color.blue)
        {
            StartCoroutine(FlashColor(new Color(0.6f, 1f, 0.2f)));
        }

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (!state.IsName("Crab_Attack_A") &&
            !state.IsName("Crab_Attack_B") &&
            !state.IsName("Crab_Attack_C") &&
            !state.IsName("Crab_Ability") &&
            !state.IsName("Death"))
        {
            StartCoroutine(PlayTakeHitThenIdle());
        }

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (currentHealth <= maxHealth / 2 && !behavior.enabled)
        {
            // Switch from Phase 1 to Phase 2
            if (bossController != null)
            {
                Debug.Log("[CrabBossPhase2Health] Triggering Phase 2");
                bossController.phase1.enabled = false;
                bossController.phase2.enabled = true;
                bossController.phase2.TriggerPhaseStart();
            }
        }

    }
    private IEnumerator FlashColor(Color color)
    {
        if (sr != null)
        {
            sr.color = color; // flash to a visible contrast
            yield return new WaitForSeconds(0.2f);
            sr.color = Color.white; // reset back to white (as expected)
        }
    }


    private IEnumerator PlayTakeHitThenIdle()
    {
        animator.Play("Crab_Hit");
        yield return new WaitForSeconds(0.25f);
        if (!isDead)
            animator.Play("Crab_Idle");
    }

    private void Die()
    {
        isDead = true;

        if (sr != null)
            sr.color = Color.white;

        // Stop phase 2 logic
        if (behavior != null)
        {
            behavior.StopAllCoroutines();
            behavior.enabled = false;
        }

        // Freeze the boss
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }

        // Disable colliders
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (var c in colliders) c.enabled = false;

        // activate death handler
        CrabBossDeathHandler deathHandler = GetComponent<CrabBossDeathHandler>();
        if (deathHandler != null)
        {
            Debug.Log("[CrabBoss] Triggering DeathHandler");
            deathHandler.enabled = true;
            deathHandler.TriggerDeathSequence();
        }
        else
        {
            Debug.LogError("[CrabBoss] No DeathHandler found!");
        }
    }
    
}