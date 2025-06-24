using UnityEngine;
using System.Collections;

public class BossGolemBehavior : MonoBehaviour, IDamageable
{
    public enum GolemForm { Base, Mid, Reinforced }

    public GolemForm currentForm = GolemForm.Base;

    [Header("References")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public Transform player;
    public Rigidbody2D rb;

    [Header("Animator Controllers")]
    public RuntimeAnimatorController baseFormController;
    public RuntimeAnimatorController midFormController;
    public RuntimeAnimatorController reinforcedController;

    [Header("Idle Sprites")]
    public Sprite baseIdleSprite;
    public Sprite midIdleSprite;
    public Sprite reinforcedIdleSprite;

    [Header("Combat Settings")]
    public float moveSpeed = 2f;
    public float detectionRange = 8f;
    public float attackRange = 1.2f;
    public float shootRange = 5.5f;
    public float attackCycleDuration = 1f;
    public GameObject projectilePrefab;
    public Transform shootPoint;

    [Header("Boss Stats")]
    public float maxHealth = 100f;
    private float currentHealth;
    private bool isInvulnerable = false;

    private bool isAttacking = false;
    private bool isInAttackCycle = false;
    private float attackTimer = 0f;
    private bool useFirstAttack = true;
    private bool hasRockAttacked = false;
    private bool isMoving = false;
    private bool hasSwitchedToMidOnce = false;
    public bool isHurting = false;




    void Start()
    {
        currentHealth = maxHealth;
        SwitchToForm(GolemForm.Base);
    }

    void Update()
    {
        if (!IsPlayerInSight() || isHurting) return;

        float dist = Vector2.Distance(transform.position, player.position);
        FacePlayer();

        if (currentForm == GolemForm.Base)
        {
            if (dist > attackRange)
            {
                MoveTowardPlayer();
            }
            else if (!hasRockAttacked && !isAttacking && !hasSwitchedToMidOnce)
            {
                StartCoroutine(RockAttackThenTransform());
            }

            AnimateMoveState();

            if (currentHealth <= maxHealth / 2f)
            {
                StartCoroutine(TransitionToReinforcedPhase());
            }
        }
        else if (currentForm == GolemForm.Mid)
        {
            if (!isInAttackCycle && !isAttacking)
            {
                if (dist <= attackRange)
                {
                    StartCoroutine(AttackCycle());
                }
                else
                {
                    StartCoroutine(ResetToBaseForm()); // back to Base form if player left range
                }
            }
        }


        else if (currentForm == GolemForm.Reinforced)
        {
            HandleReinforcedBehavior(dist);
        }
    }


    // --- Phase 1 & 2 Behavior ---
    void MoveTowardPlayer()
    {
        if (isAttacking || isInAttackCycle || isHurting) return;

        Vector2 dir = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);
        AnimateMoveState();

    }

    IEnumerator RockAttackThenTransform()
    {
        isAttacking = true;
        hasRockAttacked = true;
        rb.linearVelocity = Vector2.zero;

        animator.Play("Enemy Attack 1");
        yield return new WaitForSeconds(1f);

        hasSwitchedToMidOnce = true;
        SwitchToForm(GolemForm.Mid);
        isAttacking = false;
        AnimateMoveState();

    }

    IEnumerator AttackCycle()
    {
        if (isHurting) yield break;
        isInAttackCycle = true;
        isAttacking = true;
        attackTimer = attackCycleDuration;

        while (attackTimer > 0f)
        {
            float dist = Vector2.Distance(transform.position, player.position);

            if (dist <= attackRange)
            {
                rb.linearVelocity = Vector2.zero;
                animator.Play(useFirstAttack ? "Enemy Attack 2" : "Enemy Attack 3");
                useFirstAttack = !useFirstAttack;

                yield return new WaitForSeconds(1f); // attack animation delay
                attackTimer -= 1f;
            }
            else
            {
                yield return null;
            }
        }

        isAttacking = false;
        isInAttackCycle = false;


    }

    IEnumerator ResetToBaseForm()
    {

        if (currentForm != GolemForm.Mid || !hasSwitchedToMidOnce)
            yield break;
        isAttacking = true;
        animator.Play("Enemy Ability");
        yield return new WaitForSeconds(0.3f);

        SwitchToForm(GolemForm.Base);
        hasRockAttacked = false;
        hasSwitchedToMidOnce = false;
        isAttacking = false;
        isInAttackCycle = false;

        // Optional: check immediately if player is already in range again
        if (IsPlayerInSight() && Vector2.Distance(transform.position, player.position) > attackRange)
        {
            MoveTowardPlayer();
        }
    }



    // --- Phase 3: Reinforced Phase ---
    IEnumerator TransitionToReinforcedPhase()
    {
        currentForm = GolemForm.Reinforced;
        isInvulnerable = true;
        rb.linearVelocity = Vector2.zero;
        animator.Play("Enemy Ability");

        yield return new WaitForSeconds(1.2f);

        SwitchToForm(GolemForm.Reinforced);
        isInvulnerable = false;
    }

    void HandleReinforcedBehavior(float dist)
    {
        if (isHurting) return;
        attackTimer -= Time.deltaTime;

        if (dist <= attackRange)
        {
            rb.linearVelocity = Vector2.zero;

            if (attackTimer <= 0f)
            {
                animator.Play(useFirstAttack ? "Enemy Attack 1" : "Enemy Attack 2");
                useFirstAttack = !useFirstAttack;
                attackTimer = 1.5f;
            }
        }
        else if (dist <= shootRange)
        {
            rb.linearVelocity = Vector2.zero;

            if (attackTimer <= 0f)
            {
                animator.Play("Enemy Attack 3");
                attackTimer = 2f;
                ShootProjectile();
            }
        }
        else
        {
            Vector2 dir = (player.position - transform.position).normalized;
            rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);
            animator.Play("Enemy Run");
        }
    }

    void ShootProjectile()
    {
        if (projectilePrefab && shootPoint)
        {
            GameObject proj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);
            Vector2 dir = (player.position - shootPoint.position).normalized;
            proj.GetComponent<ProjectileBehaivor>().SetDirection(dir);
        }
    }

    // --- Utilities ---
    void AnimateMoveState()
    {
        bool currentlyMoving = Mathf.Abs(rb.linearVelocity.x) > 0.01f;
        if (currentlyMoving != isMoving)
        {
            isMoving = currentlyMoving;
            animator.Play(isMoving ? "Enemy Run" : "Enemy Idle", 0);
        }
    }

    void SwitchToForm(GolemForm newForm)
    {
        currentForm = newForm;

        switch (newForm)
        {
            case GolemForm.Base:
                animator.runtimeAnimatorController = baseFormController;
                spriteRenderer.sprite = baseIdleSprite;
                hasRockAttacked = false;
                break;
            case GolemForm.Mid:
                animator.runtimeAnimatorController = midFormController;
                spriteRenderer.sprite = midIdleSprite;
                break;
            case GolemForm.Reinforced:
                animator.runtimeAnimatorController = reinforcedController;
                spriteRenderer.sprite = reinforcedIdleSprite;
                break;
        }
    }


    void FacePlayer()
    {
        Vector3 localScale = transform.localScale;

        if (player.position.x < transform.position.x)
            localScale.x = -Mathf.Abs(localScale.x);
        else
            localScale.x = Mathf.Abs(localScale.x);

        transform.localScale = localScale;
    }

    bool IsPlayerInSight()
    {
        return Vector2.Distance(transform.position, player.position) <= detectionRange;
    }

    bool IsInAnimation(string name)
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsName(name);
    }

   public void TakeDamage(int damage)
{
        if (isInvulnerable)
        {
                    Debug.Log("[BossGolem] Ignored damage: Invulnerable.");
            return;
        }
    currentHealth -= damage;
    Debug.Log($"Boss took {damage} damage. Current HP: {currentHealth}");

    StartCoroutine(PlayHurtEffect());

    if (currentHealth <= maxHealth / 2f && currentForm != GolemForm.Reinforced)
    {
                Debug.Log("[BossGolem] Triggering transition to reinforced phase.");
        StartCoroutine(TransitionToReinforcedPhase());
    }

    if (currentHealth <= 0)
    {
        Debug.Log("[BossGolem] Triggering death.");
        animator.Play("Enemy Death, 0");
        rb.linearVelocity = Vector2.zero;
        this.enabled = false;
    }
}

    IEnumerator PlayHurtEffect()
    {
        isHurting = true;

        // Flash red
        spriteRenderer.color = Color.red;

        // Play the correct "Enemy Hit" animation depending on the current form
        if (currentForm == GolemForm.Base || currentForm == GolemForm.Mid)
        {
            animator.Play("Enemy Hit");
        }
        else if (currentForm == GolemForm.Reinforced)
        {
            animator.Play("Enemy Hurt"); // or use "Enemy Hit" if same naming
        }

        yield return new WaitForSeconds(0.2f);

        spriteRenderer.color = Color.white;
        isHurting = false;

        // Optional: return to idle manually if not handled in Animator transitions
        if (!isAttacking && !isInAttackCycle)
        {
            animator.Play("Enemy Idle");
        }
    }


}
