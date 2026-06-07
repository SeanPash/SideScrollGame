using UnityEngine;
using System.Collections;

public class BossGolemBehavior : MonoBehaviour, IDamageable, IAttackState
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
    private float attackTimer = 1.5f;
    private bool useFirstAttack = true;
    private bool hasRockAttacked = false;
    private bool isMoving = false;
    private bool hasSwitchedToMidOnce = false;
    public bool isHurting = false;
    public GameObject blackoutPanel; // Assign a full-screen UI panel with black Image + CanvasGroup
    public float fadeDuration = 1f;
    public WarriorController warriorController; // Reference to disable movement/input
    private bool isDead = false;
    private bool canBeParried = false;






    void Start()
    {
        currentHealth = maxHealth;
        SwitchToForm(GolemForm.Base);
    }

    void Update()
    {
        
        if (isDead || !IsPlayerInSight() || isHurting) return;

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
    isAttacking = true;
    isInAttackCycle = true;

    rb.linearVelocity = Vector2.zero;

    animator.Play(useFirstAttack ? "Enemy Attack 2" : "Enemy Attack 3");
    useFirstAttack = !useFirstAttack;

    yield return new WaitForSeconds(0.05f); // Wind-up buffer

    canBeParried = true;
    yield return new WaitForSeconds(0.25f);  
    canBeParried = false;

    // Wait out the rest of the animation so it doesn’t get cut off
    yield return new WaitForSeconds(1.0f); // total wait = ~1.15s

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
        isAttacking = true;
        isInAttackCycle = false;
        isHurting = false;
        rb.linearVelocity = Vector2.zero;

        // Disable player movement script
        if (warriorController != null)
            warriorController.enabled = false;

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;

            Vector2 pushDir = (player.position - transform.position).normalized;
            pushDir.y = 0f;
            pushDir.Normalize();

            float pushDistance = 2f;   // how far to move
            float pushDuration = 0.25f; // how long it takes
            float elapsed = 0f;
            Vector3 startPos = player.position;
            Vector3 targetPos = startPos + (Vector3)(pushDir * pushDistance);

            // Smooth move to targetPos
            while (elapsed < pushDuration)
            {
                elapsed += Time.deltaTime;
                player.position = Vector3.Lerp(startPos, targetPos, elapsed / pushDuration);
                yield return null;
            }

            player.position = targetPos; // snap to final
        }

        blackoutPanel.SetActive(true);
        CanvasGroup cg = blackoutPanel.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;

        // Freeze boss during transformation
        rb.linearVelocity = Vector2.zero;
        animator.Play("Enemy Ability");

        yield return new WaitForSeconds(1f); // Let the ability play out

        SwitchToForm(GolemForm.Reinforced);
        animator.Play("Enemy Idle");

        yield return new WaitForSeconds(0.5f);

        // Return screen to normal
        blackoutPanel.SetActive(false);

        // Enable player movement again
        if (warriorController != null)
            warriorController.enabled = true;

        isInvulnerable = false;
        isAttacking = false;
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

        bool isAttackingNow = IsInAnimation("Enemy Attack 1") || IsInAnimation("Enemy Attack 2") || IsInAnimation("Enemy Attack 3");
        StartCoroutine(PlayHurtEffect(isAttackingNow));
        if (currentHealth <= maxHealth / 2f && currentForm != GolemForm.Reinforced)
        {
            Debug.Log("[BossGolem] Triggering transition to reinforced phase.");
            StartCoroutine(TransitionToReinforcedPhase());
        }

        if (currentHealth <= 0)
        {
            Debug.Log("[BossGolem] Triggering death.");
            rb.linearVelocity = Vector2.zero;
            StartCoroutine(HandleDeath());
        }
    }

    IEnumerator PlayHurtEffect(bool skipAnimation)
    {
        isHurting = true;

        // Flash red to show damage
        spriteRenderer.color = Color.red;

        if (!skipAnimation)
        {
            animator.Play("Enemy Hit");

        }

        yield return new WaitForSeconds(0.2f);

        spriteRenderer.color = Color.white;
        isHurting = false;

        // Optional: reset to idle only if not attacking
        if (!isAttacking && !isInAttackCycle && !skipAnimation)
            animator.Play("Enemy Idle");
    }
    IEnumerator FadeBlack(bool fadeIn)
    {
        CanvasGroup cg = blackoutPanel.GetComponent<CanvasGroup>();
        if (cg == null) yield break;

        float targetAlpha = fadeIn ? 1f : 0f;
        float startAlpha = cg.alpha;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float blend = Mathf.Lerp(startAlpha, targetAlpha, t / fadeDuration);
            cg.alpha = blend;
            yield return null;
        }

        cg.alpha = targetAlpha;
    }
    IEnumerator HandleDeath()
    {
        isDead = true;
        isAttacking = true;
        isHurting = true;

        SwitchToForm(GolemForm.Base);

        animator.Play("Enemy Death");

        yield return new WaitForSeconds(.5f); // Match your death animation length

        Destroy(gameObject);
    }
public bool IsAttacking()
{
    var info = animator.GetCurrentAnimatorStateInfo(0);
    return canBeParried && (
        info.IsName("Enemy Attack 1") ||
        info.IsName("Enemy Attack 2") ||
        info.IsName("Enemy Attack 3")
    );
}


public void Parry()
{
    Debug.Log("[BossGolem] Stunned by parry!");
    
    // Trigger the stun logic
    BossGolemStun stunComponent = GetComponent<BossGolemStun>();
    if (stunComponent != null)
    {
        stunComponent.RegisterParry();
    }
}






}
