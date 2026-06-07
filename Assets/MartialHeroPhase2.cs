using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class MartialHeroPhase2 : MonoBehaviour, IAttackState
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

private bool canKnifeDrop = true;
private bool canGroundSlam = true;

[Header("Attack Cooldowns")]
public float knifeDropCooldown = 12f;
public float groundSlamCooldown = 10f;
    public float teleportKnifeCooldown = 8f;
    
    [Header("Spear Rain Phases")]
public GameObject spearPrefab;
public float spearSpawnY = 6f;
public float arenaLeftX = -8f;
public float arenaRightX = 8f;
public int numberOfSpears = 10;
public float spearSpacing = 1.6f;
public float delayBetweenSpears = 0.15f;

private float spearPhaseCooldown = 15f;
private float nextSpearPhaseTime = 0f;
private int currentSpearPhase = 1;

    private MartialHeroHealth health;
         public Transform spearSpawnPointLeft;

        public Transform spearSpawnPointRight;
     public Transform spearSpawnPointMiddle;

private float lastSpearAttackTime = -Mathf.Infinity;
private float spearAttackDelay = 5f; // guaranteed delay between spear attacks



[Header("Arena Bounds")]
public float arenaBottomY = -4.2f;






    private bool isActivated = false;
    private bool phase3Triggered = false;

private bool hasTriggeredFinalPhase = false;

    private bool isAttacking = false;
    public bool IsAttackingNow { get; private set; } = false;
    private int parryCountThisAttack = 0;
    private MartialHeroStun stunHandler;
    private string currentAttackPhase = "";
    private bool canChargeAttack = true;
    private bool appearBehindNext = true;
    public GameObject knifeProjectilePrefab;
public Transform leftSpawnPoint;
public Transform rightSpawnPoint;
    public float teleportAttackCooldown = 8f;
private float nextSpearPhaseAllowedTime = 0f;

    private bool canTeleportAttack = true;
private Queue<MartialHeroAttack> recentAttacks = new Queue<MartialHeroAttack>();
    private const int attackMemory = 2; // Remember last 2 attacks
    private Vector3 originalScale;
    private bool hasUsedPhase3 = false;
    private bool spearPhasesActive = false;
private bool hasTriggeredPhase3 = false;
    private Coroutine activeSpearRoutine = null; 
public GameObject smokeEffectPrefab;




    private enum MartialHeroAttack
    {
        None,
        TeleportThrow,
        RandomMelee,
        Charge,
        Dash,
        KnifeDrop,
        GroundSlam 
    }
    void Start()
    {
        health = GetComponent<MartialHeroHealth>();
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
   void OnEnable()
{
    Debug.Log("[MartialHeroPhase2] OnEnable called");

    if (player == null)
    {
        GameObject found = GameObject.FindGameObjectWithTag("Warrior");
        if (found != null)
        {
            player = found.transform;
            Debug.Log("[MartialHeroPhase2] Auto-assigned player from Warrior tag");
        }
        else
        {
            Debug.LogWarning("[MartialHeroPhase2] Couldn't auto-assign Warrior-tagged player");
        }
    }
    spriteRenderer.color = Color.white;
    StartCoroutine(DoKnifeDropAttack());
    ActivateBoss();
}





    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        stunHandler = GetComponent<MartialHeroStun>();
        originalScale = transform.localScale;
        rb.linearVelocity = Vector2.zero;
         if (health == null)
        health = GetComponent<MartialHeroHealth>();
    }
  private IEnumerator WaitForSpearCooldown()
{
    float timeSinceLast = Time.time - lastSpearAttackTime;

    if (float.IsNegativeInfinity(lastSpearAttackTime))
    {
        Debug.LogWarning("[Cooldown] lastSpearAttackTime was never set. Forcing immediate attack.");
        yield break;
    }

    float remaining = spearAttackDelay - timeSinceLast;

    if (remaining > 0f)
    {
        Debug.Log($"[Cooldown] Waiting {remaining:F2} seconds");
        yield return new WaitForSeconds(remaining);
    }
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

private IEnumerator LaunchSpearsFromSinglePoint(Vector2 origin, float width, int count, bool leftToRight)
    {
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1); // 0 to 1
            float x = leftToRight
                ? Mathf.Lerp(arenaLeftX, arenaRightX, t)
                : Mathf.Lerp(arenaRightX, arenaLeftX, t);
            float y = arenaBottomY;

            GameObject spear = Instantiate(spearPrefab, origin, Quaternion.identity);
spear.GetComponent<SpearProjectile>().Launch(origin, new Vector2(x, y));

            yield return new WaitForSeconds(delayBetweenSpears);
        }
    }
private List<Vector2> GenerateLandingPointsParabola(
    float startX, float endX, float baseY, float height, int count)
{
    List<Vector2> points = new List<Vector2>();

    for (int i = 0; i < count; i++)
    {
        float t = i / (float)(count - 1); // goes 0 to 1

        // Ease out horizontally: starts slow then accelerates right
        float x = Mathf.Lerp(startX, endX, Mathf.Pow(t, 1f));

        // Smooth drop: all spears fall down (y decreases)
        float y = baseY; // you can also curve this slightly if needed

        points.Add(new Vector2(x, y));
    }

    return points;
}

private List<Vector2> GenerateLandingPointsParabola_RightToLeft(
    float startX, float endX, float baseY, float height, int count)
{
    List<Vector2> points = new List<Vector2>();

    for (int i = 0; i < count; i++)
    {
        float t = i / (float)(count - 1);

        // Inverse easing for right to left (starts slow, speeds up to the left)
        float easedT = 1f - Mathf.Pow(1f - t, 1.5f);
        float x = Mathf.Lerp(startX, endX, easedT);

        float y = baseY; // optionally add vertical curvature if needed later

        points.Add(new Vector2(x, y));
    }

    return points;
}





    private IEnumerator DoSpearPhaseLeftToRight()
    {
        Debug.Log("[MartialHero] Spear Phase 1: Left to Right");

        Vector2 origin = spearSpawnPointLeft.position;

        int spearCount = 16;
        float spacing = 0.8f; // horizontal step size

        float totalWidth = spacing * (spearCount - 1); // spread out wide
        float extraLeft = -.1f;
        float extraRight = 4f;

        float startX = origin.x + extraLeft;
        float endX = startX + totalWidth + extraRight;


        List<Vector2> landingPoints = GenerateLandingPointsParabola(
            startX,
            endX,
            arenaBottomY,
            0f, // height unused
            spearCount
        );

        float maxDelay = 0.15f;
        float minDelay = 0.03f;

        for (int i = 0; i < landingPoints.Count; i++)
        {
            Vector2 target = landingPoints[i];
            GameObject spear = Instantiate(spearPrefab, origin, Quaternion.identity);
            spear.GetComponent<SpearProjectile>().Launch(origin, target);

            float t = i / (float)(landingPoints.Count - 1);
            float adjustedDelay = Mathf.Lerp(maxDelay, minDelay, t);

            yield return new WaitForSeconds(adjustedDelay);
            if (!health.isDead)
    lastSpearAttackTime = Time.time;
        }
    }








private IEnumerator DoSpearPhaseRightToLeft()
{
    Debug.Log("[MartialHero] Spear Phase 2: Right to Left");

    Vector2 origin = spearSpawnPointRight.position;

    int spearCount = 16;
    float spacing = 0.8f;

    float totalWidth = spacing * (spearCount - 1);

    float extraLeft = 4f;     // extend more to left
    float extraRight = -0.1f; // shift start slightly right

   float startX = origin.x;                // rightmost point
float endX = startX - totalWidth - 4f;

List<Vector2> landingPoints = GenerateLandingPointsParabola(
    startX,
    endX,
    arenaBottomY,
    0f,
    spearCount
);

    float maxDelay = 0.15f;
    float minDelay = 0.03f;

        for (int i = 0; i < landingPoints.Count; i++)
        {
            Vector2 target = landingPoints[i];
            GameObject spear = Instantiate(spearPrefab, origin, Quaternion.identity);
            spear.GetComponent<SpearProjectile>().Launch(origin, target);

            float t = i / (float)(landingPoints.Count - 1);
            float adjustedDelay = Mathf.Lerp(maxDelay, minDelay, t);

            yield return new WaitForSeconds(adjustedDelay);
        if (!health.isDead)
    lastSpearAttackTime = Time.time;
    }
}
private IEnumerator SpearPhaseLoop()
{
    Debug.Log("[MartialHero] Phase 2: Looping spear wave pattern");

    yield return new WaitForSeconds(2f); // Initial startup delay

    while (!health.isDead)
    {
        // Wait until not attacking or stunned
        float timeout = 10f;
        float waitStart = Time.time;

        while ((isAttacking || stunHandler.IsStunned()) && Time.time - waitStart < timeout)
            yield return null;

        if (Time.time - waitStart >= timeout)
            Debug.LogWarning("[SpearPhaseLoop] Waited too long to be idle");

        // Left-to-Right
        yield return StartCoroutine(FlashWarning(spearSpawnPointLeft.gameObject));
        yield return StartCoroutine(DoSpearPhaseLeftToRight());
        // Right-to-Left
        yield return StartCoroutine(FlashWarning(spearSpawnPointRight.gameObject));
        yield return StartCoroutine(DoSpearPhaseRightToLeft());

        // Final cooldown before next full wave
        lastSpearAttackTime = Time.time;
        float delay = Random.Range(5f, 10f);
        Debug.Log($"[Phase 2] Waiting {delay:F1}s before next spear wave");
        yield return new WaitForSeconds(delay);
    }
}


    private IEnumerator DoSpearPhaseSweepStraightFromMiddle()
{
    Debug.Log("[MartialHero] Spear Phase 3: Sweep outward from Middle (both sides)");

    Vector2 origin = spearSpawnPointMiddle.position;
    int spearPairs = 7; // 7 pairs = 14 total spears
    float spacing = 1.2f; // Wider spacing for better arena coverage
    float delay = 0.07f;

    for (int i = 1; i <= spearPairs; i++) // Start at 1 instead of 0 to skip center
    {
        float offset = spacing * i;

        // Right spear
        Vector2 rightTarget = new Vector2(origin.x + offset, arenaBottomY);
        GameObject spearRight = Instantiate(spearPrefab, origin, Quaternion.identity);
        spearRight.GetComponent<SpearProjectile>().Launch(origin, rightTarget);

        // Left spear
        Vector2 leftTarget = new Vector2(origin.x - offset, arenaBottomY);
        GameObject spearLeft = Instantiate(spearPrefab, origin, Quaternion.identity);
        spearLeft.GetComponent<SpearProjectile>().Launch(origin, leftTarget);

        yield return new WaitForSeconds(delay);

        if (!health.isDead)
            lastSpearAttackTime = Time.time;
    }
}

private IEnumerator AlternateSpearPhasePattern()
{
    Debug.Log("[MartialHero] Phase 3: Random alternating pattern");

    int lastRoll = -1; // Track last attack

    while (!health.isDead)
    {
        // Wait until boss is idle or timeout (prevents lock)
        float timeout = 10f;
        float waitStart = Time.time;

        while ((isAttacking || stunHandler.IsStunned()) && Time.time - waitStart < timeout)
            yield return null;

        if (Time.time - waitStart >= timeout)
            Debug.LogWarning("[Phase 3] Waited too long to be idle");

        // pick randomly, avoid repeating the last roll
        int roll;
        do {
            roll = Random.Range(0, 2); // 0 = double wave, 1 = middle sweep
        } while (roll == lastRoll);
        lastRoll = roll;

        // Perform the selected attack pattern
        if (roll == 0)
        {
            yield return StartCoroutine(FlashWarning(spearSpawnPointLeft.gameObject));
            yield return StartCoroutine(DoSpearPhaseLeftToRight());
            yield return StartCoroutine(FlashWarning(spearSpawnPointRight.gameObject));
            yield return StartCoroutine(DoSpearPhaseRightToLeft());
        }
        else
        {
            yield return StartCoroutine(FlashWarning(spearSpawnPointMiddle.gameObject));
            yield return StartCoroutine(DoSpearPhaseSweepStraightFromMiddle());
        }

        lastSpearAttackTime = Time.time;

        float delay = Random.Range(5f, 10f);
        Debug.Log($"[Phase 3] Waiting {delay:F1}s before next pattern...");
        yield return new WaitForSeconds(delay);
    }
}

public void LaunchParabola(Vector2 start, Vector2 end, float arcHeight, float travelTime = 0.5f)
{
    StartCoroutine(MoveParabola(start, end, arcHeight, travelTime));
}

private IEnumerator MoveParabola(Vector2 start, Vector2 end, float arcHeight, float duration)
{
    float elapsed = 0f;

    while (elapsed < duration)
    {
        float t = elapsed / duration;
        float height = Mathf.Sin(Mathf.PI * t) * arcHeight;

        Vector2 midPoint = Vector2.Lerp(start, end, t);
        Vector2 newPos = new Vector2(midPoint.x, midPoint.y + height);

        Vector2 dir = newPos - (Vector2)transform.position;
        if (dir != Vector2.zero)
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        transform.position = newPos;
        elapsed += Time.deltaTime;
        yield return null;
    }

    transform.position = end;
    Destroy(gameObject);
}


private IEnumerator DoFinalSpearPhaseFromSidesToCenter()
{
    Debug.Log("[MartialHero] Final Phase: Spears from both sides to center");

    int spearCount = 7;
    float spacing = 0.7f;
    float delay = 0.07f;
    float arcMultiplier = 2.5f;      // How strongly they cross center
    float arcHeightBase = 1.5f;      // Base arc height
    float arcHeightStep = 0.2f;      // How much taller each spear is

    float groundY = arenaBottomY;
    float spawnY = spearSpawnPointLeft.position.y;

    Vector2 leftBase = spearSpawnPointLeft.position;
    Vector2 rightBase = spearSpawnPointRight.position;

        for (int i = 0; i < spearCount; i++)
        {
            float arcHeight = arcHeightBase + (i * arcHeightStep);

            // left spear moves rightward past center
            Vector2 leftStart = new Vector2(leftBase.x - (i * spacing), spawnY);
            Vector2 leftTarget = new Vector2(leftBase.x + (i * spacing * arcMultiplier), groundY - 0.3f); // LOWER target Y

            GameObject leftSpear = Instantiate(spearPrefab, leftStart, Quaternion.identity);
            leftSpear.GetComponent<SpearProjectile>().LaunchParabola(leftStart, leftTarget, arcHeight, 0.4f);

            // right spear moves leftward past center
            Vector2 rightStart = new Vector2(rightBase.x + (i * spacing), spawnY);
            Vector2 rightTarget = new Vector2(rightBase.x - (i * spacing * arcMultiplier), groundY - 0.3f); // LOWER target Y

            GameObject rightSpear = Instantiate(spearPrefab, rightStart, Quaternion.identity);
            rightSpear.GetComponent<SpearProjectile>().LaunchParabola(rightStart, rightTarget, arcHeight, 0.4f);

            yield return new WaitForSeconds(delay);
        if (!health.isDead)
    lastSpearAttackTime = Time.time;
    }
}











   private IEnumerator FinalSpearPhaseRotation()
{
    Debug.Log("[MartialHero] Final Phase: Randomized spear attacks");
    int lastRoll = -1; // Initialize to an invalid value

    while (!health.isDead)
    {
        // Wait until not stunned or attacking (with timeout)
        float timeout = 10f;
        float waitStart = Time.time;

        while ((isAttacking || stunHandler.IsStunned()) && Time.time - waitStart < timeout)
        {
            if (health.isDead) yield break;
            yield return null;
        }
        if (health.isDead) yield break;

        if (Time.time - waitStart >= timeout)
            Debug.LogWarning("[SpearPhase] Waited too long to be idle");

        // pick a new roll that differs from the last
        int roll;
        do {
            roll = Random.Range(0, 3); // 0, 1, or 2
        } while (roll == lastRoll);
        lastRoll = roll;

        // trigger chosen spear attack
        switch (roll)
        {
            case 0:
                // Combo: Left - Right
                yield return StartCoroutine(FlashWarning(spearSpawnPointLeft.gameObject));
                yield return StartCoroutine(DoSpearPhaseLeftToRight());
                yield return StartCoroutine(FlashWarning(spearSpawnPointRight.gameObject));
                yield return StartCoroutine(DoSpearPhaseRightToLeft());
                break;

            case 1:
                yield return StartCoroutine(FlashWarning(spearSpawnPointMiddle.gameObject));
                yield return StartCoroutine(DoSpearPhaseSweepStraightFromMiddle());
                break;

            case 2:
                GameObject[] sides = new GameObject[]
                {
                    spearSpawnPointLeft.gameObject,
                    spearSpawnPointRight.gameObject
                };
                yield return StartCoroutine(FlashMultipleWarnings(sides));
                yield return StartCoroutine(DoFinalSpearPhaseFromSidesToCenter());
                break;
        }

        lastSpearAttackTime = Time.time;

        // Wait 5-10 seconds before next random spear attack
        float delay = Random.Range(5f, 10f);
        Debug.Log($"[SpearPhase] Waiting {delay:F1}s before next attack...");
        yield return new WaitForSeconds(delay);
    }
}


private IEnumerator FlashMultipleWarnings(GameObject[] spawnPoints, float duration = 1f)
{
    List<SpriteRenderer> renderers = new List<SpriteRenderer>();

    foreach (GameObject obj in spawnPoints)
    {
        if (obj == null) continue;
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = true;
            renderers.Add(sr);
        }
    }

    yield return new WaitForSeconds(duration);

    foreach (SpriteRenderer sr in renderers)
    {
        sr.enabled = false;
    }
}










private IEnumerator DoGroundAttackCombo()
{
    isAttacking = true;
    rb.linearVelocity = Vector2.zero;

    Debug.Log("[MartialHero] Starting Ground Slam Attack");

    // --- Phase 1: Play charge animation + flash blue ---
    animator.Play("ChargeAttack");
    spriteRenderer.color = Color.blue;
    yield return new WaitForSeconds(0.2f);

    // --- Phase 2: Reset color and disappear ---
    spriteRenderer.color = Color.white;
// DISAPPEAR with smoke
if (smokeEffectPrefab != null)
{
    GameObject smoke = Instantiate(smokeEffectPrefab, transform.position, Quaternion.identity);
    Destroy(smoke, 1f);
}
spriteRenderer.enabled = false;
    float dropHeight = 6f;
    Vector3 abovePlayer = player.position + new Vector3(0f, dropHeight, 0f);
    transform.position = abovePlayer;
    transform.up = Vector2.down;

    yield return new WaitForSeconds(0.1f); // Optional dramatic pause

    // --- Phase 3: Reappear above player and slam ---
    spriteRenderer.enabled = true;
    animator.Play("GroundAttack");

    float fallDuration = 0.35f;
    float elapsed = 0f;
    Vector3 targetPosition = new Vector3(player.position.x, player.position.y, transform.position.z);

    while (elapsed < fallDuration)
    {
        transform.position = Vector3.Lerp(abovePlayer, targetPosition, elapsed / fallDuration);
        elapsed += Time.deltaTime;
        yield return null;
    }

    transform.position = targetPosition;

    // not parryable
    currentAttackPhase = "GroundSlam";
    IsAttackingNow = true;

    yield return new WaitForSeconds(0.4f);
    IsAttackingNow = false;

    // Reset rotation and idle
    transform.rotation = Quaternion.identity;
    animator.Play("Idle");

    StartCoroutine(ResetGroundSlamCooldown());
    yield return new WaitForSeconds(1f);
    isAttacking = false;
}












private IEnumerator DoKnifeDropAttack()
{
    isAttacking = true;
    rb.linearVelocity = Vector2.zero;

    Debug.Log("[MartialHero] Starting Knife Drop Attack");

    // DISAPPEAR
// DISAPPEAR with smoke
if (smokeEffectPrefab != null)
{
    GameObject smoke = Instantiate(smokeEffectPrefab, transform.position, Quaternion.identity);
    Destroy(smoke, 1f);
}
spriteRenderer.enabled = false;    yield return new WaitForSeconds(0.2f);

    // TELEPORT ABOVE PLAYER
    Vector3 highAbovePlayer = player.position + new Vector3(0f, 6f, 0f);
    transform.position = highAbovePlayer;
    transform.up = Vector2.down; // Face downward

    // REAPPEAR & ATTACK DOWNWARD
    spriteRenderer.enabled = true;
    animator.Play("Attack3");

    yield return new WaitForSeconds(0.3f);

    // SPAWN BLUE UNPARRYABLE KNIFE
    GameObject knife = Instantiate(knifeProjectilePrefab, transform.position, Quaternion.identity);
    var proj = knife.GetComponent<SamuraiProjectile>();
    proj.SetDirection(Vector2.down);

    // OPTIONAL: make it blue + unparryable
    proj.SetParryable(false); // You must implement this if needed
    proj.GetComponent<SpriteRenderer>().color = Color.blue;

    // GIVE TIME TO REACT
    yield return new WaitForSeconds(0.5f);

    // DISAPPEAR AGAIN BEFORE FOLLOW-UP
// DISAPPEAR with smoke
if (smokeEffectPrefab != null)
{
    GameObject smoke = Instantiate(smokeEffectPrefab, transform.position, Quaternion.identity);
    Destroy(smoke, 1f);
}
spriteRenderer.enabled = false;    yield return new WaitForSeconds(0.5f);

    // TELEPORT NEXT TO PLAYER
    Vector3 sideOffset = new Vector3((player.position.x > transform.position.x ? -1.5f : 1.5f), 0f, 0f);
    transform.position = player.position + sideOffset;
    transform.rotation = Quaternion.identity;
    FacePlayer();

    // REAPPEAR AND PERFORM FOLLOW-UP ATTACK
    spriteRenderer.enabled = true;
    animator.Play("Attack1");

    currentAttackPhase = "Attack1a";
    IsAttackingNow = true;
    EnableParryableWindow(0.3f); // This one IS parryable

    yield return new WaitForSeconds(0.5f);
    IsAttackingNow = false;

    animator.Play("Idle");

    yield return new WaitForSeconds(attackDelay);
    StartCoroutine(ResetKnifeDropCooldown());
    yield return new WaitForSeconds(1f);
    isAttacking = false;
}

    private IEnumerator ResetGroundSlamCooldown()
{
    yield return new WaitForSeconds(groundSlamCooldown);
    canGroundSlam = true;
}


    private IEnumerator ResetKnifeDropCooldown()
    {
        yield return new WaitForSeconds(knifeDropCooldown);
        canKnifeDrop = true;
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
// DISAPPEAR with smoke
if (smokeEffectPrefab != null)
{
    GameObject smoke = Instantiate(smokeEffectPrefab, transform.position, Quaternion.identity);
    Destroy(smoke, 1f);
}
spriteRenderer.enabled = false;        yield return new WaitForSeconds(0.3f);

        float buffer = 0.5f;
        float playerX = player.position.x;
        float distanceFromLeft = playerX - minX;
        float distanceFromRight = maxX - playerX;

        float side;

        // Teleport to opposite side if player is near wall
        if (distanceFromLeft < knifeTeleportDistance + buffer)
        {
            side = 1f; // Near left - go right
        }
        else if (distanceFromRight < knifeTeleportDistance + buffer)
        {
            side = -1f; // Near right - go left
        }
        else
        {
            side = appearBehindNext ? -1f : 1f;
            appearBehindNext = !appearBehindNext;
        }

        float targetX = Mathf.Clamp(playerX + side * knifeTeleportDistance, minX + buffer, maxX - buffer);

        // raycast down to check for ground at teleport position
        Vector2 rayOrigin = new Vector2(targetX, transform.position.y + 2f);
        RaycastHit2D groundCheck = Physics2D.Raycast(rayOrigin, Vector2.down, 5f, LayerMask.GetMask("Ground"));

        Vector3 finalTarget;

        if (!groundCheck.collider)
        {
            // no ground - don't teleport, throw knife from current position
            Debug.Log("[MartialHero] No ground found - throwing knife from current position.");
            finalTarget = transform.position;
        }
        else
        {
            // ground found - teleport
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

// DISAPPEAR with smoke
if (smokeEffectPrefab != null)
{
    GameObject smoke = Instantiate(smokeEffectPrefab, transform.position, Quaternion.identity);
    Destroy(smoke, 1f);
}
spriteRenderer.enabled = false;
    float playerX = player.position.x;
    float distanceFromLeft = playerX - minX;
    float distanceFromRight = maxX - playerX;

    float side = 0f;
    float buffer = 0.5f;

    if (distanceFromLeft < teleportDistance + buffer)
    {
        side = 1f; // Near left wall - teleport right
    }
    else if (distanceFromRight < teleportDistance + buffer)
    {
        side = -1f; // Near right wall - teleport left
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
        // no ground - reappear in place and throw knife instead
        Debug.Log("[MartialHero] ChargeAttack failed - no ground, switching to knife throw");

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

    // ground found - teleport
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
    }
    private IEnumerator FlashWarning(GameObject spawnPoint, float flashDuration = 1.0f)
{
    if (spawnPoint == null) yield break;

    SpriteRenderer sr = spawnPoint.GetComponent<SpriteRenderer>();
    if (sr == null) yield break;

    sr.enabled = true;
    yield return new WaitForSeconds(flashDuration);
    sr.enabled = false;
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

        // phase 2 trigger at 50% health
        if (!spearPhasesActive && health.currentHealth <= health.maxHealth * 0.5f && !isAttacking && Time.time >= nextSpearPhaseAllowedTime)
        {
            spearPhasesActive = true;

            if (activeSpearRoutine != null)
                StopCoroutine(activeSpearRoutine);

            activeSpearRoutine = StartCoroutine(SpearPhaseLoop());
            nextSpearPhaseAllowedTime = Time.time + 10f;
        }

        // phase 3 trigger at 35% health
        if (!hasTriggeredPhase3 && health.currentHealth <= health.maxHealth * 0.35f && !isAttacking && Time.time >= nextSpearPhaseAllowedTime)
        {
            hasTriggeredPhase3 = true;

            if (activeSpearRoutine != null)
                StopCoroutine(activeSpearRoutine);

            activeSpearRoutine = StartCoroutine(AlternateSpearPhasePattern());
            nextSpearPhaseAllowedTime = Time.time + 10f;
        }

        // final phase at 25% health
        if (!hasTriggeredFinalPhase && health.currentHealth <= health.maxHealth * 0.25f && !isAttacking && Time.time >= nextSpearPhaseAllowedTime)
        {
            hasTriggeredFinalPhase = true;

            if (activeSpearRoutine != null)
                StopCoroutine(activeSpearRoutine);

            activeSpearRoutine = StartCoroutine(FinalSpearPhaseRotation());
            nextSpearPhaseAllowedTime = Time.time + 10f;
        }


            // regular attack logic
            float dist = Vector2.Distance(transform.position, player.position);
            float verticalDist = Mathf.Abs(transform.position.y - player.position.y);

            if (!isAttacking)
            {
                float roll = Random.value;

                if (canKnifeDrop && roll < 0.2f && !WasRecentlyUsed(MartialHeroAttack.KnifeDrop))
                {
                    RecordAttack(MartialHeroAttack.KnifeDrop);
                    yield return StartCoroutine(DoKnifeDropAttack());
                }
                else if (canTeleportAttack && roll >= 0.2f && roll < 0.4f && !WasRecentlyUsed(MartialHeroAttack.TeleportThrow))
                {
                    RecordAttack(MartialHeroAttack.TeleportThrow);
                    yield return StartCoroutine(TeleportAndThrowKnife());
                }
                else if (canGroundSlam && roll >= 0.4f && roll < 0.6f && !WasRecentlyUsed(MartialHeroAttack.GroundSlam))
                {
                    RecordAttack(MartialHeroAttack.GroundSlam);
                    yield return StartCoroutine(DoGroundAttackCombo());
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
                else if (canDashAttack && dist > 1.5f && dist <= 5f && !WasRecentlyUsed(MartialHeroAttack.Dash))
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

    // flash blue before attack
    spriteRenderer.color = Color.blue;
    yield return new WaitForSeconds(0.2f);
    spriteRenderer.color = Color.white;

    // play attack animation
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
    if (parryHitboxPrefab == null)
    {
        Debug.LogError("[Parry] Hitbox prefab is missing.");
        return;
    }

    Debug.Log($"[Parry] Spawning hitbox at {transform.position} for {duration}s during {currentAttackPhase}");

    GameObject hitbox = Instantiate(parryHitboxPrefab, transform);
    hitbox.transform.localPosition = Vector3.zero;
    Destroy(hitbox, duration);
}


    public void RegisterParry()
{
    if (!IsAttackingNow)
    {
        Debug.Log("[MartialHero] Ignored parry - not attacking.");
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
    public bool IsInParryableState()
    {
        return currentAttackPhase == "Attack1a" ||
               currentAttackPhase == "Attack1b" ||
               currentAttackPhase == "Attack2" ||
               currentAttackPhase == "ChargeAttack" ||
               currentAttackPhase == "DashAttack" ||
               currentAttackPhase == "Attack3";
}



    public bool IsBlueFlashing() => spriteRenderer != null && spriteRenderer.color == Color.blue;
    public bool IsAttacking() => IsAttackingNow;
    public void Parry() => RegisterParry();
}
