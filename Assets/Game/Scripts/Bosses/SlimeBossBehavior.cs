using UnityEngine;
using System.Collections;

// AI and attacks for the Slime Boss: jump drop attack, exploding mini slime spawns,
// and the tracking slam special that leaves goo hazards. Health lives in SlimeBossHealth.
public class SlimeBossBehavior : MonoBehaviour, IBoss
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float detectionRange = 8f;

    [Header("Attack")]
    public float attackCooldown = 3f;
    public float attackRange = 4f;
    public float jumpHeight = 2f;
    public float jumpUpDuration = 0.2f;
    public float dropSpeed = 6f;
    public float dropStartDelay = 0.05f;
    public float maxJumpDistance = 2f;

    [Header("References")]
    public Transform player;
    public Rigidbody2D rb;
    public Animator animator;
    public LayerMask groundLayer;
    public SpriteRenderer spriteRenderer;
    public GameObject landingHitbox;
    public GameObject miniSlimePrefab;
    public GameObject greenSlimeZonePrefab;
    public GameObject redOutlinePrefab;
    public Collider2D bodyCollider;

    [Header("Ability Settings")]
    public float miniSlimeInterval = 15f;
    public float slamAttackInterval = 20f;
    public float slamTrackDuration = 1.1f;
    public float slamFlashDuration = 0.2f;
    public Transform[] miniSlimeSpawnPoints;

    // State flags
    public bool isAttacking = false;
    private bool canAttack = true;
    private bool isActive = false;
    public bool isDead = false;

    // Ability timers
    private float miniSlimeTimer = 0f;
    private float slamTimer = 0f;

    // Resolves the body collider and acquires the runtime-spawned player by tag.
    // The Warrior is spawned at runtime, so an inspector reference cannot be used.
    IEnumerator Start()
    {
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();

        while (player == null)
        {
            GameObject found = GameObject.FindWithTag("Warrior");
            if (found != null) player = found.transform;
            yield return null;
        }
    }

    void Update()
    {
        if (isDead || isAttacking || player == null || !isActive) return;

        miniSlimeTimer += Time.deltaTime;
        slamTimer += Time.deltaTime;

        if (miniSlimeTimer >= miniSlimeInterval)
        {
            miniSlimeTimer = 0f;
            StartCoroutine(SpawnMiniSlimesAbility());
        }

        if (slamTimer >= slamAttackInterval)
        {
            slamTimer = 0f;
            StartCoroutine(SlamAttackSequence());
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= detectionRange && canAttack)
        {
            StartCoroutine(DoDropAttack());
        }
        else
        {
            Idle();
        }
    }

    // ---- IBoss API ----

    // Starts the boss AI when the encounter begins or this boss enters the arena.
    public void ActivateBoss()
    {
        if (isActive) return;
        isActive = true;
        canAttack = true;
        Debug.Log("Slime Boss fight started.");
    }

    // Benches the boss: stops AI, timers, and any in-progress attack. Safe to call
    // mid-vanish: sprite, collider, and landing hitbox are restored to normal.
    // Note: StopAllCoroutines also ends the Start player-acquisition loop, but the
    // player always exists by the time the phase manager benches a boss.
    public void EnterInactiveState()
    {
        isActive = false;
        isAttacking = false;
        canAttack = false;
        StopAllCoroutines();
        RestoreAfterVanish();
        if (landingHitbox != null) landingHitbox.SetActive(false);
        rb.linearVelocity = Vector2.zero;
        animator.Play("Enemy Idle");
    }

    // Resumes the boss AI after being benched.
    public void Reactivate()
    {
        isActive = true;
        canAttack = true;
        isAttacking = false;
    }

    // Final phase hook. The slime boss keeps its normal kit.
    public void EnableDoublePhase()
    {
    }

    // ---- Attacks ----

    // Jump toward the player and drop down on them.
    IEnumerator DoDropAttack()
    {
        if (isDead) yield break;

        isAttacking = true;
        canAttack = false;
        rb.linearVelocity = Vector2.zero;
        Idle();

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > detectionRange)
        {
            isAttacking = false;
            canAttack = true;
            yield break;
        }

        yield return new WaitForSeconds(0.2f);

        // Lock the jump target before committing to the attack.
        float dir = Mathf.Sign(player.position.x - transform.position.x);
        float distanceToPlayer = Mathf.Abs(player.position.x - transform.position.x);
        float clampedDistance = Mathf.Min(distanceToPlayer, maxJumpDistance);
        float lockedTargetX = transform.position.x + dir * clampedDistance;

        animator.Play("Enemy Attack 1");

        if (landingHitbox != null)
        {
            landingHitbox.SetActive(true);
            StartCoroutine(DisableLandingHitboxAfterDelay(1f));
        }

        yield return JumpAbovePlayer(lockedTargetX);
        yield return new WaitForSeconds(dropStartDelay);

        float timeout = 2f;
        while (!IsGrounded() && timeout > 0f)
        {
            rb.linearVelocity = new Vector2(0, -dropSpeed);
            timeout -= Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.5f);

        isAttacking = false;
        canAttack = true;
    }

    // Tracking slam special, repeated 3 times: vanish, track the player with a red
    // outline, lock the spot, flash as a dodge window, slam at the locked spot,
    // and leave a goo hazard.
    IEnumerator SlamAttackSequence()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        for (int i = 0; i < 3; i++)
        {
            // Vanish: hide sprite and disable collider so the player cannot hit
            // an invisible boss.
            spriteRenderer.enabled = false;
            if (bodyCollider != null) bodyCollider.enabled = false;

            Vector2 outlinePos = new Vector2(player.position.x, transform.position.y);
            GameObject redOutline = Instantiate(redOutlinePrefab, outlinePos, Quaternion.identity);

            // Track the player for the windup duration.
            float trackTimer = slamTrackDuration;
            while (trackTimer > 0f)
            {
                outlinePos = new Vector2(player.position.x, transform.position.y);
                if (redOutline != null) redOutline.transform.position = outlinePos;
                trackTimer -= Time.deltaTime;
                yield return null;
            }

            // Lock the target and flash: this is the player's dodge window.
            if (redOutline != null)
                redOutline.GetComponent<SpriteRenderer>().color = Color.red;
            yield return new WaitForSeconds(slamFlashDuration);
            if (redOutline != null) Destroy(redOutline);

            // Slam down at the locked position and leave goo.
            transform.position = outlinePos;
            RestoreAfterVanish();
            animator.Play("Enemy Attack 1");

            if (landingHitbox != null)
            {
                landingHitbox.SetActive(true);
                StartCoroutine(DisableLandingHitboxAfterDelay(0.3f));
            }

            if (greenSlimeZonePrefab != null)
                Instantiate(greenSlimeZonePrefab, transform.position, Quaternion.identity);

            yield return new WaitForSeconds(0.5f);
        }

        isAttacking = false;
    }

    // Turns the landing hitbox off after a delay.
    IEnumerator DisableLandingHitboxAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (landingHitbox != null)
            landingHitbox.SetActive(false);
    }

    // Spawns exploding mini slimes at the configured points (or near the boss).
    IEnumerator SpawnMiniSlimesAbility()
    {
        Debug.Log("Slime Boss spawns exploding slimes");

        if (miniSlimePrefab == null) yield break;

        for (int i = 0; i < 2; i++)
        {
            Vector2 spawnPos = (miniSlimeSpawnPoints != null && miniSlimeSpawnPoints.Length > i)
                ? miniSlimeSpawnPoints[i].position
                : (Vector2)transform.position + new Vector2(Random.Range(-4f, 4f), 0f);

            Instantiate(miniSlimePrefab, spawnPos, Quaternion.identity);
        }

        yield return null;
    }

    // Lerps the boss up above the locked target position.
    IEnumerator JumpAbovePlayer(float targetX)
    {
        Vector2 start = transform.position;
        Vector2 target = new Vector2(targetX, start.y + jumpHeight);

        float timer = 0f;
        while (timer < jumpUpDuration)
        {
            float t = timer / jumpUpDuration;
            transform.position = Vector2.Lerp(start, target, t);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = target;
    }

    // ---- Helpers ----

    bool IsGrounded()
    {
        return Physics2D.Raycast(transform.position, Vector2.down, 0.1f, groundLayer);
    }

    // Stops horizontal movement and plays the idle animation.
    void Idle()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        animator.Play("Enemy Idle");
    }

    // Restores sprite and collider after a vanish-based attack.
    private void RestoreAfterVanish()
    {
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (bodyCollider != null) bodyCollider.enabled = true;
    }
}
