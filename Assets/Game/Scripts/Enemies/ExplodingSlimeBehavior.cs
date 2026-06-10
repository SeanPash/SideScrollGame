using UnityEngine;
using System.Collections;

// Exploding mini slime minion spawned by the Slime Boss. Chases the player,
// hops over obstacles when progress stalls, arms (red flashing) when close or
// when its fuse runs out, keeps closing in while armed, then detonates.
// Killing it through ExplodingSlimeHealth detonates it instantly.
public class ExplodingSlimeBehavior : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2.6f;
    // Armed slimes slow down a little so the player can still escape the blast.
    public float armedSpeedMultiplier = 0.6f;
    public float hopForce = 5f;
    public float hopCooldown = 1.2f;

    [Header("Explosion")]
    // Distance to the player that starts the arming flash.
    public float armRange = 1.2f;
    // Detonates this long after spawning even if it never reaches the player.
    public float fuseSeconds = 12f;
    public float armDuration = 0.6f;
    public int explosionDamage = 2;
    public float damageRadius = 2.2f;
    public GameObject explosionPrefab;
    public LayerMask playerLayer;

    private Rigidbody2D rb;
    private Transform player;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Animator animator;

    private bool isExploding = false;
    private bool isArmed = false;
    private float hopTimer;
    private float stuckTimer;
    private float age;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        animator = GetComponent<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Warrior");
        if (playerObj != null)
            player = playerObj.transform;
    }

    void Update()
    {
        if (isExploding || player == null) return;

        age += Time.deltaTime;
        float distance = Vector2.Distance(transform.position, player.position);

        // Arm when close to the player or when the fuse expires.
        if (!isArmed && (distance <= armRange || age >= fuseSeconds))
        {
            StartCoroutine(ArmAndExplode());
        }

        Chase(distance);
    }

    // Moves toward the player, hopping when blocked by terrain.
    private void Chase(float distance)
    {
        Vector2 dir = (player.position - transform.position).normalized;
        float speed = isArmed ? moveSpeed * armedSpeedMultiplier : moveSpeed;
        // A sleeping Rigidbody2D ignores velocity writes; wake it explicitly.
        rb.WakeUp();
        rb.linearVelocity = new Vector2(dir.x * speed, rb.linearVelocity.y);

        if (dir.x != 0)
            transform.localScale = new Vector3(
                Mathf.Sign(dir.x) * Mathf.Abs(transform.localScale.x),
                transform.localScale.y, transform.localScale.z);

        if (IsGrounded() && animator != null
            && !animator.GetCurrentAnimatorStateInfo(0).IsName("Enemy Attack 1"))
        {
            animator.Play("Enemy Run");
        }

        // Hop when blocked: it wants to move but makes no horizontal progress.
        hopTimer += Time.deltaTime;
        bool wantsToMove = distance > armRange * 0.5f;
        bool blocked = wantsToMove && Mathf.Abs(rb.linearVelocity.x) < 0.05f;
        stuckTimer = blocked ? stuckTimer + Time.deltaTime : 0f;

        if (stuckTimer > 0.35f && hopTimer >= hopCooldown && IsGrounded())
        {
            rb.linearVelocity = new Vector2(dir.x * speed, hopForce);
            hopTimer = 0f;
            stuckTimer = 0f;
        }
    }

    bool IsGrounded()
    {
        return Physics2D.Raycast(transform.position, Vector2.down, 0.3f, LayerMask.GetMask("Ground")).collider != null;
    }

    // Red warning flashes while still closing in, then detonate.
    private IEnumerator ArmAndExplode()
    {
        isArmed = true;
        if (animator != null) animator.Play("Enemy Attack 1");

        int flashes = 3;
        float step = armDuration / (flashes * 2);
        for (int i = 0; i < flashes; i++)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(step);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(step);
        }

        ExplodeImmediately();
    }

    // Detonates without the flashing windup. Used when the player kills the slime.
    public void ExplodeImmediately()
    {
        if (isExploding) return;
        isExploding = true;
        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
        Explode();
    }

    // Spawns the explosion effect, damages the Warrior if in range, and despawns.
    // The explosion prefab itself stays a pure visual effect.
    private void Explode()
    {
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, damageRadius);
        foreach (Collider2D hit in hits)
        {
            if (!hit.isTrigger && hit.CompareTag("Warrior"))
            {
                PlayerHealth health = hit.GetComponentInParent<PlayerHealth>();
                if (health != null) health.TakeDamage(explosionDamage);
                break;
            }
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, armRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}
