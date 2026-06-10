using UnityEngine;
using System.Collections;

// AI and attacks for the Tornado Slime Boss: forward spin dash, projectile volleys,
// clone attack, and arena-wide wall bounce. Health lives in TornadoSlimeBossHealth.
public class TornadoSlimeBossBehavior : MonoBehaviour, IBoss
{
    [Header("References")]
    public Transform player;
    public Rigidbody2D rb;
    public Animator animator;
    public GameObject miniProjectilePrefab;
    public Transform[] wallBouncePoints;
    public SpriteRenderer spriteRenderer;
    public GameObject clonePrefab;
    public Collider2D bodyCollider;
    public DamagePlayerOnContact contactDamage;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float dashSpeed = 10f;
    public float spinRange = 4f;
    public float projectileRange = 6f;
    public float wallBounceSpeed = 12f;

    [Header("Cooldowns")]
    public float attackCooldown = 1.5f;
    public float cloneAttackInterval = 10f;
    public float wallBounceInterval = 20f;

    [Header("Final Phase")]
    public int wallBouncePasses = 3;
    public int aggressiveWallBouncePasses = 5;
    public float aggressiveSpeedMultiplier = 1.3f;
    public float aggressiveWallBounceInterval = 12f;

    [Header("Final Phase Separation")]
    // Set by the phase manager when both bosses fight; the chase holds position
    // instead of crowding the other boss.
    public Transform otherBoss;
    public float minSeparation = 2.5f;

    // State flags
    public bool isDead = false;
    public bool isAttacking = false;
    public bool canAttack = true;
    private bool isActive = false;
    private bool doublePhase = false;

    // Alternates dash and projectile volley so both attacks appear regularly;
    // a pure distance check starves the volley because the dash-chase loop
    // keeps the boss inside dash range.
    private bool preferVolley = false;

    // Ability timers
    private float cloneTimer;
    private float wallBounceTimer;

    // Where the boss vanished from during the clone attack.
    private Vector3 lastKnownPosition;

    // Resolves the body collider, disarms contact damage, and acquires the
    // runtime-spawned player by tag (inspector references cannot point at a
    // runtime-spawned Warrior).
    IEnumerator Start()
    {
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
        SetContactDamage(false);

        while (player == null)
        {
            GameObject found = GameObject.FindWithTag("Warrior");
            if (found != null) player = found.transform;
            yield return null;
        }
    }

    void Update()
    {
        if (isDead || player == null || !isActive) return;

        // Ability timers accumulate even while attacking so the specials cannot
        // be starved by long attacks; abilities only start below, between attacks.
        cloneTimer += Time.deltaTime;
        wallBounceTimer += Time.deltaTime;

        if (isAttacking) return;

        if (cloneTimer >= cloneAttackInterval)
        {
            cloneTimer = 0f;
            StartCoroutine(DoCloneSpinAttack());
            return;
        }

        if (wallBounceTimer >= wallBounceInterval)
        {
            wallBounceTimer = 0f;
            StartCoroutine(DoWallBounceAttack());
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (canAttack && distance <= projectileRange)
        {
            // Alternate: dash when close (unless a volley is due), volley
            // otherwise. Guarantees projectiles actually get used.
            if (distance <= spinRange && !preferVolley)
            {
                preferVolley = true;
                StartCoroutine(DoForwardSpinAttack());
            }
            else
            {
                preferVolley = false;
                StartCoroutine(DoMiniProjectileSpin());
            }
        }
        else
        {
            MoveTowardsPlayer();
        }
    }

    // ---- IBoss API ----

    // Starts the boss AI when the encounter begins or this boss enters the arena.
    public void ActivateBoss()
    {
        if (isActive) return;
        isActive = true;
        canAttack = true;
        Debug.Log("Tornado Slime Boss fight started.");
    }

    // Benches the boss: stops AI, timers, and any in-progress attack. Safe to call
    // mid-vanish: sprite and collider are restored and contact damage is disarmed.
    // Note: StopAllCoroutines also ends the Start player-acquisition loop, but the
    // player always exists by the time the phase manager benches a boss.
    public void EnterInactiveState()
    {
        isActive = false;
        isAttacking = false;
        canAttack = false;
        StopAllCoroutines();
        RestoreAfterVanish();
        SetContactDamage(false);
        rb.linearVelocity = Vector2.zero;
    }

    // Resumes the boss AI after being benched.
    public void Reactivate()
    {
        isActive = true;
        canAttack = true;
        isAttacking = false;
    }

    // Final phase: wall bounce gets more passes, more speed, and a shorter interval.
    public void EnableDoublePhase()
    {
        doublePhase = true;
        wallBounceInterval = aggressiveWallBounceInterval;
    }

    // ---- Movement and attacks ----

    // Chases the player horizontally while spinning. In the final phase the
    // chase holds position rather than crowding the other boss.
    void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;

        if (otherBoss != null && otherBoss.gameObject.activeInHierarchy)
        {
            float toOther = otherBoss.position.x - transform.position.x;
            bool movingTowardOther = Mathf.Sign(direction.x) == Mathf.Sign(toOther);
            if (movingTowardOther && Mathf.Abs(toOther) < minSeparation)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                animator.Play("Enemy Spin");
                return;
            }
        }

        // A sleeping Rigidbody2D ignores velocity writes; wake it explicitly.
        rb.WakeUp();
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
        animator.Play("Enemy Spin");
    }

    // Close range: spin and dash forward through the player's position.
    IEnumerator DoForwardSpinAttack()
    {
        isAttacking = true;
        canAttack = false;

        animator.Play("Enemy Spin");
        SetContactDamage(true);

        float dashDirection = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0);

        yield return new WaitForSeconds(0.8f);

        rb.linearVelocity = Vector2.zero;
        SetContactDamage(false);
        isAttacking = false;
        StartCoroutine(AttackCooldown());
    }

    // Mid range: stop and fire three mini tornado projectiles at the player.
    IEnumerator DoMiniProjectileSpin()
    {
        isAttacking = true;
        canAttack = false;

        animator.Play("Enemy Spin");
        rb.linearVelocity = Vector2.zero;

        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(1f);
            GameObject proj = Instantiate(miniProjectilePrefab, transform.position, Quaternion.identity);
            Vector2 dir = (player.position - transform.position).normalized;
            proj.GetComponent<MiniTornadoProjectile>().SetDirection(dir);
        }

        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
        StartCoroutine(AttackCooldown());
    }

    // Timed special: vanish, send two clones converging on the player, reappear
    // at the vanish position.
    IEnumerator DoCloneSpinAttack()
    {
        isAttacking = true;
        canAttack = false;
        lastKnownPosition = transform.position;

        // Vanish: hide sprite, disable collider, and freeze physics so the boss
        // cannot be hit and cannot fall through the floor while intangible.
        spriteRenderer.enabled = false;
        if (bodyCollider != null) bodyCollider.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        // Spawn one clone on each side of the player.
        Vector3 playerPos = player.position;
        GameObject leftClone = Instantiate(clonePrefab, playerPos + new Vector3(-2f, 0f, 0f), Quaternion.identity);
        GameObject rightClone = Instantiate(clonePrefab, playerPos + new Vector3(2f, 0f, 0f), Quaternion.identity);

        Animator animL = leftClone.GetComponent<Animator>();
        Animator animR = rightClone.GetComponent<Animator>();
        if (animL != null) animL.Play("Enemy Attack 1");
        if (animR != null) animR.Play("Enemy Attack 1");

        StartCoroutine(MoveCloneToPlayer(leftClone));
        StartCoroutine(MoveCloneToPlayer(rightClone));

        yield return new WaitForSeconds(1.2f);

        // Reappear where the boss vanished.
        transform.position = lastKnownPosition;
        RestoreAfterVanish();

        yield return new WaitForSeconds(0.3f);
        isAttacking = false;
        StartCoroutine(AttackCooldown());
    }

    // Moves one clone toward the player's position at spawn time, then removes it.
    private IEnumerator MoveCloneToPlayer(GameObject clone)
    {
        float speed = 6f;
        float duration = 1.2f;
        float timer = 0f;
        Vector2 direction = (player.position - clone.transform.position).normalized;

        Rigidbody2D rbClone = clone.GetComponent<Rigidbody2D>();
        if (rbClone != null)
            rbClone.gravityScale = 0;

        while (timer < duration)
        {
            if (clone == null) yield break;
            clone.transform.position += (Vector3)(direction * speed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        Destroy(clone);
    }

    // Timed special: rapidly travel between the arena walls while spinning.
    // Final phase makes it faster with more passes.
    IEnumerator DoWallBounceAttack()
    {
        if (wallBouncePoints == null || wallBouncePoints.Length < 2)
        {
            Debug.LogWarning("[TornadoSlimeBoss] wallBouncePoints not assigned, skipping wall bounce.");
            yield break;
        }

        isAttacking = true;
        canAttack = false;
        animator.Play("Enemy Spin");
        SetContactDamage(true);

        Vector3 left = wallBouncePoints[0].position;
        Vector3 right = wallBouncePoints[1].position;

        int passes = doublePhase ? aggressiveWallBouncePasses : wallBouncePasses;
        float speed = doublePhase ? wallBounceSpeed * aggressiveSpeedMultiplier : wallBounceSpeed;

        for (int i = 0; i < passes; i++)
        {
            Vector3 target = (i % 2 == 0) ? right : left;

            // Per-leg timeout so a blocked path can never loop forever.
            float legTimeout = 2.5f;
            while (Vector2.Distance(transform.position, target) > 0.1f && legTimeout > 0f)
            {
                Vector2 moveDir = (target - transform.position).normalized;
                rb.linearVelocity = moveDir * speed;
                legTimeout -= Time.deltaTime;
                yield return null;
            }
            rb.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(0.2f);
        }

        rb.linearVelocity = Vector2.zero;
        SetContactDamage(false);
        isAttacking = false;
        StartCoroutine(AttackCooldown());
    }

    // ---- Helpers ----

    // Shared cooldown after an attack so the boss cannot chain attacks every frame.
    IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    // Restores sprite, collider, and physics after a vanish-based attack.
    private void RestoreAfterVanish()
    {
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (bodyCollider != null) bodyCollider.enabled = true;
        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
        }
    }

    // Arms or disarms the contact damage component used during spin attacks.
    private void SetContactDamage(bool active)
    {
        if (contactDamage != null) contactDamage.enabled = active;
    }
}
