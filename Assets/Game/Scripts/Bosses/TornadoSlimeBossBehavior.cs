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
    // Kept distance from the arena walls so dashes and chases never grind
    // into a corner. Bounds derive from the wall bounce points.
    public float wallMargin = 0.8f;
    public float dashTelegraphTime = 0.35f;
    // The dash is a short decelerating lunge rather than a long flat push.
    public float dashDuration = 0.55f;
    // The chase stops at this distance so the boss looms instead of standing
    // inside the player.
    public float standOffRange = 1.3f;

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

    // Target tracking: refreshed periodically so the boss can never chase a
    // stale player reference, and checked for death so it stops attacking
    // a corpse.
    private PlayerHealth targetHealth;
    private float playerRefreshTimer;

    // Resolves the body collider, disarms contact damage, and acquires the
    // runtime-spawned player by tag (inspector references cannot point at a
    // runtime-spawned Warrior).
    IEnumerator Start()
    {
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
        // Physics torque must never flip the tornado; keep it upright always.
        rb.freezeRotation = true;
        transform.rotation = Quaternion.identity;
        // The tornado fights grounded; the scene body was authored with zero
        // gravity, which left it floating mid-air.
        rb.gravityScale = 1f;
        SetContactDamage(false);

        while (player == null)
        {
            RefreshPlayerTarget();
            yield return null;
        }
    }

    // Re-resolves the Warrior by tag so destroyed or replaced players are
    // never chased as ghosts.
    private void RefreshPlayerTarget()
    {
        GameObject found = GameObject.FindWithTag("Warrior");
        if (found == null) return;
        if (player == null || player != found.transform)
        {
            player = found.transform;
            targetHealth = found.GetComponentInParent<PlayerHealth>();
        }
        else if (targetHealth == null)
        {
            targetHealth = found.GetComponentInParent<PlayerHealth>();
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

        playerRefreshTimer += Time.deltaTime;
        if (playerRefreshTimer >= 2f)
        {
            playerRefreshTimer = 0f;
            RefreshPlayerTarget();
        }

        // A defeated player ends the pressure; stand down instead of attacking
        // the corpse during the respawn delay.
        if (targetHealth != null && targetHealth.isDead)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            animator.Play("Idle");
            return;
        }

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
            // Strict alternation keeps both attacks in the rotation; the
            // volley backs off on its own when it starts point-blank.
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
        animator.Play("Idle");
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

    // Arena bounds derived from the wall bounce points plus the margin.
    private float LeftBound => wallBouncePoints != null && wallBouncePoints.Length >= 2
        ? Mathf.Min(wallBouncePoints[0].position.x, wallBouncePoints[1].position.x) + wallMargin
        : float.NegativeInfinity;
    private float RightBound => wallBouncePoints != null && wallBouncePoints.Length >= 2
        ? Mathf.Max(wallBouncePoints[0].position.x, wallBouncePoints[1].position.x) - wallMargin
        : float.PositiveInfinity;

    // Flips the sprite to face the given horizontal direction.
    private void Face(float dirX)
    {
        if (dirX != 0f)
            transform.localScale = new Vector3(
                Mathf.Sign(dirX) * Mathf.Abs(transform.localScale.x),
                transform.localScale.y, transform.localScale.z);
    }

    // Walks toward the player (the tornado only spins during attacks, so the
    // spin reads as danger). Holds position at walls, near the other boss, and
    // at stand-off range so it looms instead of standing inside the player.
    void MoveTowardsPlayer()
    {
        float dx = player.position.x - transform.position.x;
        float distance = Vector2.Distance(transform.position, player.position);

        // Close enough: face the player and idle until an attack is ready.
        if (distance <= standOffRange)
        {
            rb.WakeUp();
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            Face(dx);
            animator.Play("Idle");
            return;
        }

        float dirX = Mathf.Sign(dx);

        if (otherBoss != null && otherBoss.gameObject.activeInHierarchy)
        {
            float toOther = otherBoss.position.x - transform.position.x;
            bool movingTowardOther = dirX == Mathf.Sign(toOther);
            if (movingTowardOther && Mathf.Abs(toOther) < minSeparation)
            {
                rb.WakeUp();
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                animator.Play("Idle");
                return;
            }
        }

        // Never walk into the arena walls.
        float nextX = transform.position.x + dirX * moveSpeed * Time.deltaTime;
        if (nextX < LeftBound || nextX > RightBound)
        {
            rb.WakeUp();
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            animator.Play("Idle");
            return;
        }

        // A sleeping Rigidbody2D ignores velocity writes; wake it explicitly.
        rb.WakeUp();
        rb.linearVelocity = new Vector2(dirX * moveSpeed, rb.linearVelocity.y);
        Face(dirX);
        animator.Play("Walk");
    }

    // Close range: hop telegraph, then spin and dash through the player's
    // position. The dash ends early at the arena walls instead of grinding
    // into a corner.
    IEnumerator DoForwardSpinAttack()
    {
        isAttacking = true;
        canAttack = false;

        // Telegraph: face the player and hop in place before committing.
        Face(player.position.x - transform.position.x);
        rb.linearVelocity = Vector2.zero;
        animator.Play("Jump");
        yield return new WaitForSeconds(dashTelegraphTime);

        animator.Play("Spin");
        SetContactDamage(true);

        // Pass through the player during the lunge so the two bodies can
        // never wedge against each other; contact damage still triggers.
        if (bodyCollider != null) bodyCollider.isTrigger = true;

        float dashDirection = Mathf.Sign(player.position.x - transform.position.x);
        if (dashDirection == 0f) dashDirection = Mathf.Sign(transform.localScale.x);

        // Decelerating lunge: fast launch that eases off instead of a long
        // flat push across the arena.
        float timer = 0f;
        while (timer < dashDuration)
        {
            float speed = Mathf.Lerp(dashSpeed, dashSpeed * 0.25f, timer / dashDuration);
            float nextX = transform.position.x + dashDirection * speed * Time.deltaTime;
            if (nextX < LeftBound || nextX > RightBound) break;
            rb.WakeUp();
            rb.linearVelocity = new Vector2(dashDirection * speed, 0f);
            timer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        if (bodyCollider != null) bodyCollider.isTrigger = false;
        SetContactDamage(false);
        animator.Play("Idle");
        isAttacking = false;
        StartCoroutine(AttackCooldown());
    }

    // Spin in place and fire three mini tornado projectiles, each aimed at the
    // player's position at fire time. Starting point-blank, the boss first
    // hops backward so the volley reads as a ranged attack.
    IEnumerator DoMiniProjectileSpin()
    {
        isAttacking = true;
        canAttack = false;

        animator.Play("Spin");

        float away = Mathf.Sign(transform.position.x - player.position.x);
        if (away == 0f) away = -Mathf.Sign(transform.localScale.x);
        float retreat = 0f;
        while (Vector2.Distance(transform.position, player.position) < standOffRange + 0.7f
            && retreat < 0.45f)
        {
            float nextX = transform.position.x + away * 6f * Time.deltaTime;
            if (nextX < LeftBound || nextX > RightBound) break;
            rb.WakeUp();
            rb.linearVelocity = new Vector2(away * 6f, rb.linearVelocity.y);
            retreat += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(1f);
            Face(player.position.x - transform.position.x);
            GameObject proj = Instantiate(miniProjectilePrefab, transform.position, Quaternion.identity);
            Vector2 dir = (player.position - transform.position).normalized;
            proj.GetComponent<MiniTornadoProjectile>().SetDirection(dir);
        }

        yield return new WaitForSeconds(0.5f);
        animator.Play("Idle");
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

        // Spawn one clone on each side of the player. Clones spin and are
        // slightly translucent so they read as copies, not a second boss.
        Vector3 playerPos = player.position;
        GameObject leftClone = Instantiate(clonePrefab, playerPos + new Vector3(-2f, 0f, 0f), Quaternion.identity);
        GameObject rightClone = Instantiate(clonePrefab, playerPos + new Vector3(2f, 0f, 0f), Quaternion.identity);
        SetupClone(leftClone, 1f);
        SetupClone(rightClone, -1f);

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

    // Configures a freshly spawned clone: spinning, translucent, upright, and
    // facing its travel direction.
    private void SetupClone(GameObject clone, float faceDir)
    {
        var anim = clone.GetComponent<Animator>();
        if (anim != null) anim.Play("Spin");
        var sr = clone.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f, 1f, 1f, 0.7f);
        var cloneRb = clone.GetComponent<Rigidbody2D>();
        if (cloneRb != null) cloneRb.freezeRotation = true;
        clone.transform.rotation = Quaternion.identity;
        clone.transform.localScale = new Vector3(
            faceDir * Mathf.Abs(clone.transform.localScale.x),
            clone.transform.localScale.y, clone.transform.localScale.z);
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

        // Telegraph the arena-wide attack before it starts.
        rb.linearVelocity = Vector2.zero;
        animator.Play("Jump");
        yield return new WaitForSeconds(dashTelegraphTime);

        animator.Play("Spin");
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
        animator.Play("Idle");
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

    // Restores sprite, collider, physics, and upright rotation after a
    // vanish-based attack or an interrupted lunge.
    private void RestoreAfterVanish()
    {
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (bodyCollider != null)
        {
            bodyCollider.enabled = true;
            bodyCollider.isTrigger = false;
        }
        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
        }
        transform.rotation = Quaternion.identity;
    }

    // Arms or disarms the contact damage component used during spin attacks.
    private void SetContactDamage(bool active)
    {
        if (contactDamage != null) contactDamage.enabled = active;
    }
}
