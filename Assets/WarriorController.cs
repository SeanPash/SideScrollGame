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
    private bool hasPlayedChargeFinish = false;


    public float chargeTimeThreshold = 1.5f;
    private readonly float[] attackSpeeds = new float[] { 1.0f, 2f, 0.7f }; // Attack, Attack2, Attack3
    private readonly float[] attackDurations = new float[] { 0.4f, 0.01f, 0.6f }; // adjust to match speed

    private bool attackInterrupted = false;


    private float chargeTimer = 0f;

    private Coroutine dashCoroutine;

    private bool isCharging = false;
    private bool chargedAttackReady = false;

    public float maxChargeTime = 2f;

    private bool attackCompleted = false;

    public float minChargeTime = 0.5f;
    private bool chargedAttackTriggered = false;
    public float slideDuration = 0.1f;
    private bool isSliding = false;
    public LayerMask whatIsGround;
    public float cooldownTime = 0.5f;
    private bool isOnCooldown = false;
    public float moveSpeed = 5f;
    private int comboStep = 0;
    private float comboTimer = 0f;
    public float maxComboDelay = 1f; // seconds allowed between hits

    public string[] comboAnimations = new string[] { "Attack", "Attack2", "Attack3" };
    public TrailRenderer trail;
    bool wasGrounded = false;
    private Coroutine attackCoroutine;
    private Coroutine parryCoroutine;

    private bool isAttackCooldown = false;
    private bool isParryCooldown = false;

    public int maxJumps = 2;
    private int jumpCount = 0;
    public float dashDistance = 12f;
    private bool isFacingRight = true;
    private bool midEffectPlayed = false;
    private bool fullEffectPlayed = false;
    private bool hasReleasedMouse = true;

    private bool isDashing = false;
    private bool isParrying = false;

    private bool isAttacking = false;
    void Start()
    {
        ResetCombo();
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

        CombatMethod();
        MovementMethod();
        WallSlideMethod();

        if (comboStep > 0 && !isSliding && !isAttacking)
        {
            comboTimer += Time.deltaTime;
            if (comboTimer > maxComboDelay)
            {
                    comboStep = 0;
                    comboTimer = 0f;
                
            }
        }
    }

    void MovementMethod()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
if (Input.GetKeyDown(KeyCode.LeftControl) && IsGrounded() && (!isSliding || (isAttacking && !isAttackCooldown)))
    {

        if (isAttacking && attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
                isAttacking = false;
            }

        if (isParrying && parryCoroutine != null)
        {
            StopCoroutine(parryCoroutine);
            parryCoroutine = null;
            isParrying = false;
            animator.Play("Idle");
        }
         if (isCharging)
    {
        isCharging = false;
        chargeTimer = 0f;
        sr.color = Color.white;
        animator.Play("Idle");
    }
    comboTimer = 0f;
    StartCoroutine(DoSlide());
    return;
       
    }

    if (!isSliding && ((isAttacking && IsGrounded()) || isDashing || (isParrying && IsGrounded()) || isCharging))
    {
        if (!isAttacking && !isParrying)
        {
            if (moveInput > 0 && !isFacingRight) Flip();
            else if (moveInput < 0 && isFacingRight) Flip();
        }
        return;
    }





        if (!isDashing && !isParrying && !isAttacking)

        {
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        }

        //run animation
        if (IsGrounded() && !isDashing && !isParrying && !isSliding && !isAttacking && !isCharging)

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

        if (rb.linearVelocity.y < -0.1f && !IsGrounded() && !isParrying && !isAttacking && !isCharging)
        {
            animator.Play("JumptoFall");
        }



        //flip sprite

        if (!isAttacking && !isParrying)
        {
            if (moveInput > 0 && !isFacingRight)
            {
                Flip();
            }
            else if (moveInput < 0 && isFacingRight)
            {
                Flip();
            }
        }
        //dash

        if (Input.GetKeyDown(KeyCode.LeftShift) && !IsGrounded())
        {
            if (dashCoroutine != null)
                StopCoroutine(dashCoroutine);
            dashCoroutine = StartCoroutine(DoDash());

        }

        //slide
        if (Input.GetKeyDown(KeyCode.LeftControl) && IsGrounded() && !isSliding)
        {
            if (isAttacking && attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
                isAttacking = false;
                animator.Play("Idle");
            }

            if (isParrying && parryCoroutine != null)
            {
                StopCoroutine(parryCoroutine);
                parryCoroutine = null;
                isParrying = false;
                animator.Play("Idle");
            }
            StartCoroutine(DoSlide());
        }
    }
    void CombatMethod()
    {
         //regular attack

        if (Input.GetMouseButtonDown(0) && !isAttacking && !isCharging && !isParrying && !isAttackCooldown && IsGrounded())
        {
            attackCoroutine = StartCoroutine(DoComboAttack());
            return;
        }
        // Allow dash cancel first, then block other input
        if (isSliding)
        {
            if (Input.GetMouseButtonDown(0) && !isCharging && !isParrying && !isAttackCooldown)
            {
                StopCoroutine("DoSlide");
                isSliding = false;
                comboTimer = 0f;
                comboStep = 0;
                attackCoroutine = StartCoroutine(DoComboAttack());
                return;
            }
        }

        // Block further input if doing something grounded (AFTER dash cancel check)
        if (!CanPerformAction())
            return;

        if ((isParrying && IsGrounded()) || (isAttacking && IsGrounded()))
            return;


        //charge attack

        if (Input.GetMouseButton(0) && !isAttacking && !isCharging && !isParrying && !isAttackCooldown && IsGrounded())
        {
            chargeTimer += Time.deltaTime;

            if (chargeTimer >= minChargeTime && !isCharging)
            {
                isCharging = true;
                chargeTimer = 0f;
                hasPlayedChargeFinish = false;
                hasReleasedMouse = false;
                rb.linearVelocity = Vector2.zero;
                animator.Play("ChargeAttackStart");
            }

            // While holding, accumulate charge
            if (isCharging)
            {
                // Mid charge color change
                if (!midEffectPlayed && chargeTimer >= (maxChargeTime * 0.5f))
                {
                    sr.color = new Color(1f, 1f, 0.5f); // yellow
                    midEffectPlayed = true;
                }

                // Full charge color change
                if (!fullEffectPlayed && chargeTimer >= maxChargeTime)
                {
                    sr.color = Color.red;
                    fullEffectPlayed = true;
                }

                if (chargeTimer >= maxChargeTime && !hasPlayedChargeFinish)
                {
                    chargeTimer = maxChargeTime;
                    hasPlayedChargeFinish = true;
                    isCharging = false;

                    StartCoroutine(DoChargedAttack(chargeTimer));
                }
            }
        }
        // On release
        if (Input.GetMouseButtonUp(0))
        {
            hasReleasedMouse = true;

            if (isCharging)
            {

                if (chargeTimer >= minChargeTime && chargeTimer < maxChargeTime)
                {
                    StartCoroutine(DoChargedAttack(chargeTimer));
                }
                isCharging = false;
                chargeTimer = 0f;
                hasPlayedChargeFinish = false;
                midEffectPlayed = false;
                fullEffectPlayed = false;
                sr.color = Color.white;
            } else
            {
                chargeTimer = 0f;
            }
        }

        //Parry

        if (Input.GetMouseButtonDown(1) && !isParryCooldown)
        {
            parryCoroutine = StartCoroutine(DoParry());
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
        ResetCombo();
        isDashing = true;
        animator.Play("Dash");
        rb.linearVelocity = new Vector2(transform.localScale.x * dashDistance, rb.linearVelocity.y);

        float dashTime = 0.3f;
        float timer = 0f;

        while (timer < dashTime)
        {
            if (!isDashing) yield break; // cancel dash early if interrupted
            timer += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
        StartCoroutine(StartCooldown());
    }


    IEnumerator DoAttack(bool allowCooldown)
    {
        if (isAttacking) yield break;

        isAttacking = true;
        isDashing = false;
        rb.linearVelocity = Vector2.zero;

        bool isAir = !IsGrounded();
        string animName = isAir ? "AirAttack" : "Attack";
        float attackDuration = isAir ? 0.4f : 0.5f;

        animator.Play(animName);
        yield return null;

        float elapsed = 0f;
        bool interrupted = false;

        float startTime = Time.time;

        while (elapsed < attackDuration)
        {
            // Check for jump interrupt
            if (Input.GetKeyDown(KeyCode.Space) && jumpCount < maxJumps)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                animator.Play("jump");
                jumpCount++;
                interrupted = true;
                break;
            }

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (!(state.IsName("Attack") || state.IsName("AirAttack")) && (Time.time - startTime < attackDuration * 0.5f))
            {
                interrupted = true;
                break;
            }

            // Add more interrupt conditions here if needed (e.g., dash or parry)

            elapsed += Time.deltaTime;
            yield return null;
        }

        isAttacking = false;

        // Only apply cooldown if not interrupted
        if (!interrupted && allowCooldown)
        {

            StartCoroutine(StartAttackCooldown(0.2f));
        }
        else
        {

            isAttackCooldown = false;
        }
        // Return to appropriate animation
        if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0 && IsGrounded())
            animator.Play("Run");
        else if (!IsGrounded())
            animator.Play("JumptoFall");
        else
            animator.Play("Idle");
    }

    IEnumerator DoComboAttack() {

    isAttacking = true;
    rb.linearVelocity = Vector2.zero;

    string anim = comboAnimations[comboStep];

    animator.speed = attackSpeeds[comboStep];
    animator.Play(anim, 0);
    trail.emitting = true;

    yield return new WaitForSeconds(0.2f); // early cutoff if needed
    trail.emitting = false;

    float elapsed = 0f;
    float inputWindow = 0.4f;
    bool nextClickDetected = false;

    while (elapsed < inputWindow)
    {
        if (Input.GetMouseButtonDown(0))
        {
            nextClickDetected = true;
        }

        elapsed += Time.deltaTime;
        yield return null;
    }

        // ✔ Either continue the combo OR stay on current step
if (nextClickDetected && comboStep < comboAnimations.Length - 1 && !isSliding)
        {
            comboStep++;
            comboTimer = 0f;
            attackCoroutine = StartCoroutine(DoComboAttack());
        }else
        {
            comboStep = 0;
            comboTimer = 0f;
            isAttacking = false;

            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0 && IsGrounded())
                animator.Play("Run");
            else
                animator.Play("Idle");
        }

        StartCoroutine(StartAttackCooldown(0.1f));
    animator.speed = 1f;
}




    IEnumerator DoParry()
    {
        ResetCombo();

        if (isParrying || isParryCooldown) yield break;

        isParrying = true;

        // Don't freeze movement in air
        if (IsGrounded())
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        animator.Play("Parry");

        yield return new WaitForSeconds(0.3f);

        // Attempt to stun enemies
        bool hitEnemy = false;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.2f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                hit.GetComponent<EnemyAI>()?.Stun(1.0f);
                hitEnemy = true;
            }
        }

        isParrying = false;

        // Fallback animation if no enemy hit
        if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0)
            animator.Play("Run");
        else if (!IsGrounded())
            animator.Play("JumptoFall");
        else
            animator.Play("Idle");

        StartCoroutine(StartParryCooldown(0.2f));
    }

    IEnumerator DoChargedAttack(float chargeTime)
    {
        ResetCombo();
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        animator.Play("ChargeAttackFinish");

        float chargeRatio = Mathf.InverseLerp(minChargeTime, maxChargeTime, chargeTime);
        float damage = Mathf.Lerp(10f, 40f, chargeRatio);

        yield return new WaitForSeconds(0.1f); // Small delay before hit detection

        // Detect hit
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.2f);
        bool hitEnemy = false;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                hitEnemy = true;
                hit.GetComponent<EnemyAI>()?.TakeDamage(damage);
            }
        }

        // If hit, apply hitstop
        if (hitEnemy)
        {
            yield return StartCoroutine(Hitstop(0.1f));
        }

        yield return new WaitForSeconds(0.5f); // Finish animation time

        isAttacking = false;

        if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0)
            animator.Play("Run");
        else
            animator.Play("Idle");

        StartCoroutine(StartAttackCooldown(0.2f));

        // Clean up effects
        sr.color = Color.white; // reset color
        midEffectPlayed = false;
        fullEffectPlayed = false;
        midEffectPlayed = false;
        fullEffectPlayed = false;
    }

    IEnumerator Hitstop(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    IEnumerator ReturnToIdleAfterAnimation(float duration)
    {
        yield return new WaitForSeconds(duration);

        // Only switch back if we’re not attacking or doing something else
        if (!isAttacking && !isDashing && !isSliding && !isParrying && !isCharging)
        {
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0)
            {
                animator.Play("Run");
            }
            else if (!IsGrounded())
            {
                animator.Play("JumptoFall");
            }
            else
            {
                animator.Play("Idle");
            }
        }
    }



    IEnumerator DoSlide()
    {
        ResetCombo();
        isSliding = true;
        animator.Play("Slide");

        float originalSpeed = moveSpeed;
        float slideSpeed = 2.5f;
        float slideDuration = 0.4f;

        moveSpeed = slideSpeed;
        rb.linearVelocity = new Vector2((isFacingRight ? 1 : -1) * slideSpeed, rb.linearVelocity.y);

        yield return new WaitForSeconds(slideDuration);

        moveSpeed = originalSpeed;
        isSliding = false;
        if (!isAttacking && !isParrying && !isCharging)
        {
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0)
                animator.Play("Run");
            else
                animator.Play("Idle");
        }
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

    void ResetCombo()
{
    comboStep = 0;
    comboTimer = 0f;
    isAttacking = false;
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

    bool IsInAnimation(string name)
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsName(name);
    }



    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }

    bool CanPerformAction()
    {
        return !(isParrying && IsGrounded()) && !(isAttacking && IsGrounded());
    }

}
    


