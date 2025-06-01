using UnityEngine;
using System.Collections;

public class WarriorController : MonoBehaviour
{

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    public float jumpForce = 7f;
    private float wallJumpCooldown = 0.25f; 
    private bool jumpedFromThisWall = false;

    private float lastWallJumpTime = -999f;

    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;

    public float slideSpeed = .1f;
    private bool hasPlayedChargeFinish = false;

    private bool isCrouching = false;
    public Transform wallCheck;
    public float wallCheckDistance = 0.3f;
    private int lastWallID = -1;
    private int currentWallID = -1;

    private float wallSlideGraceTime = 0.1f;
    private float wallSlideGraceTimer = 0f;

    public LayerMask whatIsWall;
    private bool isDownwardAttacking = false;


    public float chargeTimeThreshold = 1.5f;
    private readonly float[] attackSpeeds = new float[] { 1.0f, 2f, 0.7f }; // Attack, Attack2, Attack3
    private readonly float[] attackDurations = new float[] { 0.4f, 0.01f, 0.6f }; // adjust to match speed

    private bool attackInterrupted = false;


    private float chargeTimer = 0f;

    public GameObject groundImpactPrefab;

    private bool isWallSliding = false;


    private Coroutine dashCoroutine;

    private bool isCharging = false;
    private bool chargedAttackReady = false;

    public float maxChargeTime = 2f;

    private bool attackCompleted = false;

    public float minChargeTime = 0.5f;
    private bool chargedAttackTriggered = false;
    private float wallSlideCooldown = 0f;

    public float slideDuration = 0.1f;
    private bool isSliding = false;
    public LayerMask whatIsGround;
    private bool isCrouchIdlePlaying = false;
    private bool wasCrouching = false;
    public float cooldownTime = 0.5f;
    private bool hasFinishedChargeAttack = false;
    private float flipSuppressTime = 0f;


    private bool isOnCooldown = false;
    public float moveSpeed = 5f;
    private bool mouseHeldSincePressed = false;
    private Vector2 lastWallNormal = Vector2.zero;

    private int comboStep = 0;
    private bool hasAirDashed = false;
    private int wallJumpCount = 0;


    private float comboTimer = 0f;
    public float maxComboDelay = 1f; // seconds allowed between hits

    public string[] comboAnimations = new string[] { "Attack", "Attack2", "Attack3" };

    private float wallHangTime = 2f;
    private float wallHangTimer = 0f;
    private float wallSlideSpeed = -0.5f;
    private float wallJumpFlipSuppressTimer = 0f;
    public int maxWallJumps = 1;


    public TrailRenderer trail;
    bool wasGrounded = false;
    private Coroutine attackCoroutine;
    private Coroutine parryCoroutine;

    private bool isAttackCooldown = false;
    private bool isWaitingForCombo = false;

    private bool isParryCooldown = false;
    private bool wallJumpGraceActive = false;
    private float wallJumpGraceTimer = 0f;
    private readonly float wallJumpGraceDuration = 0.5f;

    public int maxJumps = 3;
    private int jumpCount = 0;
    public float dashDistance = 12f;
    private bool isFacingRight = true;
    private bool midEffectPlayed = false;
    private bool suppressNextFlip = false;

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
        if (Input.GetMouseButtonDown(0))
        {
            mouseHeldSincePressed = true;
        }
        if (Input.GetMouseButtonUp(0))
        {
            mouseHeldSincePressed = false;
        }

        bool currentlyGrounded = IsGrounded();

        if (currentlyGrounded && !wasGrounded)
        {
            wallJumpCount = 0;
            jumpCount = 0;
            animator.Play("Idle");
        }

        wasGrounded = currentlyGrounded;

        CombatMethod();
        MovementMethod();
        WallSlideMethod();

        if (!IsBusy())
            UpdateIdleRunFallAnimations();


        if (comboStep > 0 && !isSliding && !isAttacking)
        {
            comboTimer += Time.deltaTime;
            if (comboTimer > maxComboDelay)
            {
                comboStep = 0;
                comboTimer = 0f;

            }
        }
        // Reset jump lock if on a new wall
if (!IsGrounded() && currentWallID != -1 && currentWallID != lastWallID)
{
    jumpedFromThisWall = false;
}


        if (wallJumpFlipSuppressTimer > 0f)
            wallJumpFlipSuppressTimer -= Time.deltaTime;

        if (IsGrounded())
        {
            jumpedFromThisWall = false;
        }
else if (currentWallID != lastWallID && currentWallID != -1)
        {
            jumpedFromThisWall = false;
        }
        if (IsGrounded())
{
    jumpedFromThisWall = false;
    lastWallID = -1;
}   


    }

    void MovementMethod()
    {
        IsTouchingWall();
        float moveInput = Input.GetAxisRaw("Horizontal");
        if (flipSuppressTime <= 0f && wallJumpFlipSuppressTimer <= 0f && !isWallSliding)
{
    if (moveInput > 0 && !isFacingRight) Flip();
    else if (moveInput < 0 && isFacingRight) Flip();
}
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing && !isSliding && !isParrying && !isCharging && !isAttacking)
        {
            if (IsGrounded() || !hasAirDashed)
            {
                if (!IsGrounded())
                    hasAirDashed = true;

                if (dashCoroutine != null)
                    StopCoroutine(dashCoroutine);
                dashCoroutine = StartCoroutine(DoDash());
            }
        }


        if (isCharging || chargedAttackTriggered)
        {
            rb.velocity  = new Vector2(0f, rb.velocity .y);
            return;
        }
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
                chargedAttackTriggered = false;
                hasPlayedChargeFinish = false;
                animator.Play("Idle");
            }
            comboTimer = 0f;
            StartCoroutine(DoSlide());
            return;

        }

        if (!isDashing && !isParrying && (!isAttacking || !IsGrounded()))

        {
            if (IsGrounded() || (!isWallSliding && Mathf.Abs(moveInput) > 0.1f))
            {
                rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
            }
        }

        //run animation
        if (IsGrounded() && !IsBusy() && !wasCrouching)
        {
            if (moveInput != 0)
                animator.Play("Run");
            else
                animator.Play("Idle");
        }


        //crouch on ground animation

        bool pressingS = Input.GetKey(KeyCode.S);
        bool grounded = IsGrounded();

        if (grounded && pressingS && !isAttacking && !isSliding && !isCharging)
        {
            if (!wasCrouching)
            {
                wasCrouching = true;
                isCrouchIdlePlaying = false;
                animator.Play("Croush");

            }
            else
            {
                if (!isCrouchIdlePlaying && IsInAnimationFinished("Croush"))
                {
                    animator.Play("crouchidle");
                    isCrouchIdlePlaying = true;
                }
            }

            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }
        else if (wasCrouching)
        {
            wasCrouching = false;
            isCrouchIdlePlaying = false;
            animator.Play("CrouchExit");
            return;
        }

        //downward attack if in air

        if (!IsGrounded() && Input.GetKey(KeyCode.S) && Input.GetKeyDown(KeyCode.Space) && !isAttacking)
        {
            StartCoroutine(DoDownwardAttack());
        }


        //Initial jump animation
       if (Input.GetKeyDown(KeyCode.Space))
{
    if ((isWallSliding || wallJumpGraceActive) && !IsGrounded())
{
    float horizontalInput = Input.GetAxisRaw("Horizontal");
    bool pressingOpposite = (isFacingRight && horizontalInput < 0) || (!isFacingRight && horizontalInput > 0);
    bool isNewWall = currentWallID != lastWallID && currentWallID != -1;

    if (pressingOpposite && !jumpedFromThisWall && Time.time - lastWallJumpTime >= wallJumpCooldown && wallJumpCount < maxWallJumps)
    {
if (currentWallID != -1)
        {
            lastWallID = currentWallID;
            jumpedFromThisWall = true;
        }

        Vector2 jumpDirection = isFacingRight ? Vector2.left + Vector2.up : Vector2.right + Vector2.up;
        float decayMultiplier = Mathf.Clamp01(1f - (wallJumpCount * 0.25f)); 
        rb.velocity = jumpDirection.normalized * jumpForce * 1.2f * decayMultiplier;

        wallJumpCount++;
        lastWallJumpTime = Time.time;
        StartCoroutine(TemporarilyReduceGravity());
        hasAirDashed = false;
        Flip();
        wallJumpFlipSuppressTimer = 0.15f;
        StartCoroutine(HandleJumpAnimation());
        wallJumpGraceActive = false;
        return;
    }
}


    // Normal jump fallback
    if (jumpCount < maxJumps)
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        jumpCount++;
        hasAirDashed = false;
        StartCoroutine(HandleJumpAnimation());
        return;
    }
        }




            //falling jump animation
            /*
                    if (!isWallSliding && rb.velocity.y < -0.1f && !IsGrounded() && !isParrying && !isAttacking && !isCharging)
                    {
                        animator.Play("JumptoFall");
                    }
            */



            //flip sprite

            if (!isAttacking && !isParrying && !isCharging && !chargedAttackTriggered)
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

        if (Input.GetMouseButtonDown(0) && !isCharging && !isParrying && !isAttackCooldown)
        {
            StartCoroutine(WaitToTriggerCombo());
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

        if (Input.GetMouseButton(0) && !isAttacking && !isParrying && !isAttackCooldown && !isWaitingForCombo && IsGrounded())
        {
            if (chargedAttackTriggered || hasFinishedChargeAttack) return;
            chargeTimer += Time.deltaTime;

            if (!isCharging && !hasFinishedChargeAttack)
            {
                isCharging = true;
                hasPlayedChargeFinish = false;
                rb.velocity = Vector2.zero;
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

                if (chargeTimer >= maxChargeTime && !hasPlayedChargeFinish && !chargedAttackTriggered && hasReleasedMouse)
                {
                    chargedAttackTriggered = true;
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
            hasFinishedChargeAttack = false;

            if (isCharging)
            {
                if (chargeTimer >= minChargeTime && chargeTimer < maxChargeTime)
                {
                    chargedAttackTriggered = true;
                    hasPlayedChargeFinish = true;
                    isCharging = false;
                    StartCoroutine(DoChargedAttack(chargeTimer));
                }
                else if (chargeTimer < minChargeTime)
                {
                    if (!isAttacking)
                    {
                        if (IsGrounded())
                            attackCoroutine = StartCoroutine(DoComboAttack());
                        else
                        {
                            attackCoroutine = StartCoroutine(DoAttack(false));
                        }
                    }

                }

                isCharging = false;
                chargeTimer = 0f;
                midEffectPlayed = false;
                fullEffectPlayed = false;
                sr.color = Color.white;
            }
            else
            {
                chargeTimer = 0f;
            }

            // Reset this even if wasn't charging
            chargedAttackTriggered = false;
            hasPlayedChargeFinish = false;
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
    bool grounded = IsGrounded();
    bool touchingWall = IsTouchingWall();
    float moveInput = Input.GetAxisRaw("Horizontal");
    bool movingTowardsWall = (isFacingRight && moveInput > 0) || (!isFacingRight && moveInput < 0);
    bool verticalSlowEnough = rb.velocity.y <= 0.1f;

bool suppressWallSlide = Time.time - lastWallJumpTime < 0.2f;

if (!grounded && touchingWall && verticalSlowEnough && !isSliding && !isParrying && !isAttacking && !isCharging && movingTowardsWall && !suppressWallSlide)
    {
        if (!isWallSliding)
        {
            isWallSliding = true;
            wallHangTimer = wallHangTime;
            animator.Play("Wall-Slide");
        }

        wallJumpGraceActive = true;
        wallJumpGraceTimer = wallJumpGraceDuration;

        if (wallHangTimer > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0f); // hang
            wallHangTimer -= Time.deltaTime;
        }
        else
        {
            rb.velocity = new Vector2(rb.velocity.x, wallSlideSpeed);
        }
    }
    else
    {
        if (isWallSliding)
        {
            isWallSliding = false;
            wallHangTimer = 0f;
        }

        if (wallJumpGraceActive)
        {
            wallJumpGraceTimer -= Time.deltaTime;
            if (wallJumpGraceTimer <= 0f)
                wallJumpGraceActive = false;
        }
    }
}






    IEnumerator DoDash()
    {
        ResetCombo();
        isDashing = true;
        animator.Play("Dash");
        rb.velocity = new Vector2(transform.localScale.x * dashDistance, rb.velocity.y);
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

    IEnumerator DoDownwardAttack()
    {
        isAttacking = true;
        isDownwardAttacking = true;
        animator.Play("DownwardAttack");

        // Add force downward to simulate attack
        rb.velocity = new Vector2(0, -6f); // adjust force as needed

        // Wait until grounded
        yield return new WaitUntil(() => IsGrounded());

        // Spawn particle effect
        if (groundImpactPrefab != null)
        {
            Instantiate(groundImpactPrefab, groundCheck.position, Quaternion.identity);
        }

        // Knockback enemies in area
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.5f); // adjust radius
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Rigidbody2D enemyRb = hit.GetComponent<Rigidbody2D>();
                if (enemyRb != null)
                {
                    Vector2 knockDir = (hit.transform.position - transform.position).normalized;
                    enemyRb.AddForce(knockDir * 3f); // adjust strength
                }
            }
        }

        yield return new WaitForSeconds(0.3f); // short delay
        isAttacking = false;
        isDownwardAttacking = false;
        animator.Play("Idle");
    }


    IEnumerator WaitToTriggerCombo()
    {
        isWaitingForCombo = true;
        float waitTime = 0.15f;
        float elapsed = 0f;

        while (elapsed < waitTime)
        {
            if (Input.GetMouseButtonUp(0)) // Player tapped
            {
                if (!isAttacking && !isCharging)
                {
                    if (IsGrounded())
                        attackCoroutine = StartCoroutine(DoComboAttack());
                    else
                        attackCoroutine = StartCoroutine(DoAttack(false));
                }
                isWaitingForCombo = false;
                yield break;

            }

            if (Input.GetMouseButton(0) && chargeTimer > 0f) // Player is charging
            {
                isWaitingForCombo = false;
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // If they held too long without releasing, don't attack
        isWaitingForCombo = false;
    }



    IEnumerator DoAttack(bool allowCooldown)
    {
        if (isAttacking) yield break;

        isAttacking = true;
        isDashing = false;
        if (IsGrounded())
        {
            rb.velocity = Vector2.zero;
        }
        else
        {
            // Keep existing x-velocity, don't interfere with fall or jump
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y);
        }

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
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                jumpCount++;
                StartCoroutine(HandleJumpAnimation());
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



    IEnumerator DoComboAttack()
    {

        isAttacking = true;
        rb.velocity = Vector2.zero;

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


        if (nextClickDetected && comboStep < comboAnimations.Length - 1 && !isSliding)
        {
            comboStep++;
            comboTimer = 0f;
            attackCoroutine = StartCoroutine(DoComboAttack());
        }
        else
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

IEnumerator TemporarilyReduceGravity()
{
    float originalGravity = rb.gravityScale;
    rb.gravityScale = 1f; // or even lower
    yield return new WaitForSeconds(0.2f);
    rb.gravityScale = originalGravity;
}




    IEnumerator DoParry()
    {
        ResetCombo();

        if (isParrying || isParryCooldown) yield break;

        isParrying = true;

        // Don't freeze movement in air
        if (IsGrounded())
            rb.velocity = new Vector2(0, rb.velocity.y);

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
        rb.velocity = Vector2.zero;

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
        yield return new WaitForSeconds(0.05f);

        if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0)
            animator.Play("Run");
        else
            animator.Play("Idle");

        StartCoroutine(StartAttackCooldown(0.2f));

        // Clean up effects
        sr.color = Color.white; // reset color
        midEffectPlayed = false;
        fullEffectPlayed = false;
        hasReleasedMouse = false;
        chargedAttackTriggered = false;
        hasPlayedChargeFinish = false;
        hasFinishedChargeAttack = true;


    }

    IEnumerator HandleJumpAnimation()
    {
        yield return new WaitForFixedUpdate(); // allow physics to apply jump

        if (!IsGrounded() && IsTouchingWall() && rb.velocity.y < 0 && wallJumpFlipSuppressTimer <= 0f)
{
    isWallSliding = true;
    animator.Play("Wall-Slide");
}
        else if (!IsGrounded() && rb.velocity.y > 0)
        {
            isWallSliding = false;
            animator.Play("jump");
        }

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


    bool IsInAnimationFinished(string animName)
    {
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        return state.IsName(animName) && state.normalizedTime >= 1f;
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
        rb.velocity = new Vector2((isFacingRight ? 1 : -1) * slideSpeed, rb.velocity.y);

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
    Collider2D wall = Physics2D.OverlapCircle(wallCheck.position, 0.08f, whatIsWall);
    currentWallID = wall ? wall.gameObject.GetInstanceID() : -1;
    return wall != null;
    }

    bool IsGrounded()
{
    Vector2 boxSize = new Vector2(0.45f, 0.01f); 
    float castDistance = 0.01f;                 

    RaycastHit2D hit = Physics2D.BoxCast(groundCheck.position, boxSize, 0f, Vector2.down, castDistance, whatIsGround);
    return hit.collider != null;
}

   void Flip()
{
    if (flipSuppressTime > 0f) return;

    if (isWallSliding) return;

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

    void UpdateIdleRunFallAnimations()
{
    if (IsBusy()) return;

    if (isWallSliding)
    {
        animator.Play("Wall-Slide");
        return;
    }

    if (IsGrounded())
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        if (moveInput != 0)
            animator.Play("Run");
        else
            animator.Play("Idle");
    }
    else
    {
        if (rb.velocity.y < -0.1f)
            animator.Play("JumptoFall");
        else if (rb.velocity.y > 0.1f)
            animator.Play("jump");
    }
}



    bool CanPerformAction()
    {
        return !(isParrying && IsGrounded()) && !(isAttacking && IsGrounded());
    }
    bool IsBusy()
    {
        return isSliding || isAttacking || isParrying || isCharging || chargedAttackTriggered || isWallSliding || isDownwardAttacking || isDashing;
    }


}
    


