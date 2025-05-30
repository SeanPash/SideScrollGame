using UnityEngine;
using System.Collections;

public class WarriorController : MonoBehaviour
{

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    public float jumpForce = 7f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;

    public float slideSpeed = .1f;
    public float slideDuration = 0.1f;
    private bool isSliding = false;
    public LayerMask whatIsGround;
    public float cooldownTime = 0.5f;
    private bool isOnCooldown = false;
    public float moveSpeed = 5f;
    bool wasGrounded = false;

    private bool isAttackCooldown = false;
    private bool isParryCooldown = false;

    public int maxJumps = 2;
    private int jumpCount = 0;
    public float dashDistance = 12f;
    private bool isFacingRight = true;
    private bool isDashing = false;
    private bool isParrying = false;

    private bool isAttacking = false;
    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        bool currentlyGrounded = IsGrounded();

        if (currentlyGrounded && !wasGrounded)
        {
            jumpCount = 0;
        }

        wasGrounded = currentlyGrounded;


        MovementMethod();
        CombatMethod();
        WallSlideMethod();
    }

    void MovementMethod()
    {
        if (isAttacking || isDashing || isParrying || isSliding) return;



        float moveInput = Input.GetAxisRaw("Horizontal");

        if (!isDashing && !isParrying)
        {
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        }

        //run animation
        if (IsGrounded() && !isDashing && !isParrying && !isSliding)

            if (moveInput != 0)
            {
                animator.Play("Run");
            }
            else
            {
                animator.Play("Idle");
            }

        //Initial jump animation
        if (Input.GetKeyDown(KeyCode.Space) && jumpCount < maxJumps)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            animator.Play("jump");
            jumpCount++;
        }

        //falling jump animation

        if (rb.linearVelocity.y < -0.1f && !IsGrounded())
        {
            animator.Play("JumptoFall");
        }



        //flip sprite

        if (moveInput > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (moveInput < 0 && isFacingRight)
        {
            Flip();
        }

        //dash

        if (Input.GetKeyDown(KeyCode.LeftShift) && !IsGrounded())
        {
            StartCoroutine(DoDash());
        }

        //slide
        if (Input.GetKeyDown(KeyCode.LeftControl) && IsGrounded() && !isSliding)
        {
            StartCoroutine(DoSlide());
        }

    }

    void CombatMethod()
    {
        if (isAttacking || isParrying || isSliding)
            return;
            //basic attack

            if (Input.GetMouseButtonDown(0) && !isDashing && !isParrying && !isAttackCooldown)
            {
                StartCoroutine(DoAttack());
                return;
            }

        //dash attack after dash

        if (isDashing && Input.GetMouseButtonDown(0))
        {
            StartCoroutine(DoDashAttack());
            return;
        }

        //Parry

        if (Input.GetMouseButtonDown(1) && !isParryCooldown)
        {
            StartCoroutine(DoParry());
            return;
        }
    }

    void WallSlideMethod()
    {
        bool touchingWall = IsTouchingWall();
        bool holdingDirection = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0;

        if (touchingWall && holdingDirection && !IsGrounded())
        {
            animator.Play("Wall-Slide");
            //slow fall
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, -2f, float.MaxValue));

        }
    }
    IEnumerator DoDash()
    {
        isDashing = true;
        animator.Play("Dash");
        rb.linearVelocity = new Vector2(transform.localScale.x * dashDistance, rb.linearVelocity.y);
        yield return new WaitForSeconds(.3f);
        isDashing = false;
        StartCoroutine(StartCooldown());

    }

    IEnumerator DoDashAttack()
    {
        if (isOnCooldown) yield break;
        isDashing = true;
        animator.Play("Dash-Attack");
        rb.linearVelocity = new Vector2(transform.localScale.x * dashDistance, rb.linearVelocity.y);
        yield return new WaitForSeconds(.4f);
        isDashing = false;
        StartCoroutine(StartCooldown());
    }

        IEnumerator DoAttack()
        {
                if (isAttacking) yield break;

        isAttacking = true;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            animator.Play("Attack");

        yield return new WaitForSeconds(.4f);

            isAttacking = false;

            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0)
                animator.Play("Run");
            else
                animator.Play("Idle");

            StartCoroutine(StartAttackCooldown(0.2f));
        }

    IEnumerator DoParry()
    {
    if (isParrying || isParryCooldown) yield break;

        isParrying = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        animator.Play("Parry");
        yield return new WaitForSeconds(.3f);


        //Stun regular enemies

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.2f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                hit.GetComponent<EnemyAI>()?.Stun(1.0f);
            }
            isParrying = false;
             if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0)
        animator.Play("Run");
    else
        animator.Play("Idle");
        StartCoroutine(StartParryCooldown(0.2f));
        }
    }
   IEnumerator DoSlide()
{
    isSliding = true;
    animator.Play("Slide");

    float originalSpeed = moveSpeed;
    float slideSpeed = 2.5f; // customize as needed
    float slideDuration = 0.4f;

    moveSpeed = slideSpeed; // temporarily increase speed
    rb.linearVelocity = new Vector2((isFacingRight ? 1 : -1) * slideSpeed, rb.linearVelocity.y);

    yield return new WaitForSeconds(slideDuration);

    moveSpeed = originalSpeed; // restore speed
    isSliding = false;

    if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0)
        animator.Play("Run");
    else
        animator.Play("Idle");

    StartCoroutine(StartCooldown());
}

    bool IsTouchingWall()
    {
        return false;
    }

    bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);

    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    IEnumerator StartCooldown()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(cooldownTime);
        isOnCooldown = false;
    }

    IEnumerator StartAttackCooldown(float time)
{
    isAttackCooldown = true;
    yield return new WaitForSeconds(time);
    isAttackCooldown = false;
}

IEnumerator StartParryCooldown(float time)
{
    isParryCooldown = true;
    yield return new WaitForSeconds(time);
    isParryCooldown = false;
}


    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }


}

