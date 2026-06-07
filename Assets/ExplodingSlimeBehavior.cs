using UnityEngine;
using System.Collections;

public class ExplodingSlimeBehavior : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float hopForce = 5f;
    public float checkRadius = 2f;
    public float hopCooldown = 2f;

    [Header("Explosion")]
    public float explosionRange = 1.5f;
    public GameObject explosionPrefab;
    public LayerMask playerLayer;

    private Rigidbody2D rb;
    private Transform player;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isExploding = false;
    private float hopTimer = 0f;
    private Animator animator;

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

        float distance = Vector2.Distance(transform.position, player.position);

        // Start exploding if in explosion range
        if (distance <= explosionRange)
        {
            StartCoroutine(ExplodeSequence());
            return;
        }

        // Move toward player
        Vector2 dir = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);

        // Face direction
        if (dir.x != 0)
            transform.localScale = new Vector3(Mathf.Sign(dir.x), 1, 1);

        // Play run animation if grounded
        if (IsGrounded() && !animator.GetCurrentAnimatorStateInfo(0).IsName("Enemy Attack 1"))
        {
            animator.Play("Enemy Run");
        }

        // Try to hop if player is slightly farther and grounded
        hopTimer += Time.deltaTime;
        if (distance <= checkRadius && distance > explosionRange && hopTimer >= hopCooldown && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, hopForce);
            hopTimer = 0f;
        }
    }

    bool IsGrounded()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.2f, LayerMask.GetMask("Ground"));
        return hit.collider != null;
    }

    IEnumerator ExplodeSequence()
    {
        isExploding = true;
        rb.linearVelocity = Vector2.zero;

        if (animator != null)
            animator.Play("Enemy Attack 1"); // Explosion animation

        // Flash twice
        for (int i = 0; i < 2; i++)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }

        // Explode
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRange);
    }
}
