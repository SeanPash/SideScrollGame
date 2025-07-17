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
    public Transform wallCheck;
    public float wallCheckDistance = 0.3f;
    private int lastWallID = -1;
    private bool isGrounded;

    private int currentWallID = -1;
    private float hitboxDelay = .15f;
    public float chargedHitboxDelay = .2f;

    public LayerMask whatIsWall;
    private bool isDownwardAttacking = false;


    public float chargeTimeThreshold = 1.5f;
    private readonly float[] attackSpeeds = new float[] { 1.0f, 1f, 0.7f }; // Attack, Attack2, Attack3
    private readonly float[] attackDurations = new float[] { 0.8f, 0.4f, 0.6f }; // adjust to match speed

    public float[] hitboxDurations = { 0.3f, 0.25f, 0.4f };

    private float chargeTimer = 0f;

    public GameObject groundImpactPrefab;

    private bool isWallSliding = false;


    private Coroutine dashCoroutine;

    private bool isCharging = false;
    public float maxChargeTime = 2f;
    public float minChargeTime = 0.5f;
    private bool chargedAttackTriggered = false;
    public float slideDuration = 0.1f;
    private bool isSliding = false;
    public LayerMask whatIsGround;
    private bool wasCrouching = false;
    public float cooldownTime = 0.05f;
    private bool hasFinishedChargeAttack = false;
    private float flipSuppressTime = 0f;


    private bool isOnCooldown = false;
    public float moveSpeed = 5f;
    private Vector2 lastWallNormal = Vector2.zero;


    private int comboStep = 0;
    private bool hasAirDashed = false;
    private bool lockFlipDuringCharge = false;

    private int wallJumpCount = 0;


    private float comboTimer = 0f;
    public float maxComboDelay = 1f; // seconds allowed between hits

    public string[] comboAnimations = new string[] { "Attack", "Attack2", "Attack3" };

    private float wallHangTime = 2f;
    private float wallHangTimer = 0f;
    private float wallSlideSpeed = -0.5f;
    private CameraShake cameraShake;
    private bool wasGrounded = false;


    private float wallJumpFlipSuppressTimer = 0f;
    public int maxWallJumps = 1;


    public TrailRenderer trail;
    private Coroutine attackCoroutine;
    public ParryHitbox parryHitbox; 
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
    private bool fullEffectPlayed = false;
    private bool hasReleasedMouse = true;

    private bool isDashing = false;
    private bool isParrying = false;

    private bool isAttacking = false;
    private ParrySystem parrySystem;
    public GameObject attackHitbox;

    void Start()
    {
        ResetCombo();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        cameraShake = Camera.main.GetComponent<CameraShake>();
        parrySystem = GetComponent<ParrySystem>();
    }

    void Update()
    {
        bool grounded = IsGrounded();
bool holdingCrouch = Input.GetKey(KeyCode.S);

        if (grounded && holdingCrouch)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // freeze horizontal motion
            animator.Play("crouchidle"); // or "Croush" if you want an entry every time
            return; // prevents anything else from running this frame
        }

        bool currentlyGrounded = IsGrounded();

        if (currentlyGrounded && !wasGrounded)
        {
            wallJumpCount = 0;
            jumpCount = 0;
            if (!Input.GetKey(KeyCode.S))
            {
                if (!IsInAnyCrouch())
                {
                    animator.Play("Idle");
                }

            }
    
        }

        wasGrounded = currentlyGrounded;
       
            CombatMethod();
            MovementMethod();
            WallSlideMethod();
        
        // Detect landing impact from downward attack
            bool isGrounded = IsGrounded();

if (isDownwardAttacking && isGrounded && !wasGrounded)
{
    // Just landed while downward attacking
    //GroundParticle.Play();

    if (cameraShake != null)
        StartCoroutine(cameraShake.Shake(0.15f, 0.3f));

    isDownwardAttacking = false;
}

wasGrounded = isGrounded;


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

        if (ShouldLockMovement())
        {
            // Stop all movement while attacking, charging, or parrying
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        IsTouchingWall();
        float moveInput = Input.GetAxisRaw("Horizontal");
        if (flipSuppressTime <= 0f && wallJumpFlipSuppressTimer <= 0f && !isWallSliding && !lockFlipDuringCharge)
        {
            if (moveInput > 0 && !isFacingRight) Flip();
            else if (moveInput < 0 && isFacingRight) Flip();
        }
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing && !IsGrounded() && !isSliding && !isParrying && !isCharging && !isAttacking)
        {
            if (!PlayerStats.Instance.UseStamina(15f)) return;
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
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }
     bool holdingLeft = Input.GetKey(KeyCode.A);
    bool holdingRight = Input.GetKey(KeyCode.D);
        if (Input.GetKeyDown(KeyCode.LeftControl) && IsGrounded() && (!isSliding || (isAttacking && !isAttackCooldown)) && (holdingLeft || holdingRight))
        {
            if (!PlayerStats.Instance.UseStamina(15f)) return;
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
                if (!IsInAnyCrouch())
                {
                    animator.Play("Idle");
                }

            }
            if (isCharging)
            {
                isCharging = false;
                chargeTimer = 0f;
                sr.color = Color.white;
                chargedAttackTriggered = false;
                hasPlayedChargeFinish = false;
                if (!IsInAnyCrouch())
                {
                    animator.Play("Idle");
                }

            }
            comboTimer = 0f;
            StartCoroutine(DoSlide());
            return;

        }

        if (!isDashing && !isParrying && (!isAttacking || !IsGrounded()))

        {
            if (IsGrounded() || (!isWallSliding && Mathf.Abs(moveInput) > 0.1f))
            {
                rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
            }
        }

        //run animation
        if (IsGrounded() && !IsBusy() && !wasCrouching)
        {
            if (moveInput != 0)
                animator.Play("Run");
            else
if (!IsInAnyCrouch())
            {
                animator.Play("Idle");
            }

        }


        //crouch on ground animation
        bool grounded = IsGrounded();
bool pressingS = Input.GetKey(KeyCode.S);
bool releasedS = Input.GetKeyUp(KeyCode.S);

// 1. Hard enter crouch
if (grounded && pressingS)
{
    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("crouchidle") &&
        !animator.GetCurrentAnimatorStateInfo(0).IsName("Croush"))
    {
        animator.Play("Croush");
    }

    // Once Croush finishes, go to crouchidle
    if (animator.GetCurrentAnimatorStateInfo(0).IsName("Croush") &&
        animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
    {
        animator.Play("crouchidle");
    }

    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    return;
}

// 2. Force exit crouch when S is released
if (releasedS &&
    (animator.GetCurrentAnimatorStateInfo(0).IsName("crouchidle") ||
     animator.GetCurrentAnimatorStateInfo(0).IsName("Croush")))
{
    animator.Play("CrouchExit");
    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    return;
}

// 3. When CrouchExit finishes, return to Idle
if (animator.GetCurrentAnimatorStateInfo(0).IsName("CrouchExit") &&
    animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
{
    animator.Play("Idle");
}

        //downward attack if in air

        if (!IsGrounded() && Input.GetKey(KeyCode.S) && Input.GetKeyDown(KeyCode.Space) && !isAttacking)
        {
            StartCoroutine(DoDownwardAttack());
        }


        //Initial jump animation
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!PlayerStats.Instance.UseStamina(7f)) return;
            if ((isWallSliding || wallJumpGraceActive) && !IsGrounded())
            {
                float horizontalInput = Input.GetAxisRaw("Horizontal");
                bool pressingOpposite = (isFacingRight && horizontalInput < 0) || (!isFacingRight && horizontalInput > 0);
                bool isNewWall = currentWallID != lastWallID && currentWallID != -1;

                if (pressingOpposite && !jumpedFromThisWall && Time.time - lastWallJumpTime >= wallJumpCooldown && jumpCount < maxJumps)
                {
                    if (currentWallID != -1)
                    {
                        lastWallID = currentWallID;
                        jumpedFromThisWall = true;
                    }

                    Vector2 jumpDirection = isFacingRight ? Vector2.left + Vector2.up : Vector2.right + Vector2.up;
                    float decayMultiplier = Mathf.Clamp01(1f - (wallJumpCount * 0.25f));
                    rb.linearVelocity = jumpDirection.normalized * jumpForce * 1.2f * decayMultiplier;
                    jumpCount++;
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
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpCount++;
                hasAirDashed = false;
                StartCoroutine(HandleJumpAnimation());
                return;
            }

        }




        //falling jump animation
        /*
                if (!isWallSliding && rb.linearVelocity.y < -0.1f && !IsGrounded() && !isParrying && !isAttacking && !isCharging)
                {
                    animator.Play("JumptoFall");
                }
        */



        //flip sprite

        if (!ShouldLockMovement())
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
        if (Input.GetKeyDown(KeyCode.LeftControl)  && IsGrounded() && !isSliding && (holdingLeft || holdingRight))
        {
            if (!PlayerStats.Instance.UseStamina(15f)) return;
            if (isAttacking && attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
                isAttacking = false;
                if (!IsInAnyCrouch())
                {
                    animator.Play("Idle");
                }

            }

            if (isParrying && parryCoroutine != null)
            {
                StopCoroutine(parryCoroutine);
                parryCoroutine = null;
                isParrying = false;
                if (!IsInAnyCrouch())
                {
                    animator.Play("Idle");
                }

            }
            StartCoroutine(DoSlide());
        }
        // CROUCH UNLOCK AFTER EXIT
        if (IsInAnimation("CrouchExit") && IsInAnimationFinished("CrouchExit"))
        {
            animator.Play("Idle");
            rb.linearVelocity = Vector2.zero;
        }


    }

    void CombatMethod()
    {
        //regular attack
        if (isOnCooldown) return;
        if (Input.GetMouseButtonDown(0) && !isCharging && !isParrying && !isAttackCooldown)
        {
            if (!PlayerStats.Instance.UseStamina(10f)) return;
            StartCoroutine(WaitToTriggerCombo());
            return;
        }
        // Allow dash cancel first, then block other input
        if (isSliding)
        {
            if (Input.GetMouseButtonDown(0) && !isCharging && !isParrying && !isAttackCooldown)
            {
                if (!PlayerStats.Instance.UseStamina(10f)) return;
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

        if (Input.GetMouseButton(0))
        {
            if (!isAttacking && !isParrying && !isAttackCooldown && !isWaitingForCombo && IsGrounded())
            {
                if (chargedAttackTriggered || hasFinishedChargeAttack) return;
                chargeTimer += Time.deltaTime;

                if (!isCharging && !hasFinishedChargeAttack && chargeTimer >= 0.15f)
                {
                    isCharging = true;
                    lockFlipDuringCharge = true;
                    hasPlayedChargeFinish = false;
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

                    if (chargeTimer >= maxChargeTime && !hasPlayedChargeFinish && !chargedAttackTriggered && hasReleasedMouse)
                    {
                        if (!PlayerStats.Instance.UseStamina(20f)) return;
                        chargedAttackTriggered = true;
                        hasPlayedChargeFinish = true;
                        isCharging = false;

                        StartCoroutine(DoChargedAttack(chargeTimer));
                    }

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
                        if (!PlayerStats.Instance.UseStamina(20f)) return;
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
                            {
                                if (!PlayerStats.Instance.UseStamina(10f)) return;
                                attackCoroutine = StartCoroutine(DoComboAttack());
                            }
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
                lockFlipDuringCharge = false;
            }



        //Parry

        if (Input.GetMouseButtonDown(1) && !isParryCooldown)
        {
            parryCoroutine = StartCoroutine(DoParry());
            return;
        }
        if (!isGrounded && Input.GetKeyDown(KeyCode.Space) && Input.GetKey(KeyCode.S) && !isDownwardAttacking)
        {
            StartCoroutine(DoDownwardAttack());
        }

    }


    void WallSlideMethod()
    {
        bool grounded = IsGrounded();
        bool touchingWall = IsTouchingWall();
        float moveInput = Input.GetAxisRaw("Horizontal");
        bool movingTowardsWall = (isFacingRight && moveInput > 0) || (!isFacingRight && moveInput < 0);
        bool verticalSlowEnough = rb.linearVelocity.y <= 0.1f;

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
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // hang
                wallHangTimer -= Time.deltaTime;
            }
            else
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, wallSlideSpeed);
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

    IEnumerator DoDownwardAttack()
    {
        isAttacking = true;
        isDownwardAttacking = true;
        animator.Play("DownwardAttack");

        // Add force downward to simulate attack
        rb.linearVelocity = new Vector2(0, -6f); // adjust force as needed

        // Wait until grounded
        yield return new WaitUntil(() => IsGrounded());

        // 🔥 Play screen shake when hitting ground
        if (cameraShake != null)
        {
            StartCoroutine(cameraShake.Shake(0.15f, 0.3f));
        }

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
        if (!IsInAnyCrouch())
        {
            animator.Play("Idle");
        }

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
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            // Keep existing x-velocity, don't interfere with fall or jump
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y);
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
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
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

            StartCoroutine(StartAttackCooldown(0.05f));
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
if (!IsInAnyCrouch())
        {
            animator.Play("Idle");
        }

    }



    IEnumerator DoComboAttack()
    {

        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        string anim = comboAnimations[comboStep];

        animator.speed = attackSpeeds[comboStep];
        animator.Play(anim, 0);
        trail.emitting = true;

        if (comboStep < hitboxDurations.Length)
        {
                yield return new WaitForSeconds(hitboxDelay);
             int damage = PlayerStats.Instance.GetDamage(comboStep == 2);
            var dmg = attackHitbox.GetComponent<DealDamage>();
            dmg.ResetHit();
            dmg.overrideDamage = Mathf.RoundToInt(damage);
            attackHitbox.SetActive(true);

            float timer = 0f;
            while (timer < hitboxDurations[comboStep])
            {
                dmg.ManualCheckHits(); // do this every frame
                timer += Time.deltaTime;
                yield return null;
            }

            attackHitbox.SetActive(false);
            dmg.overrideDamage = -1; 

}
        else

        {
            Debug.LogWarning($"[WarriorController] No hitbox duration defined for comboStep {comboStep}");
        }

        yield return new WaitForSeconds(0.05f); // early cutoff if needed
        trail.emitting = false;

        float elapsed = 0f;
        float inputWindow = 0.2f;
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
if (!IsInAnyCrouch())
            {
                animator.Play("Idle");
            }

        }

        StartCoroutine(StartAttackCooldown(0.05f));
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

    // Don't use stamina yet — wait to see if parry succeeds
    if (!PlayerStats.Instance.HasEnoughStamina(10))
    {
        Debug.Log("Not enough stamina to parry.");
        yield break;
    }

    isParrying = true;

    if (IsGrounded())
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

    animator.Play("Parry");

    Debug.Log("Parry started — hitbox activated.");

    // Reset hitbox parry state
    parryHitbox.parrySucceeded = false;
    parryHitbox.gameObject.SetActive(true);

    yield return new WaitForSeconds(0.2f); // parry window

    parryHitbox.gameObject.SetActive(false);

    yield return new WaitForSeconds(0.2f); // lock time

    isParrying = false;

    if (!parryHitbox.parrySucceeded)
    {
        PlayerStats.Instance.UseStamina(10f);
        Debug.Log("Parry missed — stamina used.");
    }
    else
    {
        Debug.Log("Parry successful — no stamina used.");
    }

    if (!IsInAnyCrouch())
    {
        if (!IsGrounded())
            animator.Play("JumptoFall");
        else if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0)
            animator.Play("Run");
        else
            animator.Play("Idle");
    }

    StartCoroutine(StartParryCooldown(0.2f));
}



    IEnumerator DoChargedAttack(float chargeTime)
    {
        ResetCombo();
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        animator.Play("ChargeAttackFinish");

        float chargeRatio = Mathf.InverseLerp(minChargeTime, maxChargeTime, chargeTime);
        float damage = Mathf.Lerp(PlayerStats.Instance.baseDamage, 8f, chargeRatio);
            yield return new WaitForSeconds(chargedHitboxDelay);
var dmg = attackHitbox.GetComponent<DealDamage>();
dmg.ResetHit();
 dmg.overrideDamage = Mathf.RoundToInt(damage);
attackHitbox.SetActive(true);

float timer = 0f;
while (timer < hitboxDurations[comboStep])
{
    dmg.ManualCheckHits(); // do this every frame
    timer += Time.deltaTime;
    yield return null;
}

        attackHitbox.SetActive(false);
        dmg.overrideDamage = -1; 


        yield return new WaitForSeconds(0.1f); // Small delay before hit detection

        // Detect hit
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.2f);
        bool hitEnemy = false;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                hitEnemy = true;
                hit.GetComponent<IDamageable>()?.TakeDamage((int)damage);
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
if (!IsInAnyCrouch())
{
    animator.Play("Idle");
}


        StartCoroutine(StartAttackCooldown(0.05f));

        // Clean up effects
        sr.color = Color.white; // reset color
        midEffectPlayed = false;
        fullEffectPlayed = false;
        hasReleasedMouse = false;
        chargedAttackTriggered = false;
        hasPlayedChargeFinish = false;
        hasFinishedChargeAttack = true;
        lockFlipDuringCharge = false;


    }

    IEnumerator HandleJumpAnimation()
    {
        yield return new WaitForFixedUpdate(); // allow physics to apply jump

        if (!IsGrounded() && IsTouchingWall() && rb.linearVelocity.y < 0 && wallJumpFlipSuppressTimer <= 0f)
        {
            isWallSliding = true;
            animator.Play("Wall-Slide");
        }
        else if (!IsGrounded() && rb.linearVelocity.y > 0)
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
                if (!IsInAnyCrouch())
                {
                    animator.Play("Idle");
                }

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
        rb.linearVelocity = new Vector2((isFacingRight ? 1 : -1) * slideSpeed, rb.linearVelocity.y);

        yield return new WaitForSeconds(slideDuration);

        moveSpeed = originalSpeed;
        isSliding = false;
        if (!isAttacking && !isParrying && !isCharging)
        {
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0)
                animator.Play("Run");
            else
if (!IsInAnyCrouch())
            {
                animator.Play("Idle");
            }

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
if (!IsInAnyCrouch())
            {
                animator.Play("Idle");
            }

        }
        else
        {
            if (rb.linearVelocity.y < -0.1f)
                animator.Play("JumptoFall");
            else if (rb.linearVelocity.y > 0.1f)
                animator.Play("jump");
        }
    }



    bool ShouldLockMovement()
    {
        return isAttacking || isCharging || chargedAttackTriggered || isParrying;
    }

    bool CanPerformAction()
    {
        return !(isParrying && IsGrounded()) && !(isAttacking && IsGrounded());
    }
    bool IsInAnyCrouch()
    {
        return IsInAnimation("Croush") || IsInAnimation("crouchidle") || IsInAnimation("CrouchExit");
    }
    bool IsInAnimation(string name)
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsName(name);
    }
bool IsCrouching()
{
    var state = animator.GetCurrentAnimatorStateInfo(0);
    return state.IsName("Croush") || state.IsName("crouchidle") || state.IsName("CrouchExit");
}


    bool IsBusy()
    {
        return isSliding || isAttacking || isParrying || isCharging || chargedAttackTriggered || isWallSliding || isDownwardAttacking || isDashing;
    }



}

    


