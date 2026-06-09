using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CrabBossBehaviorPhase1 : MonoBehaviour, IAttackState
{
    public Transform player;
    public float moveSpeed = 2f;
    public float attackRange = 5f;

    public GameObject groundShotPrefab;
    public GameObject redLineShotPrefab;
    public GameObject arcProjectilePrefab;
    public Transform groundPearlSpawnPoint;
    public Transform arcPearlSpawnPoint;
    public GameObject redLinePrefab;

    private Rigidbody2D rb;
    private Animator animator;

    private bool canAttackA = true;
    private bool canAttackB = true;
    private bool canAttackC = true;
    private bool canGroundShot = true;
    private bool isAttacking = false;
    private bool facingRight = true;
    private float flipThreshold = 0.1f;
    private float nextAttackTime = 0f;
    public float attackDelay = 3f;
    private string currentAnim = "";
    private bool isParryableAttack = false;
    [SerializeField] private GameObject stunIcon;





    private SpriteRenderer sr;
    private bool isActive = false;  // <- New flag to prevent early logic

    [Header("AttackB Parry Toggle")]
    [Range(0f, 1f)] public float unParryableChance = 0.3f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        sr = GetComponent<SpriteRenderer>();

    }

    public void ActivatePhase()
    {
        GameObject[] warriors = GameObject.FindGameObjectsWithTag("Warrior");
        if (warriors.Length > 0)
        {
            GameObject closest = warriors[0];
            float closestDistance = Vector2.Distance(transform.position, closest.transform.position);

            foreach (GameObject w in warriors)
            {
                float dist = Vector2.Distance(transform.position, w.transform.position);
                if (dist < closestDistance)
                {
                    closest = w;
                    closestDistance = dist;
                }
            }

            player = closest.transform;
            isActive = true;
            Debug.Log("[CrabBoss] Targeting closest warrior: " + player.name);

        }
        else
        {
            Debug.LogWarning("[CrabBoss] No Warrior-tagged player found!");
        }
    }

    private void Update()
    {
        if (!isActive || player == null) return;

        float directionX = player.position.x - transform.position.x;
        float distance = Vector2.Distance(transform.position, player.position);

        FaceDirection();

        // Cancel everything else if AttackA is ready
        if (!isAttacking && canAttackA && Time.time >= nextAttackTime)
        {
            StartCoroutine(DoAttackA());
            return;
        }

        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (distance > attackRange)
        {
            Vector2 dir = new Vector2(directionX, 0).normalized;
            rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);

            if (Mathf.Abs(rb.linearVelocity.x) > 0.2f)
            {
                PlayAnimation("CrabRun");
            }

            // Remove AttackA from this logic
            if (Time.time >= nextAttackTime && (canGroundShot || canAttackC))
            {
                TryRandomRangedAttack();
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            PlayAnimation("Crab_Idle");

            if (Time.time >= nextAttackTime && canAttackB)
            {
                StartCoroutine(DoAttackB());
            }
        }
    }

    private void PlayAnimation(string animName)
    {
        if (currentAnim != animName)
        {
            Debug.Log("[CrabBoss] Playing animation: " + animName);
            animator.Play(animName);
            currentAnim = animName;
        }
    }

    private void FaceDirection()
    {
        if (player == null) return;

        float xDiff = player.position.x - transform.position.x;
        if (Mathf.Abs(xDiff) < flipThreshold) return;

        bool faceLeft = xDiff < 0f;
        Vector3 localScale = transform.localScale;
        localScale.x = Mathf.Abs(localScale.x) * (faceLeft ? -1 : 1);
        transform.localScale = localScale;

        facingRight = !faceLeft;
    }

    private void TryRandomRangedAttack()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        List<int> available = new List<int>();

        if (distance >= 6f && canAttackC) available.Add(2);
        if (canGroundShot) available.Add(1);

        if (available.Count == 0) return;

        int choice = available[Random.Range(0, available.Count)];
        Debug.Log("Crab ranged attack roll: " + choice);

        isAttacking = true;
        nextAttackTime = Time.time + attackDelay;

        switch (choice)
        {
            case 1:
                canGroundShot = false;
                StartCoroutine(DoGroundShot());
                break;
            case 2:
                canAttackC = false;
                StartCoroutine(DoAttackC());
                break;
        }
    }


private IEnumerator DoAttackB()
{
    FaceDirection();
    canAttackB = false;
    isAttacking = true;
    isParryableAttack = false;
    currentAnim = "AttackB";
    nextAttackTime = Time.time + attackDelay;

    rb.linearVelocity = Vector2.zero;

    // Decide if this AttackB is UNPARRYABLE (blue)
    bool isUnparryable = Random.value < unParryableChance;
    if (isUnparryable)
    {
        sr.color = Color.blue;
        Debug.Log("[CrabBoss] AttackB is UNPARRYABLE (blue)");
        yield return new WaitForSeconds(0.3f); // Show blue briefly
        sr.color = Color.white;
    }

    PlayAnimation("Crab_Attack_B");

    // Wait before parry window opens
    yield return new WaitForSeconds(0.4f);

    if (!isUnparryable)
    {
        isParryableAttack = true;
        Debug.Log("[CrabBoss] AttackB parry window STARTED");
    }

    // parry window is 0.25s
    yield return new WaitForSeconds(0.25f);

    isParryableAttack = false;
    Debug.Log("[CrabBoss] AttackB parry window ENDED");

    // Wait remainder of animation time (0.05s to complete 0.7s total)
    yield return new WaitForSeconds(0.05f);

    isAttacking = false;
    currentAnim = "";

    yield return new WaitForSeconds(4f); // Cooldown
    canAttackB = true;
}





    private IEnumerator DoAttackA()
    {
        FaceDirection();
        isAttacking = true;
        canAttackA = false;
        nextAttackTime = Time.time + attackDelay;

        PlayAnimation("Crab_Attack_A");

        // Lock during animation only (~0.5s)
        yield return new WaitForSeconds(0.6f);
        isAttacking = false;

        // Start red line logic on its own
        StartCoroutine(TrackAndDropRedLine());

        // Wait full cooldown before AttackA can happen again
        yield return new WaitForSeconds(7f);
        canAttackA = true;
    }

    private IEnumerator TrackAndDropRedLine()
    {
        // 1. Spawn red line
        GameObject redLine = Instantiate(redLinePrefab);
        redLine.transform.position = new Vector3(player.position.x, transform.position.y, 0f);
        redLine.transform.localScale = new Vector3(0.8f, 30f, 1f);

        // 2. Follow player's X for 3 seconds
        float followDuration = 3f;
        float timer = 0f;
        while (timer < followDuration)
        {
            if (player != null)
            {
                redLine.transform.position = new Vector3(player.position.x, transform.position.y, 0f);
            }
            timer += Time.deltaTime;
            yield return null;
        }

        float finalX = redLine.transform.position.x;
        Destroy(redLine);

        // 3. Wait 0.5 seconds
        yield return new WaitForSeconds(0.5f);

        // 4. Drop projectile from top
        if (redLineShotPrefab != null)
        {
            float topY = Camera.main.transform.position.y + Camera.main.orthographicSize + 2f;
            Vector3 spawnPos = new Vector3(finalX, topY, 0f);
            Instantiate(redLineShotPrefab, spawnPos, Quaternion.identity);
        }
    }


    private IEnumerator DoGroundShot()
    {
        FaceDirection();
        isAttacking = true;
        PlayAnimation("Crab_Attack_B");
        yield return new WaitForSeconds(0.5f);

        if (groundShotPrefab && groundPearlSpawnPoint)
        {
            GameObject pearl = Instantiate(groundShotPrefab, groundPearlSpawnPoint.position, Quaternion.identity);
            CrabProjectileBehavior proj = pearl.GetComponent<CrabProjectileBehavior>();
            if (proj != null)
                proj.Initialize(player.position);
        }

        isAttacking = false;
        yield return new WaitForSeconds(10f);
        canGroundShot = true;
    }

    private IEnumerator DoAttackC()
    {
        FaceDirection();
        isAttacking = true;
        PlayAnimation("Crab_Attack_C");
        yield return new WaitForSeconds(0.6f);

        if (arcProjectilePrefab && arcPearlSpawnPoint)
        {
            GameObject proj = Instantiate(arcProjectilePrefab, arcPearlSpawnPoint.position, Quaternion.identity);
            CrabArcProjectile arc = proj.GetComponent<CrabArcProjectile>();
            if (arc != null)
                arc.Initialize(player.position);
        }

        isAttacking = false;
        yield return new WaitForSeconds(14f);
        canAttackC = true;
    }
    public bool IsAttacking()
{
    return isAttacking;
}

  public void Parry()
{
    if (CanBeParried())
    {
        OnParried();
    }
    else
    {
        Debug.Log("[CrabBoss] Parry attempt failed - not in a valid state.");
    }
}
public bool CanBeParried()
{
    return isAttacking && isParryableAttack && currentAnim == "Crab_Attack_B";
}

public void OnParried()
{
    Debug.Log("[CrabBoss] Parried successfully!");
    isAttacking = false;
    isParryableAttack = false;

    // Play hit animation immediately
    PlayAnimation("Crab_Hit");

    // Show stun icon
    if (stunIcon != null)
        stunIcon.SetActive(true);

    // Start 1-second stun cycle
    StartCoroutine(StunCoroutine());
}



private IEnumerator StunCoroutine()
{
    // Wait a short moment so Crab_Hit plays
    yield return new WaitForSeconds(0.2f); // small delay before switching to idle

    PlayAnimation("Crab_Idle");

    // Wait for the full stun duration (still in idle)
    yield return new WaitForSeconds(0.8f); // total stun = 1s

    // Hide stun icon after stun duration
    if (stunIcon != null)
        stunIcon.SetActive(false);
}



}
