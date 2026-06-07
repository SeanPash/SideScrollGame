using UnityEngine;
using System.Collections;

public class SlimeBehavior : MonoBehaviour
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

    public bool isAttacking = false;
    public bool canAttack = true;
    public GameObject landingHitbox;

    public bool isDead = false;


    void Update()
    {
        if (isDead || isAttacking || player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= detectionRange)
        {
            Idle();

            if (canAttack)
            {
                if (distance <= 1f)
                {
                    StartCoroutine(DoAbilityAttack());
                }
                else
                {
                    StartCoroutine(DoDropAttack());
                }
            }
        }
        else
        {
            Idle();
        }
    }



    void FollowPlayer()
    {
        Vector2 dir = player.position.x > transform.position.x ? Vector2.right : Vector2.left;
        rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);
        transform.localScale = new Vector3(Mathf.Sign(dir.x), 1, 1);
        animator.Play("Enemy Run");
    }

    void Idle()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        animator.Play("Enemy Idle");
    }

    IEnumerator DoDropAttack()
{
    if (isDead) yield break;

    isAttacking = true;
    canAttack = false;
    rb.linearVelocity = Vector2.zero;
    Idle();

    // Optional mini pre-check (you can remove this if not needed)
    float dist = Vector2.Distance(transform.position, player.position);
    if (dist > detectionRange)
    {
        isAttacking = false;
        canAttack = true;
        yield break;
    }

    yield return new WaitForSeconds(0.2f); // Optional early windup

    // Lock target position at flash time
float dir = Mathf.Sign(player.position.x - transform.position.x); 
float distanceToPlayer = Mathf.Abs(player.position.x - transform.position.x);
float clampedDistance = Mathf.Min(distanceToPlayer, maxJumpDistance);
        float lockedTargetX = transform.position.x + dir * clampedDistance;
    yield return StartCoroutine(FlashBlue(0.4f));

    // Now attack with committed target
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

    IEnumerator DisableLandingHitboxAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (landingHitbox != null)
            landingHitbox.SetActive(false);
    }
    IEnumerator DoAbilityAttack()
    {
        isAttacking = true;
        canAttack = false;

        Debug.Log("Slime is doing ability attack");

        rb.linearVelocity = Vector2.zero;
        animator.Play("Enemy Ability");

        yield return new WaitForSeconds(1f);

        isAttacking = false;
        canAttack = true;
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
    IEnumerator FlashBlue(float duration)
    {
        if (spriteRenderer == null) yield break;

        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.blue;

        yield return new WaitForSeconds(duration);

        spriteRenderer.color = originalColor;
    }


}
