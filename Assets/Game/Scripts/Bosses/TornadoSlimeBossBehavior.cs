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
    // The chase stops at this distance so the boss looms with space between
    // it and the player, giving room to react when an attack launches.
    public float standOffRange = 2.5f;
    // Projectiles fire flat from player height at this speed so they reach
    // the player instead of clipping the ground short.
    public float projectileSpeed = 10f;
    // Clones appear this far from the player, with a windup pause, so the
    // attack can be reacted to; they despawn quickly after converging.
    public float cloneSpawnDistance = 3f;
    public float cloneWindupTime = 0.35f;
    public float cloneTravelTime = 0.85f;
    // The volley hop-back keeps at least this distance before firing.
    public float volleyRetreatRange = 3.5f;
    [Header("Dive Attack")]
    // Aerial special: climb to the top corner across from the player, hover,
    // dive down through their position, and pull up past them.
    public float diveAttackInterval = 14f;
    public float diveHeight = 4f;
    public float diveSpeed = 11f;


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
    private float diveTimer;

    // Where the boss vanished from during the clone attack.
    private Vector3 lastKnownPosition;

    // Target tracking: refreshed periodically so the boss can never chase a
    // stale player reference, and checked for death so it stops attacking
    // a corpse.
    private PlayerHealth targetHealth;
    private float playerRefreshTimer;

    // Watchdog: how long the boss has been continuously vanished.
    private float vanishedTime;

    // The boss's center height while standing on the floor, captured whenever
    // it is grounded. All flight attacks ride at exactly this height so they
    // hit the player instead of sailing above, and never sink below it.
    private float restingY = float.NaN;

    // Ride height for flight attacks: the captured standing height, falling
    // back to the wall bounce line before the first capture.
    private float RideY => !float.IsNaN(restingY) ? restingY
        : (wallBouncePoints != null && wallBouncePoints.Length >= 2
            ? Mathf.Min(wallBouncePoints[0].position.y, wallBouncePoints[1].position.y)
            : transform.position.y);

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
        // Re-applied every refresh: Unity drops IgnoreCollision pairs whenever
        // a collider is disabled, which the vanish attacks do.
        IgnorePlayerCollision(found);
    }

    // The player can always move and slide through the tornado's body; damage
    // comes only from the armed contact triggers, never from body shoving.
    private void IgnorePlayerCollision(GameObject playerObj)
    {
        if (bodyCollider == null) return;
        foreach (var col in playerObj.GetComponentsInChildren<Collider2D>())
            if (!col.isTrigger)
                Physics2D.IgnoreCollision(bodyCollider, col, true);
    }

    void Update()
    {
        if (isDead || player == null || !isActive) return;

        // Ability timers accumulate even while attacking so the specials cannot
        // be starved by long attacks; abilities only start below, between attacks.
        cloneTimer += Time.deltaTime;
        wallBounceTimer += Time.deltaTime;
        diveTimer += Time.deltaTime;

        // Target refresh must tick during attacks too; it also re-applies the
        // player pass-through that collider toggles keep dropping.
        playerRefreshTimer += Time.deltaTime;
        if (playerRefreshTimer >= 2f)
        {
            playerRefreshTimer = 0f;
            RefreshPlayerTarget();
        }

        // Watchdog: never stay vanished past the longest legitimate vanish
        // window; self-heal whatever interrupted the attack coroutine.
        if (spriteRenderer != null && !spriteRenderer.enabled)
        {
            vanishedTime += Time.deltaTime;
            if (vanishedTime > 3f)
            {
                vanishedTime = 0f;
                StopAllCoroutines();
                RestoreAfterVanish();
                SetContactDamage(false);
                isAttacking = false;
                StartCoroutine(AttackCooldown());
            }
        }
        else vanishedTime = 0f;

        // Capture the natural standing height whenever the boss is grounded
        // with a solid body; flight attacks ride at this exact level.
        if (!isAttacking && bodyCollider != null && !bodyCollider.isTrigger
            && Mathf.Abs(rb.linearVelocity.y) < 0.05f)
        {
            restingY = transform.position.y;
        }

        // Watchdog: pull the boss back up the moment anything drops it below
        // its standing height.
        if (transform.position.y < RideY - 0.5f)
        {
            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x, LeftBound, RightBound),
                RideY, 0f);
            rb.linearVelocity = Vector2.zero;
        }

        if (isAttacking) return;

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

        if (diveTimer >= diveAttackInterval)
        {
            diveTimer = 0f;
            StartCoroutine(DoDiveAttack());
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

        // Too close (for example after lunging through the player): back away
        // to stand-off range so the next attack starts with reaction space.
        if (distance < standOffRange - 0.8f)
        {
            float awayDir = dx != 0f ? -Mathf.Sign(dx) : 1f;
            float backX = transform.position.x + awayDir * moveSpeed * Time.deltaTime;
            if (backX >= LeftBound && backX <= RightBound)
            {
                rb.WakeUp();
                rb.linearVelocity = new Vector2(awayDir * moveSpeed, rb.linearVelocity.y);
                Face(dx);
                animator.Play("Walk");
                return;
            }
        }

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
        RestoreSolidBody();
        SetContactDamage(false);
        animator.Play("Idle");
        isAttacking = false;
        StartCoroutine(AttackCooldown());
    }

    // Snaps the boss back to standing height if a flight frame dipped under.
    private void ClampToRideHeight()
    {
        if (transform.position.y < RideY)
            transform.position = new Vector3(transform.position.x, RideY,
                transform.position.z);
    }

    // Snaps the boss fully back inside the arena: fast sweep frames can
    // overshoot past a wall where there is no floor underneath.
    private void ClampInsideArena()
    {
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, LeftBound, RightBound),
            Mathf.Max(transform.position.y, RideY),
            transform.position.z);
    }

    // Makes the body solid again after a trigger-mode attack and immediately
    // re-applies the player pass-through (fixture rebuilds drop ignore pairs).
    private void RestoreSolidBody()
    {
        if (bodyCollider != null) bodyCollider.isTrigger = false;
        if (player != null) IgnorePlayerCollision(player.gameObject);
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
        while (Vector2.Distance(transform.position, player.position) < volleyRetreatRange
            && retreat < 0.6f)
        {
            float nextX = transform.position.x + away * 7f * Time.deltaTime;
            if (nextX < LeftBound || nextX > RightBound) break;
            rb.WakeUp();
            rb.linearVelocity = new Vector2(away * 7f, rb.linearVelocity.y);
            retreat += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(1f);
            float toPlayer = Mathf.Sign(player.position.x - transform.position.x);
            if (toPlayer == 0f) toPlayer = Mathf.Sign(transform.localScale.x);
            Face(toPlayer);

            // Fire flat from the player's height so the shot travels straight
            // at them instead of angling down into the ground short.
            Vector3 spawnPos = new Vector3(transform.position.x + toPlayer * 0.8f, player.position.y, 0f);
            GameObject proj = Instantiate(miniProjectilePrefab, spawnPos, Quaternion.identity);
            var mini = proj.GetComponent<MiniTornadoProjectile>();
            mini.speed = projectileSpeed;
            mini.SetDirection(player.position - spawnPos);
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

        // Spawn one clone on each side of the player, far enough out that the
        // converge can be dodged, at the player's height but never below the
        // floor. Clones spin and are slightly translucent so they read as
        // copies, not a second boss.
        Vector3 playerPos = player.position;
        float spawnY = Mathf.Max(playerPos.y, RideY);
        GameObject leftClone = Instantiate(clonePrefab,
            new Vector3(playerPos.x - cloneSpawnDistance, spawnY, 0f), Quaternion.identity);
        GameObject rightClone = Instantiate(clonePrefab,
            new Vector3(playerPos.x + cloneSpawnDistance, spawnY, 0f), Quaternion.identity);
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
        // Clones never block movement; slides and walks pass straight through
        // (their contact damage works through trigger events).
        foreach (var col in clone.GetComponentsInChildren<Collider2D>())
            col.isTrigger = true;
        clone.transform.rotation = Quaternion.identity;
        clone.transform.localScale = new Vector3(
            faceDir * Mathf.Abs(clone.transform.localScale.x),
            clone.transform.localScale.y, clone.transform.localScale.z);
    }

    // Holds the clone in a brief windup, then moves it toward the player's
    // position and removes it.
    private IEnumerator MoveCloneToPlayer(GameObject clone)
    {
        // Windup: visible and spinning but not yet moving, giving the player
        // a beat to pick a dodge direction.
        yield return new WaitForSeconds(cloneWindupTime);
        if (clone == null) yield break;

        float speed = 6f;
        Vector2 start = clone.transform.position;
        Vector2 targetPos = player.position;
        Vector2 direction = (targetPos - start).normalized;
        // Vanish the instant the clone crosses the convergence point (with a
        // tiny overshoot); no lingering after the squeeze.
        float travelDistance = Vector2.Distance(start, targetPos) + 0.6f;
        float maxDistance = speed * cloneTravelTime;
        float traveled = 0f;

        Rigidbody2D rbClone = clone.GetComponent<Rigidbody2D>();
        if (rbClone != null)
            rbClone.gravityScale = 0;

        // Clones glide along the floor once they reach it; they never sink
        // below standing height.
        float minY = RideY;

        while (traveled < travelDistance && traveled < maxDistance)
        {
            if (clone == null) yield break;
            float step = speed * Time.deltaTime;
            Vector3 next = clone.transform.position + (Vector3)(direction * step);
            if (next.y < minY) next.y = minY;
            clone.transform.position = next;
            traveled += step;
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

        Vector3 left = wallBouncePoints[0].position;
        Vector3 right = wallBouncePoints[1].position;

        // Rush quickly to the nearest wall first (harmless travel).
        animator.Play("Spin");
        if (bodyCollider != null) bodyCollider.isTrigger = true;
        Vector3 startWall = Mathf.Abs(transform.position.x - left.x) < Mathf.Abs(transform.position.x - right.x)
            ? left : right;
        float rushDir = Mathf.Sign(startWall.x - transform.position.x);
        float rushTimeout = 2f;
        while (rushTimeout > 0f && Mathf.Abs(transform.position.x - startWall.x) > 0.3f)
        {
            // The body is in pass-through mode, so the floor cannot hold it:
            // hold standing height manually.
            ClampToRideHeight();
            rb.WakeUp();
            rb.linearVelocity = new Vector2(rushDir * wallBounceSpeed * 1.4f, 0f);
            rushTimeout -= Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = Vector2.zero;
        ClampInsideArena();
        Face(-rushDir);

        // Brief hop windup at the wall, then the sweep launches.
        animator.Play("Jump");
        yield return new WaitForSeconds(0.4f);

        animator.Play("Spin");
        SetContactDamage(true);

        int passes = doublePhase ? aggressiveWallBouncePasses : wallBouncePasses;
        float speed = doublePhase ? wallBounceSpeed * aggressiveSpeedMultiplier : wallBounceSpeed;

        // Straight floor-level passes between the walls; gravity keeps the
        // sweep grounded.
        for (int i = 0; i < passes; i++)
        {
            Vector3 target = Mathf.Abs(transform.position.x - left.x) < Mathf.Abs(transform.position.x - right.x)
                ? right : left;
            float dir = Mathf.Sign(target.x - transform.position.x);
            if (dir == 0f) dir = 1f;

            float legTimeout = 3f;
            while (legTimeout > 0f
                && (dir > 0f ? transform.position.x < target.x - 0.2f
                             : transform.position.x > target.x + 0.2f))
            {
                // Pass-through body: hold the sweep at standing height so it
                // connects with the player.
                ClampToRideHeight();
                rb.WakeUp();
                rb.linearVelocity = new Vector2(dir * speed, 0f);
                legTimeout -= Time.deltaTime;
                yield return null;
            }
            rb.linearVelocity = Vector2.zero;
            ClampInsideArena();
            yield return new WaitForSeconds(0.15f);
        }

        rb.linearVelocity = Vector2.zero;
        RestoreSolidBody();
        SetContactDamage(false);
        animator.Play("Idle");
        isAttacking = false;
        StartCoroutine(AttackCooldown());
    }

    // Aerial special: climb to the top corner across from the player, hover a
    // beat, dive down through their position, then pull up while passing them.
    IEnumerator DoDiveAttack()
    {
        isAttacking = true;
        canAttack = false;

        float floorY = wallBouncePoints != null && wallBouncePoints.Length >= 2
            ? Mathf.Min(wallBouncePoints[0].position.y, wallBouncePoints[1].position.y)
            : transform.position.y;

        animator.Play("Spin");
        // Fully airborne pass-through attack; damage arms only for the dive.
        if (bodyCollider != null) bodyCollider.isTrigger = true;

        // Climb to the upper corner on the far side from the player.
        float cornerX = player.position.x < (LeftBound + RightBound) * 0.5f ? RightBound : LeftBound;
        Vector2 climbTarget = new Vector2(cornerX, floorY + diveHeight);
        float timeout = 2.5f;
        while (timeout > 0f && Vector2.Distance(transform.position, climbTarget) > 0.5f)
        {
            Vector2 climbDir = (climbTarget - (Vector2)transform.position).normalized;
            rb.WakeUp();
            rb.linearVelocity = climbDir * diveSpeed;
            timeout -= Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = Vector2.zero;

        // Hover telegraph aimed at the player, then commit to the dive.
        Face(player.position.x - transform.position.x);
        yield return new WaitForSeconds(0.35f);

        SetContactDamage(true);
        Vector2 diveTarget = player.position;
        // The dive bottoms out at standing height, never below it.
        diveTarget.y = Mathf.Max(diveTarget.y, RideY);
        float diveDir = Mathf.Sign(diveTarget.x - transform.position.x);
        if (diveDir == 0f) diveDir = 1f;

        // Descend through the player's locked position.
        timeout = 2f;
        while (timeout > 0f
            && (diveDir > 0f ? transform.position.x < diveTarget.x - 0.3f
                             : transform.position.x > diveTarget.x + 0.3f))
        {
            ClampToRideHeight();
            Vector2 dir = (diveTarget - (Vector2)transform.position).normalized;
            rb.WakeUp();
            rb.linearVelocity = dir * diveSpeed;
            timeout -= Time.deltaTime;
            yield return null;
        }

        // Pull up slightly while carrying on past them.
        timeout = 0.5f;
        while (timeout > 0f)
        {
            ClampToRideHeight();
            float nextX = transform.position.x + diveDir * diveSpeed * 0.8f * Time.deltaTime;
            if (nextX < LeftBound || nextX > RightBound) break;
            rb.WakeUp();
            rb.linearVelocity = new Vector2(diveDir * diveSpeed * 0.8f, diveSpeed * 0.5f);
            timeout -= Time.deltaTime;
            yield return null;
        }

        // Gravity settles the boss back to the floor afterwards.
        rb.linearVelocity = Vector2.zero;
        ClampInsideArena();
        SetContactDamage(false);
        RestoreSolidBody();
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
        // Re-enabling the collider drops IgnoreCollision pairs; restore the
        // player pass-through immediately.
        if (player != null) IgnorePlayerCollision(player.gameObject);
    }

    // Arms or disarms the contact damage component used during spin attacks.
    private void SetContactDamage(bool active)
    {
        if (contactDamage != null) contactDamage.enabled = active;
    }
}
