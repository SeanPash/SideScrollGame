using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class MartialHeroBehavior : MonoBehaviour, IAttackState
{
    [Header("References")]
    public Transform player;
    public Rigidbody2D rb;
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public GameObject parryHitboxPrefab;

    [Header("Settings")]
    public float moveSpeed = 3f;
    public float attackRange = 2f;
    public float attackDelay = 1.5f;
    public float chargeCooldown = 10f;
[Header("Teleport Settings")]
public float teleportDistance = 3f; // Used for charge attack
public float knifeTeleportDistance = 4.5f; // NEW: Longer teleport for knife attack

    [Header("Teleport Bounds")]
public float minX = -10f;
public float maxX = 10f;

[Header("Dash Attack Settings")]
public float dashDistance = 5f;
public float dashSpeed = 12f;
public float dashDuration = 0.35f;
public float dashAttackCooldown = 8f;
private bool canDashAttack = true;



    private bool isActivated = false;
    private bool isAttacking = false;
    public bool IsAttackingNow { get; private set; } = false;
    private int parryCountThisAttack = 0;
    private MartialHeroStun stunHandler;
    public string currentAttackPhase = "";
    private bool canChargeAttack = true;
    private bool appearBehindNext = true;
    public GameObject knifeProjectilePrefab;
public Transform leftSpawnPoint;
public Transform rightSpawnPoint;
public float teleportAttackCooldown = 8f;
    private bool canTeleportAttack = true;
private Queue<MartialHeroAttack> recentAttacks = new Queue<MartialHeroAttack>();
    private const int attackMemory = 2; // Remember last 2 attacks
    private Vector3 originalScale;
public GameObject smokeEffectPrefab;



    private enum MartialHeroAttack
    {
        None,
        TeleportThrow,
        RandomMelee,
        Charge,
        Dash
    }

private bool WasRecentlyUsed(MartialHeroAttack attack)
{
    return recentAttacks.Contains(attack);
}

private void RecordAttack(MartialHeroAttack attack)
{
    recentAttacks.Enqueue(attack);
    if (recentAttacks.Count > attackMemory)
        recentAttacks.Dequeue();
}


    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        stunHandler = GetComponent<MartialHeroStun>();
            originalScale = transform.localScale;
        rb.linearVelocity = Vector2.zero;
    }
void Update()
{
    if (player != null)
        FacePlayer(); // Constantly face the player, even when idle or attacking
}


    public void ActivateBoss()
    {
        if (isActivated) return;
        StartCoroutine(BossBehaviorLoop());

        GameObject[] warriors = GameObject.FindGameObjectsWithTag("Warrior");
        if (warriors.Length > 0)
        {
            GameObject closest = warriors[0];
            float closestDistance = Vector2.Distance(transform.position, closest.transform.position);

            foreach (GameObject w in warriors)
            {
                // Skip prefabs and inactive/destroyed objects
                if (!w.scene.IsValid() || !w.activeInHierarchy) continue;

                float dist = Vector2.Distance(transform.position, w.transform.position);
                if (dist < closestDistance)
                {
                    closest = w;
                    closestDistance = dist;
                }
            }

            player = closest.transform;
            isActivated = true;
            Debug.Log("[MartialHero] Targeting closest warrior: " + player.name);
        }
        else
        {
            Debug.LogWarning("[MartialHero] No Warrior-tagged player found!");
        }
    }


    private void MoveTowardPlayer()
    {
        if (player == null) return;
        Vector2 dir = (player.position.x > transform.position.x) ? Vector2.right : Vector2.left;
        rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);

        if (!isAttacking)
            animator.Play("Run");
    }
private IEnumerator DoDashAttack()
{
    isAttacking = true;
    canDashAttack = false;
    rb.linearVelocity = Vector2.zero;

    FacePlayer();

    // 1. Play "Charge" animation and wait 2 seconds
    animator.Play("Charge");
    yield return new WaitForSeconds(.2f);

    // 2. Flash the boss red
    spriteRenderer.color = Color.red;
    yield return new WaitForSeconds(0.1f); // Flash duration
    spriteRenderer.color = Color.white;

    // 3. Dash toward player (just before their position)
    float stopDistance = 1f;
    Vector2 target = new Vector2(
        player.position.x + (player.position.x > transform.position.x ? -stopDistance : stopDistance),
        rb.position.y
    );

    float dashTimer = 0f;
    float maxDashDuration = 1.0f;

    while (Mathf.Abs(transform.position.x - target.x) > 0.1f && dashTimer < maxDashDuration)
    {
        if (GetComponent<MartialHeroHealth>().isDead)
        {
            rb.linearVelocity = Vector2.zero;
            yield break;
        }

        Vector2 dir = (target - (Vector2)transform.position).normalized;
        rb.linearVelocity = new Vector2(dir.x * dashSpeed, rb.linearVelocity.y);
        dashTimer += Time.deltaTime;
        yield return null;
    }

    rb.linearVelocity = Vector2.zero;

    // 4. Attack3 and parry window
    if (!GetComponent<MartialHeroHealth>().isDead)
        animator.Play("Attack3");

    currentAttackPhase = "DashAttack";
    IsAttackingNow = true;

    EnableParryableWindow(0.05f);

    yield return new WaitForSeconds(0.5f);

    IsAttackingNow = false;
    animator.Play("Idle");

    yield return new WaitForSeconds(attackDelay);

    isAttacking = false;
    currentAttackPhase = "";

    StartCoroutine(ResetDashAttackCooldown());
}






private IEnumerator ResetDashAttackCooldown()
{
    yield return new WaitForSeconds(dashAttackCooldown);
    canDashAttack = true;
}


    private IEnumerator TeleportAndThrowKnife()
    {
        canTeleportAttack = false;
        isAttacking = true;

        // Disappear
        if (smokeEffectPrefab != null)
    Instantiate(smokeEffectPrefab, transform.position, Quaternion.identity);
        spriteRenderer.enabled = false;
        yield return new WaitForSeconds(0.3f);

        float buffer = 0.5f;
        float playerX = player.position.x;
        float distanceFromLeft = playerX - minX;
        float distanceFromRight = maxX - playerX;

        float side;

        // Teleport to opposite side if player is near wall
        if (distanceFromLeft < knifeTeleportDistance + buffer)
        {
            side = 1f; // Near left → go right
        }
        else if (distanceFromRight < knifeTeleportDistance + buffer)
        {
            side = -1f; // Near right → go left
        }
        else
        {
            side = appearBehindNext ? -1f : 1f;
            appearBehindNext = !appearBehindNext;
        }

        float targetX = Mathf.Clamp(playerX + side * knifeTeleportDistance, minX + buffer, maxX - buffer);

        // ✅ Raycast down to check for ground at teleport spot
        Vector2 rayOrigin = new Vector2(targetX, transform.position.y + 2f);
        RaycastHit2D groundCheck = Physics2D.Raycast(rayOrigin, Vector2.down, 5f, LayerMask.GetMask("Ground"));

        Vector3 finalTarget;

        if (!groundCheck.collider)
        {
            // ❌ No ground — don't teleport, just reappear and throw knife
            Debug.Log("[MartialHero] No ground found — throwing knife from current position.");
            finalTarget = transform.position;
        }
        else
        {
            // ✅ Safe to teleport
            finalTarget = new Vector3(targetX, groundCheck.point.y + 1f, transform.position.z);
            transform.position = finalTarget;
        }

        // Reappear
        spriteRenderer.enabled = true;
        FacePlayer();
        yield return new WaitForSeconds(0.3f);

        // Throw knife
        animator.Play("Attack3");
        yield return new WaitForSeconds(0.3f);

        Transform spawnPoint = transform.localScale.x > 0 ? rightSpawnPoint : leftSpawnPoint;
        GameObject knife = Instantiate(knifeProjectilePrefab, spawnPoint.position, Quaternion.identity);
        knife.GetComponent<SamuraiProjectile>().SetDirection(transform.localScale.x > 0 ? Vector2.right : Vector2.left);

        yield return new WaitForSeconds(0.3f);
        animator.Play("Idle");
        isAttacking = false;
        currentAttackPhase = "";


        StartCoroutine(KnifeThrowCooldown());
        yield return new WaitForSeconds(1f);
    }





private IEnumerator KnifeThrowCooldown()
{
    yield return new WaitForSeconds(teleportAttackCooldown);
    canTeleportAttack = true;
}





  private IEnumerator DoChargeAttack()
{
    isAttacking = true;
    canChargeAttack = false;
    rb.linearVelocity = Vector2.zero;

    if (!GetComponent<MartialHeroHealth>().isDead)
        animator.Play("ChargeAttack");

    spriteRenderer.color = Color.white;
    yield return new WaitForSeconds(0.6f);

    spriteRenderer.color = Color.red;
    yield return new WaitForSeconds(0.2f);
    // Smoke effect when disappearing
if (smokeEffectPrefab != null)
    Instantiate(smokeEffectPrefab, transform.position, Quaternion.identity);
    spriteRenderer.enabled = false;

    float playerX = player.position.x;
    float distanceFromLeft = playerX - minX;
    float distanceFromRight = maxX - playerX;

    float side = 0f;
    float buffer = 0.5f;

    if (distanceFromLeft < teleportDistance + buffer)
    {
        side = 1f; // Near left wall → teleport right
    }
    else if (distanceFromRight < teleportDistance + buffer)
    {
        side = -1f; // Near right wall → teleport left
    }
    else
    {
        side = appearBehindNext ? -1f : 1f;
        appearBehindNext = !appearBehindNext;
    }

    float targetX = Mathf.Clamp(playerX + side * teleportDistance, minX + buffer, maxX - buffer);

    Vector2 rayOrigin = new Vector2(targetX, transform.position.y + 2f);
    RaycastHit2D groundCheck = Physics2D.Raycast(rayOrigin, Vector2.down, 5f, LayerMask.GetMask("Ground"));

    if (!groundCheck.collider)
    {
        // ❌ No ground — reappear in place and throw knife instead
        Debug.Log("[MartialHero] ChargeAttack failed — no ground, switching to knife throw");

        spriteRenderer.enabled = true;
        spriteRenderer.color = Color.white;
        FacePlayer();

        yield return new WaitForSeconds(0.3f);

        animator.Play("Attack3");
        yield return new WaitForSeconds(0.3f);

        Transform spawnPoint = transform.localScale.x > 0 ? rightSpawnPoint : leftSpawnPoint;
        GameObject knife = Instantiate(knifeProjectilePrefab, spawnPoint.position, Quaternion.identity);
        knife.GetComponent<SamuraiProjectile>().SetDirection(transform.localScale.x > 0 ? Vector2.right : Vector2.left);

        yield return new WaitForSeconds(0.3f);
        animator.Play("Idle");
        isAttacking = false;
        currentAttackPhase = "";


        StartCoroutine(ResetChargeCooldown());
        yield break;
    }

    // ✅ Safe to teleport
    float targetY = groundCheck.point.y + 1f;
    Vector3 finalTarget = new Vector3(targetX, targetY, transform.position.z);
    transform.position = finalTarget;

    spriteRenderer.enabled = true;
    spriteRenderer.color = Color.white;

    FacePlayer();

    currentAttackPhase = "ChargeAttack";
    IsAttackingNow = true;
    yield return new WaitForSeconds(0.2f);

    EnableParryableWindow(0.25f);
    yield return new WaitForSeconds(0.35f);
    IsAttackingNow = false;

    if (parryCountThisAttack >= 1)
        stunHandler.Stun(0.7f);

    animator.Play("Idle");
    yield return new WaitForSeconds(attackDelay);
    isAttacking = false;
    currentAttackPhase = "";


    StartCoroutine(ResetChargeCooldown());
}




    private IEnumerator ResetChargeCooldown()
    {
        yield return new WaitForSeconds(chargeCooldown);
        canChargeAttack = true;
    }

    private IEnumerator DoRandomAttack()
    {
        FacePlayer();
        isAttacking = true;
        parryCountThisAttack = 0;

        int rand = Random.Range(1, 4);
        if (rand == 1)
        {
            yield return StartCoroutine(DoAttack1a());
            yield return new WaitForSeconds(0.4f);
            yield return StartCoroutine(DoAttack1b());
        }
        else if (rand == 2)
        {
            yield return StartCoroutine(DoAttack2());
        }
        else
        {
            yield return StartCoroutine(DoAttack3());
        }

        yield return new WaitForSeconds(attackDelay);
        isAttacking = false;
        currentAttackPhase = "";

    }

private IEnumerator BossBehaviorLoop()
{
    while (!GetComponent<MartialHeroHealth>().isDead)
    {
        if (stunHandler.IsStunned())
        {
            yield return null;
            continue;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        if (!isAttacking)
{
    float roll = Random.value;

    if (canTeleportAttack && roll < 0.4f && !WasRecentlyUsed(MartialHeroAttack.TeleportThrow))
    {
        RecordAttack(MartialHeroAttack.TeleportThrow);
        yield return StartCoroutine(TeleportAndThrowKnife());
    }
    else if (canChargeAttack && dist > 2f && dist < 6f && !WasRecentlyUsed(MartialHeroAttack.Charge))
    {
        RecordAttack(MartialHeroAttack.Charge);
        yield return StartCoroutine(DoChargeAttack());
    }
    else if (dist <= attackRange && !WasRecentlyUsed(MartialHeroAttack.RandomMelee))
    {
        RecordAttack(MartialHeroAttack.RandomMelee);
        yield return StartCoroutine(DoRandomAttack());
    }
else if (canDashAttack && dist > 2.5f && dist <= 5f && !WasRecentlyUsed(MartialHeroAttack.Dash))
{
    RecordAttack(MartialHeroAttack.Dash);
    yield return StartCoroutine(DoDashAttack());
}
                else
                {
                    MoveTowardPlayer();
                    RecordAttack(MartialHeroAttack.None);
                }
}
        yield return null;
    }
}




    private IEnumerator DoAttack1a()
    {
        currentAttackPhase = "Attack1a";
        if (!GetComponent<MartialHeroHealth>().isDead)
            animator.Play("Attack1");
        yield return new WaitForSeconds(0.2f);
        IsAttackingNow = true;
        EnableParryableWindow(0.05f);
        yield return new WaitForSeconds(0.3f);
        IsAttackingNow = false;
    }

    private IEnumerator DoAttack1b()
    {
        currentAttackPhase = "Attack1b";
        if (!GetComponent<MartialHeroHealth>().isDead)
            animator.Play("Attack1Part2");
        yield return new WaitForSeconds(0.15f);
        IsAttackingNow = true;
        EnableParryableWindow(0.05f);
        yield return new WaitForSeconds(0.3f);
        IsAttackingNow = false;

        if (parryCountThisAttack >= 2)
            stunHandler.Stun(.7f);

        animator.Play("Idle");
    }

    private IEnumerator DoAttack2()
    {
        currentAttackPhase = "Attack2";
        if (!GetComponent<MartialHeroHealth>().isDead)
            animator.Play("Attack2");
        yield return new WaitForSeconds(0.55f);

        IsAttackingNow = true;
        EnableParryableWindow(0.2f);
        yield return new WaitForSeconds(0.4f);
        IsAttackingNow = false;

        if (parryCountThisAttack >= 1)
            stunHandler.Stun(.7f);

        yield return new WaitForSeconds(0.4f);
        animator.Play("Idle");
    }

   private IEnumerator DoAttack3()
{
    isAttacking = true;
    currentAttackPhase = "Attack3";

    // 🔹 Flash blue briefly BEFORE attack
    spriteRenderer.color = Color.blue;
    yield return new WaitForSeconds(0.2f);
    spriteRenderer.color = Color.white;

    // ▶️ Now play attack animation
    if (!GetComponent<MartialHeroHealth>().isDead)
        animator.Play("Attack3");

    IsAttackingNow = true;
    yield return new WaitForSeconds(0.5f); // Duration of the animation

    IsAttackingNow = false;
    animator.Play("Idle");
    currentAttackPhase = "";
    isAttacking = false;
}

    private void EnableParryableWindow(float duration)
    {
        if (parryHitboxPrefab == null) return;

        Debug.Log($"[MartialHero] Parry window opened during {currentAttackPhase} for {duration} seconds");
GameObject hitbox = Instantiate(parryHitboxPrefab, transform);
        hitbox.transform.localPosition = Vector3.zero;
        Destroy(hitbox, duration);
    }

    public void RegisterParry()
{
    if (!IsAttackingNow)
    {
        Debug.Log("[MartialHero] Ignored parry — not attacking.");
        return;
    }

    parryCountThisAttack++;
    Debug.Log($"[MartialHero] Registered parry during {currentAttackPhase}, count = {parryCountThisAttack}");

    switch (currentAttackPhase)
    {
        case "Attack1b":
            if (parryCountThisAttack >= 2)
                stunHandler.Stun(0.7f);
            break;

        case "ChargeAttack":
            if (parryCountThisAttack >= 1)
                stunHandler.Stun(0.7f);
            break;

        case "DashAttack":
            if (parryCountThisAttack >= 1)
                stunHandler.Stun(0.7f);
            break;

        default:
            Debug.Log($"[MartialHero] Parry registered, but no stun allowed during {currentAttackPhase}.");
            break;
    }
}




    private void FacePlayer()
    {
        if (player == null) return;
        Vector3 scale = transform.localScale;
scale.x = (player.position.x > transform.position.x) ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    public bool IsCurrentlyAttacking()
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsName("Attack1") ||
               animator.GetCurrentAnimatorStateInfo(0).IsName("Attack2") ||
               animator.GetCurrentAnimatorStateInfo(0).IsName("Attack3") ||
               animator.GetCurrentAnimatorStateInfo(0).IsName("ChargeAttack");
    }
    private bool IsInParryableState()
    {
        return currentAttackPhase == "Attack1a" ||
        currentAttackPhase == "Attack1b" ||
        currentAttackPhase == "Attack2" ||
        currentAttackPhase == "ChargeAttack" ||
        currentAttackPhase == "DashAttack";
    }
    public bool CanBeParried()
    {
        return currentAttackPhase == "Attack1" ||
               currentAttackPhase == "Attack2" ||
               currentAttackPhase == "ChargeAttack" ||
               currentAttackPhase == "DashAttack";
    }



    public bool IsBlueFlashing() => spriteRenderer != null && spriteRenderer.color == Color.blue;
    public bool IsAttacking() => IsAttackingNow;
    public void Parry() => RegisterParry();
}
