    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;

public class CrabBossBehaviorPhase2 : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 2f;
    public float attackRange = 5f;

    public GameObject groundShotPrefab;
    public GameObject redLineShotVerticalPrefab;
    public GameObject redLineShotHorizontalPrefab;
    public GameObject arcProjectilePrefab;
    public Transform groundPearlSpawnPoint;
    public Transform arcPearlSpawnPoint;
    public GameObject redLinePrefab;


    private Rigidbody2D rb;
    private Animator animator;

    private bool canAttackB = true;
    private bool canAttackC = true;
    private bool canGroundShot = true;
    private bool isAttacking = false;
    private bool facingRight = true;
    private float flipThreshold = 0.1f;
    private float nextAttackTime = 0f;
    public float attackDelay = 3f;
    private string currentAnim = "";
    public bool isImmune = false;


    private SpriteRenderer sr;
    private bool isActive = false;  // <- New flag to prevent early logic

    private float nextAlternatingAttackTime = 0f;
    private bool nextIsAttackA = true; // flips after each attack


    [Header("AttackB Parry Toggle")]
    [Range(0f, 1f)] public float unParryableChance = 0.3f;
    [Header("Global Attack Timing")]
    public float globalAttackInterval = 10f;
    private float nextGlobalAttackTime;

    [Header("Red Line Spread Config")]
    public int verticalLineCount = 4;
    public int horizontalLineCount = 3;
    public float redLineDuration = 2f;
    public float delayBeforeShot = 0.5f;
    [Header("Red Line Prefabs")]
public GameObject redLineStaticPrefab;  // <-- doesn't follow
    public GameObject redLineTrackPrefab;   // <-- has RedLineFollow




    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
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

    // Replace your Update() method with this exact version:
    private void Update()
    {
        if (!isActive || player == null) return;

        float directionX = player.position.x - transform.position.x;
        float distance = Vector2.Distance(transform.position, player.position);

        FaceDirection();

        // Alternate between AttackA and GlobalAttack
        if (!isAttacking && Time.time >= nextAlternatingAttackTime)
        {
            if (nextIsAttackA)
            {
                StartCoroutine(DoAttackA());
            }
            else
            {
                nextAlternatingAttackTime = Time.time + 5.5f; // set early to prevent spam
                StartCoroutine(DelayedGlobalRedLineAttack(1.5f));
                nextIsAttackA = true;
            }
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
        bool isAttackAnim = animName == "Crab_Attack_A" ||
                            animName == "Crab_Attack_B" ||
                            animName == "Crab_Attack_C";

        if (currentAnim != animName || isAttackAnim)
        {
            Debug.Log("[CrabBoss] Forcing animation: " + animName);
            animator.Play(animName, -1, 0f);  // ← force restart from beginning
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
        if (player == null)
        {
            Debug.LogError("[CrabBossPhase2] player is null during ranged attack.");
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        Debug.Log("[CrabBossPhase2] Distance to player: " + distance);

        List<int> available = new List<int>();
        if (distance >= 6f && canAttackC) available.Add(2);
        if (canGroundShot) available.Add(1);

        if (available.Count == 0)
        {
            Debug.Log("[CrabBossPhase2] No ranged attacks available right now.");
            return;
        }

        int choice = available[Random.Range(0, available.Count)];
        Debug.Log("CrabPhase2 ranged attack roll: " + choice);

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
    private IEnumerator DelayedGlobalRedLineAttack(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(DoGlobalRedLineAttack());
    }



    private IEnumerator DoAttackB()
    {
        FaceDirection();
        canAttackB = false;
        isAttacking = true;
        nextAttackTime = Time.time + attackDelay;

        rb.linearVelocity = Vector2.zero;

        bool isUnparryable = Random.value < unParryableChance;
        if (isUnparryable && sr != null)
        {
            sr.color = Color.blue;
            yield return new WaitForSeconds(0.3f);
            sr.color = Color.white;
        }

        PlayAnimation("Crab_Attack_B");
        yield return new WaitForSeconds(.7f);

        isAttacking = false;
        yield return new WaitForSeconds(4f);
        canAttackB = true;
    }

    private IEnumerator DoAttackA()
    {
        nextAlternatingAttackTime = Time.time + 5.5f; // set cooldown immediately
        nextIsAttackA = false;

        FaceDirection();
        isAttacking = true;
        nextAttackTime = Time.time + attackDelay;


        // Fire vertical red line (async)
        PlayAnimation("Crab_Attack_A");
        yield return new WaitForSeconds(0.6f);
        StartCoroutine(TrackAndDropRedLine(true));
        yield return new WaitForSeconds(0.3f);

        // Fire horizontal red line (async)
        PlayAnimation("Crab_Attack_A");
        yield return new WaitForSeconds(0.6f);
        StartCoroutine(TrackAndDropRedLine(false));

        // Exit attack logic while red lines finish in background
        isAttacking = false;

        yield return new WaitForSeconds(7f); // Local cooldown
    }


    private IEnumerator TrackAndDropRedLine(bool isVertical)
    {
        if (player == null)
        {
            Debug.LogError("[CrabBossPhase2] Player is NULL in TrackAndDropRedLine!");
            yield break;
        }

GameObject redLine = Instantiate(redLineTrackPrefab);

        // Set initial scale and position
        if (isVertical)
        {

            redLine.transform.localScale = new Vector3(.8f, 30f, 1f);  // Tall line
            redLine.transform.position = new Vector3(player.position.x, transform.position.y, 0f);

        }
        else
        {
            redLine.transform.localScale = new Vector3(30f, .8f, 1f);  // Wide line
            redLine.transform.position = new Vector3(transform.position.x, player.position.y, 0f);
        }


        // Follow player for 3 seconds
        float followDuration = 3f;
        float timer = 0f;
        while (timer < followDuration)
        {
            if (player != null)
            {
                if (isVertical)
                {
                    // Vertical: track X-axis
                    redLine.transform.position = new Vector3(player.position.x, transform.position.y, 0f);

                }
                else
                {
                    // Horizontal: track Y-axis
                    redLine.transform.position = new Vector3(transform.position.x, player.position.y, 0f);
                }

            }

            timer += Time.deltaTime;
            yield return null;
        }

        Vector3 finalPos = redLine.transform.position;
        Destroy(redLine);

        // Delay before firing projectile
        yield return new WaitForSeconds(0.3f);

        // Drop redLineShot from final position
        Vector3 spawnPos;

        if (isVertical)
        {
            float topY = Camera.main.transform.position.y + Camera.main.orthographicSize + 2f;
            spawnPos = new Vector3(finalPos.x, topY, 0f);

            if (redLineShotVerticalPrefab != null)
            {
                Instantiate(redLineShotVerticalPrefab, spawnPos, Quaternion.identity);
            }
        }
        else
        {
            float sideX = finalPos.x + (facingRight ? -10f : 10f); // <- flip here
            spawnPos = new Vector3(sideX, finalPos.y, 0f);

            if (redLineShotHorizontalPrefab != null)
            {
                GameObject proj = Instantiate(redLineShotHorizontalPrefab, spawnPos, Quaternion.identity);

                HorizontalProjectile mover = proj.GetComponent<HorizontalProjectile>();
                if (mover != null)
                {
                    Vector2 dir = facingRight ? Vector2.right : Vector2.left;
                    mover.Initialize(dir);
                }
            }
        }
        // at the end of the coroutine:
        if (isVertical)
        {
            Debug.Log("[CrabBoss] Vertical red line completed.");
        }
        else
        {
            Debug.Log("[CrabBoss] Horizontal red line completed.");
        }

    }


    private IEnumerator DoGroundShot()
    {
        FaceDirection();
        isAttacking = true;

        // First shot
        PlayAnimation("Crab_Attack_B");
        yield return new WaitForSeconds(0.6f);
        FireGroundPearl();

        // Wait a bit and fire again
        yield return new WaitForSeconds(0.3f);
        PlayAnimation("Crab_Attack_B");
        yield return new WaitForSeconds(0.6f);
        FireGroundPearl();

        isAttacking = false;
        yield return new WaitForSeconds(10f);
        canGroundShot = true;
    }

    private void FireGroundPearl()
    {
        if (groundShotPrefab && groundPearlSpawnPoint)
        {
            GameObject pearl = Instantiate(groundShotPrefab, groundPearlSpawnPoint.position, Quaternion.identity);
            CrabProjectileBehavior proj = pearl.GetComponent<CrabProjectileBehavior>();
            if (proj != null)
                proj.Initialize(player.position);
        }
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
    public void TriggerPhaseStart()
    {
        StartCoroutine(DoPhaseStart());
    }
   private IEnumerator DoPhaseStart()
{

    ActivatePhase();
    isActive = false;
    isAttacking = true;
    isImmune = true;

    PlayAnimation("Crab_Idle");
    yield return new WaitForSeconds(4f);

    PlayAnimation("Crab_Ability");
    Debug.Log("[CrabBoss] Phase 2 starting with ability animation");

    yield return new WaitForSeconds(0.6f);

    isImmune = false;
    isActive = true;
    isAttacking = false;
}


    private IEnumerator DoGlobalRedLineAttack()
    {
        nextAlternatingAttackTime = Time.time + 5.5f;
        nextIsAttackA = true;

        List<GameObject> redLines = new List<GameObject>();

        float camHeight = Camera.main.orthographicSize * 2f;
        float camWidth = camHeight * Camera.main.aspect;
        float camX = Camera.main.transform.position.x;
        float camY = Camera.main.transform.position.y;

        // ---- 1. Compute evenly spaced X positions for 4 vertical lines ----
      float[] verticalX = new float[4];
verticalX[0] = camX - camWidth / 2f + 2f;     // Far left
verticalX[1] = camX - camWidth / 8f;          // move closer to center
verticalX[2] = camX + camWidth / 8f;          // move closer to center
verticalX[3] = camX + camWidth / 2f - 2f;     // Far right

        // ---- 2. Compute Y positions for 3 horizontal lines ----
    float[] horizontalY = new float[3];
horizontalY[0] = camY - camHeight / 2f + 1.8f;   // bottom line moved up from +1.2f to +1.8f
        horizontalY[1] = camY - 0.5f;    
horizontalY[2] = camY + camHeight / 2f - 2.2f;   // top line moved down from -1.5f to -2.2f

        // ---- 3. Spawn vertical red lines ----
       foreach (float x in verticalX)
{
    Vector3 pos = new Vector3(x, camY, 0f);
GameObject line = Instantiate(redLineStaticPrefab, Vector3.zero, Quaternion.identity);
    line.transform.SetParent(null); // <-- ensure it's not a child of anything
    line.transform.position = pos;  // <-- set world position AFTER instantiation
line.transform.localScale = new Vector3(0.8f, camHeight + 2f, 1f);
    redLines.Add(line);
}

        // ---- 4. Spawn horizontal red lines ----
        foreach (float y in horizontalY)
{
    Vector3 pos = new Vector3(camX, y, 0f);
GameObject line = Instantiate(redLineStaticPrefab, Vector3.zero, Quaternion.identity);
    line.transform.SetParent(null); // <-- ensure it's not a child of anything
    line.transform.position = pos;
    line.transform.localScale = new Vector3(30f, 0.8f, 1f);
    redLines.Add(line);
}


        // ---- 5. Wait while red lines are visible ----
        yield return new WaitForSeconds(redLineDuration);

        // ---- 6. Destroy red lines ----
        foreach (GameObject line in redLines)
        {
            if (line) Destroy(line);
        }

        // ---- 7. Delay before actual shots ----
        yield return new WaitForSeconds(delayBeforeShot);

        // ---- 8. Fire vertical projectiles (from top) ----
        foreach (float x in verticalX)
        {
            float spawnY = camY + camHeight / 2f + 2f;
            Vector3 spawnPos = new Vector3(x, spawnY, 0f);
            Instantiate(redLineShotVerticalPrefab, spawnPos, Quaternion.identity);
        }

        // ---- 9. Fire horizontal projectiles (from right side) ----
        foreach (float y in horizontalY)
        {
            float spawnX = camX + camWidth / 2f + 2f;
            Vector3 spawnPos = new Vector3(spawnX, y, 0f);
            GameObject proj = Instantiate(redLineShotHorizontalPrefab, spawnPos, Quaternion.identity);

            HorizontalProjectile mover = proj.GetComponent<HorizontalProjectile>();
            if (mover != null)
            {
                mover.Initialize(Vector2.left);
            }
        }
    }
}