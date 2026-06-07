using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SlimeBossBehavior : MonoBehaviour
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

    [Header("Ability Settings")]
    public float miniSlimeInterval = 15f;
    public float slamAttackInterval = 20f;
    public Transform[] miniSlimeSpawnPoints;

    public bool isAttacking = false;
    private bool canAttack = true;
    private bool isActive = false;
    public bool isDead = false;
    private float miniSlimeTimer = 0f;
    private float slamTimer = 0f;
    private float maxHealth;
    private float currentHealth;

    void Start()
    {
        maxHealth = 100f;
        currentHealth = maxHealth;
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

    public void ActivateBoss()
    {
        if (isActive) return;
        isActive = true;
        Debug.Log("Slime Boss fight started.");
    }

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

    IEnumerator SlamAttackSequence()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        for (int i = 0; i < 3; i++)
        {
            // Disappear
            spriteRenderer.enabled = false;

            Vector2 target = new Vector2(player.position.x, transform.position.y);
            GameObject redOutline = Instantiate(redOutlinePrefab, target, Quaternion.identity);

            float trackDuration = 1.1f;
            float flashDuration = 0.2f;

            while (trackDuration > 0f)
            {
                if (redOutline != null)
                    redOutline.transform.position = new Vector2(player.position.x, transform.position.y);
                trackDuration -= Time.deltaTime;
                yield return null;
            }

            if (redOutline != null)
            {
                redOutline.GetComponent<SpriteRenderer>().color = Color.red;
                Destroy(redOutline, flashDuration);
            }

            // Slam in
            transform.position = new Vector2(player.position.x, transform.position.y);
            spriteRenderer.enabled = true;
            Instantiate(greenSlimeZonePrefab, transform.position, Quaternion.identity);

            yield return new WaitForSeconds(0.5f);
        }

        isAttacking = false;
    }

    IEnumerator DisableLandingHitboxAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (landingHitbox != null)
            landingHitbox.SetActive(false);
    }

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

    bool IsGrounded()
    {
        return Physics2D.Raycast(transform.position, Vector2.down, 0.1f, groundLayer);
    }


    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            isDead = true;
            Debug.Log("Slime Boss defeated!");
        }
    }

    public float GetHealthPercent()
    {
        return (float)currentHealth / maxHealth;
    }

    public void EnterInactiveState()
    {
        isAttacking = false;
        canAttack = false;
        rb.linearVelocity = Vector2.zero;
        animator.Play("Enemy Idle");
    }

    public void Reactivate()
    {
        canAttack = true;
        isAttacking = false;
        ActivateBoss();
    }
    void Idle()
{
    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    animator.Play("Enemy Idle");
}


    public void EnableDoublePhase()
    {
        // Optional: enhance AI or change behavior in double boss phase
    }
}
