    using UnityEngine;
    using System.Collections;

public class PurpleWizardBehavior : MonoBehaviour, IDamageable
{
    public float detectionRange = 8f;
    public float attackRange = 1.5f;
    public float moveSpeed = 2f;
    public float maxHealth = 30f;


    public Transform player;
    public Animator animator;
    public Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;


    private bool isAttacking = false;
    private bool useFirstAttack = false;

    private float currentHealth;
    private bool isHurting = false;
    private bool isDead = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;
        StartCoroutine(WizardBehaviorLoop());

    }

    // Update is called once per frame
    IEnumerator WizardBehaviorLoop()
    {
        while (true)
        {
            if (isHurting || isAttacking || isDead) yield break;
            if (isDead) yield break;
            if (player == null)
            {
                Idle();
                yield return null;
                continue;
            }

            float distance = Vector2.Distance(transform.position, player.position);
            FacePlayer();

            if (distance > detectionRange)
            {
                Idle();
                yield return null;
            }
            else if (distance > attackRange)
            {
                MoveTowardPlayer();
                yield return null;
            }
            else
            {
                // Attack once
                rb.linearVelocity = Vector2.zero;
                animator.Play(useFirstAttack ? "Attack1" : "Attack2");
                useFirstAttack = !useFirstAttack;

                // Wait for animation to complete (~1s), adjust as needed
                yield return new WaitForSeconds(.7f);

                // Go back to Idle
                Idle();

                // Wait before next attack
                float elapsed = 0f;
                while (elapsed < 1f)
                {
                    elapsed += Time.deltaTime;

                    // If player left attack range, break early
                    float newDist = Vector2.Distance(transform.position, player.position);
                    if (newDist > attackRange)
                        break;

                    yield return null;
                }
            }
        }
    }


    void Idle()
    {
            if (isDead) return;
        rb.linearVelocity = Vector2.zero;
        animator.Play("Idle");
    }

    void MoveTowardPlayer()
    {
            if (isDead) return;
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
        animator.Play("Run");
    }

    void FacePlayer()
    {
        Vector3 scale = transform.localScale;
        if (player.position.x < transform.position.x)
        {
            scale.x = -Mathf.Abs(scale.x);
        }
        else
        {
            scale.x = Mathf.Abs(scale.x);
        }
        transform.localScale = scale;
    }
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"[PurpleWizard] Took {damage} damage. Current HP: {currentHealth}");

        bool isInAttackAnim = IsInAnimation("Attack1") || IsInAnimation("Attack2");

        StartCoroutine(HurtEffect(isInAttackAnim));

        if (currentHealth <= 0)
        {
            StartCoroutine(HandleDeath());
        }
    }
    IEnumerator HurtEffect(bool skipHitAnim)
    {
        isHurting = true;
        spriteRenderer.color = Color.red;

        if (!skipHitAnim)
            animator.Play("Hit");

        yield return new WaitForSeconds(0.2f);
        spriteRenderer.color = Color.white;

        isHurting = false;
    }
    IEnumerator HandleDeath()
    {
        isDead = true;
            isAttacking = false;
        rb.linearVelocity = Vector2.zero;

        animator.Play("Death");

        yield return new WaitForSeconds(1f); 

        Destroy(gameObject);
    }

    bool IsInAnimation(string name)
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsName(name);
    }
    
}
