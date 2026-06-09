using UnityEngine;
using System.Collections;

public class RegularGolemBehavior : MonoBehaviour, IDamageable, IAttackState
{
    public bool IsAttackingNow { get; private set; } = false; 
    public enum GolemForm { Base, Mid }

    public GolemForm currentForm = GolemForm.Base;
    [Header("Stats")]
    public float maxHealth = 30f;
    private float currentHealth;
    private bool isDead = false;
    public bool isHurting = false;

    [Header("Refs")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public Transform player;
    public Rigidbody2D rb;

    [Header("Animator Controllers")]
    public RuntimeAnimatorController baseFormController;
    public RuntimeAnimatorController form2Controller;

    [Header("Idle Sprites")]
    public Sprite baseIdleSprite;
    public Sprite form2IdleSprite;

    [Header("Combat Settings")]
    public float moveSpeed = 2f;
    public float detectionRange = 8f;
    public float attackRange = .2f;
    public float attackCycleDuration = 1f;

    private bool isAttacking = false;
    private bool isInAttackCycle = false;
    private float attackTimer = 0f;
    private bool useFirstAttack = true;
    private bool hasRockAttacked = false;
    private bool isMoving = false;
    private RegularGolemStun stunHandler;




    void Start()
    {
        currentHealth = maxHealth;
        SwitchToForm(GolemForm.Base);
        stunHandler = GetComponent<RegularGolemStun>();
    }

    void Update()
    {
        if ((stunHandler != null && stunHandler.IsStunned()))
            return;
        if (isDead || isHurting || !IsPlayerInSight()) return;
        if (!IsPlayerInSight()) return;

        float dist = Vector2.Distance(transform.position, player.position);
        FacePlayer();

        if (currentForm == GolemForm.Base)
        {
            // Determine if should move
            if (dist > attackRange)
            {
                MoveTowardPlayer();
            }
            else if (!hasRockAttacked && !isAttacking)
            {
                StartCoroutine(RockAttackThenTransform());
            }

            // Animate only on change
            bool currentlyMoving = Mathf.Abs(rb.linearVelocity.x) > 0.2f;

            if (currentlyMoving != isMoving)
            {
                isMoving = currentlyMoving;

                if (isMoving)
                {
                    animator.Play("Enemy Run", 0);
                }
                else
                {
                    animator.Play("Enemy Idle", 0);
                }
            }
        }
        else if (currentForm == GolemForm.Mid)
        {
            if (!isInAttackCycle)
            {
                StartCoroutine(AttackCycle());
            }
            else
            {
                attackTimer -= Time.deltaTime;

                if (attackTimer <= 0f && dist > attackRange)
                {
                    StartCoroutine(ResetToBaseForm());
                }
            }
        }
    }



    void MoveTowardPlayer()
    {
        if (isAttacking || isInAttackCycle) return;

        Vector2 dir = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);
        if (!IsInAnimation("Enemy Run"))
        {
            animator.Play("Enemy Run", 0);
        }
    }

    IEnumerator RockAttackThenTransform()
    {
        isAttacking = true;
        hasRockAttacked = true;
        rb.linearVelocity = Vector2.zero;

        // Step 1: Rock attack
        animator.Play("Enemy Attack 1");
        yield return new WaitForSeconds(1f); // adjust based on animation timing

        // Step 3: Switch to Mid form
        SwitchToForm(GolemForm.Mid);
        isAttacking = false;
    }

    IEnumerator AttackCycle()
    {
        isInAttackCycle = true;
        attackTimer = attackCycleDuration;

        while (attackTimer > 0f)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= attackRange)
            {
                rb.linearVelocity = Vector2.zero;
                animator.Play(useFirstAttack ? "Enemy Attack 2" : "Enemy Attack 3");
                useFirstAttack = !useFirstAttack;
                yield return new WaitForSeconds(1f); // attack interval
            }
            else
            {
                yield return null;
            }
        }

        isInAttackCycle = false;
    }

    IEnumerator ResetToBaseForm()
    {
        isAttacking = true;
        animator.Play("Enemy Ability"); // form2 -> rock
        yield return new WaitForSeconds(.3f);
        SwitchToForm(GolemForm.Base);
        hasRockAttacked = false;
        isAttacking = false;
        isInAttackCycle = false;

        if (IsPlayerInSight() && Vector2.Distance(transform.position, player.position) > attackRange)
        {
            MoveTowardPlayer(); // manually trigger running again
        }

    }

    void SwitchToForm(GolemForm newForm)
    {
        currentForm = newForm;
        Debug.Log("Switching to form: " + newForm);

        switch (newForm)
        {
            case GolemForm.Base:
                animator.runtimeAnimatorController = baseFormController;
                spriteRenderer.sprite = baseIdleSprite;
                break;
            case GolemForm.Mid:
                animator.runtimeAnimatorController = form2Controller;
                spriteRenderer.sprite = form2IdleSprite;
                break;
        }
    }

    void FacePlayer()
    {
        if (player.position.x < transform.position.x)
            spriteRenderer.flipX = true;
        else
            spriteRenderer.flipX = false;
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
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"[RegularGolem] Took {damage} damage. HP: {currentHealth}");

        StartCoroutine(PlayHurtEffect());

        if (currentHealth <= 0)
        {
            StartCoroutine(HandleDeath());
        }
    }

    IEnumerator PlayHurtEffect()
    {
        isHurting = true;

        // Flash red to show damage
        spriteRenderer.color = Color.red;

        // Check if it's currently in an attack animation
        bool isInAttackAnim = IsInAnimation("Enemy Attack 1") || IsInAnimation("Enemy Attack 2") || IsInAnimation("Enemy Attack 3");

        // Only play "Enemy Hit" animation if not attacking
        if (!isInAttackAnim)
        {
            animator.Play("Enemy Hit");
        }

        yield return new WaitForSeconds(0.2f);

        spriteRenderer.color = Color.white;
        isHurting = false;

        // Only return to idle if not attacking or in attack cycle
        if (!isAttacking && !isInAttackCycle && !isInAttackAnim)
        {
            animator.Play("Enemy Idle");
        }
    }

    IEnumerator HandleDeath()
    {
        isDead = true;
        isAttacking = true;
        isHurting = true;
        rb.linearVelocity = Vector2.zero;

        animator.Play("Enemy Death");

        yield return new WaitForSeconds(0.5f);

        Destroy(gameObject);
    }
    public bool IsAttacking()
{
     AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
    
    bool isAttackAnim = info.IsName("Enemy Attack 1") ||
                        info.IsName("Enemy Attack 2") ||
                        info.IsName("Enemy Attack 3");

    // Only return true if the animation is in its middle section (e.g. 30%-90%)
    return isAttackAnim && info.normalizedTime >= 0.15f && info.normalizedTime <= 0.4f;
}
public void Parry()
{
    Debug.Log("[RegularGolem] Parried!");

    // Trigger stun effect
    RegularGolemStun stun = GetComponent<RegularGolemStun>();
    if (stun != null)
    {
        stun.RegisterParry();
    }
}

}
