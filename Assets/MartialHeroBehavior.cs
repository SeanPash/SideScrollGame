using UnityEngine;
using System.Collections;

public class MartialHeroBehavior : MonoBehaviour, IAttackState
{
    [Header("References")]
    public Transform player;
    public Rigidbody2D rb;
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public GameObject parryHitboxPrefab;

    [Header("Settings")]
    public float moveSpeed = 3f;
    public float attackRange = 2f;
    public float attackDelay = 1.5f;
    public float chargeCooldown = 10f;
    public float teleportDistance = 1.5f;

    private bool isAttacking = false;
    public bool IsAttackingNow { get; private set; } = false;
    private int parryCountThisAttack = 0;
    private MartialHeroStun stunHandler;
    private string currentAttackPhase = "";
    private bool canChargeAttack = true;
    private bool appearBehindNext = true;


    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        stunHandler = GetComponent<MartialHeroStun>();
    }

    private void Update()
    {
        if (GetComponent<MartialHeroHealth>()?.isDead == true)
            return;
        if (stunHandler.IsStunned()) return;
        FacePlayer();

        float dist = Vector2.Distance(transform.position, player.position);

        if (!isAttacking)
        {
            if (dist > attackRange && canChargeAttack)
            {
                StartCoroutine(DoChargeAttack());
            }
            else if (dist <= attackRange)
            {
                StartCoroutine(DoRandomAttack());
            }
            else
            {
                MoveTowardPlayer();
            }
        }
    }

    private void MoveTowardPlayer()
    {
        Vector2 dir = (player.position.x > transform.position.x) ? Vector2.right : Vector2.left;
        rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);
        animator.Play("Run");
    }

   private IEnumerator DoChargeAttack()
{
    isAttacking = true;
    canChargeAttack = false;
    rb.linearVelocity = Vector2.zero;

    if (!GetComponent<MartialHeroHealth>().isDead)
    animator.Play("ChargeAttack");

    spriteRenderer.color = Color.white;

    yield return new WaitForSeconds(0.6f);

    spriteRenderer.color = Color.red;

    yield return new WaitForSeconds(0.2f); 


    spriteRenderer.enabled = false;
    float offset = appearBehindNext ? -teleportDistance : teleportDistance;
    appearBehindNext = !appearBehindNext;
    transform.position = player.position + new Vector3(offset, 0f, 0f);
    spriteRenderer.enabled = true;

    spriteRenderer.color = Color.white;
    FacePlayer();

    currentAttackPhase = "ChargeAttack";
    IsAttackingNow = true;

    yield return new WaitForSeconds(0.35f); 

    EnableParryableWindow(0.25f);

    yield return new WaitForSeconds(0.35f); 
    IsAttackingNow = false;

    if (parryCountThisAttack >= 1)
    {
        stunHandler.Stun(.7f);
    }

    animator.Play("Idle");
    yield return new WaitForSeconds(attackDelay); 

    isAttacking = false;
    StartCoroutine(ResetChargeCooldown());
}




    private IEnumerator ResetChargeCooldown()
    {
        yield return new WaitForSeconds(chargeCooldown);
        canChargeAttack = true;
    }

    private IEnumerator DoRandomAttack()
    {
        FacePlayer();
        isAttacking = true;
        parryCountThisAttack = 0;

        int rand = Random.Range(1, 4);
        if (rand == 1)
        {
            yield return StartCoroutine(DoAttack1a());
            yield return new WaitForSeconds(0.4f);
            yield return StartCoroutine(DoAttack1b());
        }
        else if (rand == 2)
        {
            yield return StartCoroutine(DoAttack2());
        }
        else
        {
            yield return StartCoroutine(DoAttack3());
        }

        yield return new WaitForSeconds(attackDelay);
        isAttacking = false;
    }

    private IEnumerator DoAttack1a()
    {
        currentAttackPhase = "Attack1a";
        if (!GetComponent<MartialHeroHealth>().isDead)
        animator.Play("Attack1");
        yield return new WaitForSeconds(0.2f);
        IsAttackingNow = true;
        EnableParryableWindow(0.2f);
        yield return new WaitForSeconds(0.5f);
        IsAttackingNow = false;
    }

   private IEnumerator DoAttack1b()
{
    currentAttackPhase = "Attack1b";
    if (!GetComponent<MartialHeroHealth>().isDead)
    animator.Play("Attack1Part2");
    yield return new WaitForSeconds(0.15f);
    IsAttackingNow = true;
    EnableParryableWindow(0.2f);
    yield return new WaitForSeconds(0.5f);
    IsAttackingNow = false;

    if (parryCountThisAttack >= 2)
    {
        stunHandler.Stun(.7f);
    }

    animator.Play("Idle"); 
}


  private IEnumerator DoAttack2()
{
    currentAttackPhase = "Attack2";
    if (!GetComponent<MartialHeroHealth>().isDead)
    animator.Play("Attack2");

    yield return new WaitForSeconds(0.55f);

    IsAttackingNow = true;
    EnableParryableWindow(0.3f);  
    yield return new WaitForSeconds(0.4f);
    IsAttackingNow = false;

    if (parryCountThisAttack >= 1)
    {
        stunHandler.Stun(.7f);
    }

    yield return new WaitForSeconds(0.4f); 
    animator.Play("Idle");
}


 private IEnumerator DoAttack3()
{
    currentAttackPhase = "Attack3";
    if (!GetComponent<MartialHeroHealth>().isDead)
    animator.Play("Attack3");
    spriteRenderer.color = Color.blue;
    yield return new WaitForSeconds(0.5f);
    spriteRenderer.color = Color.white;

    animator.Play("Idle"); 
}


    private void EnableParryableWindow(float duration)
    {
        if (parryHitboxPrefab == null) return;
            Debug.Log($"[MartialHero] Parry window opened during {currentAttackPhase} for {duration} seconds");
        GameObject hitbox = Instantiate(parryHitboxPrefab, transform.position, Quaternion.identity);
        Destroy(hitbox, duration);
    }

   public void RegisterParry()
{
    if (!IsAttackingNow)
    {
        Debug.Log("[MartialHero] Ignored parry — not attacking.");
        return;
    }

    parryCountThisAttack++;
    Debug.Log($"[MartialHero] Registered parry during {currentAttackPhase}, count = {parryCountThisAttack}");

    if ((currentAttackPhase == "Attack1a" || currentAttackPhase == "Attack1b") && parryCountThisAttack >= 2)
    {
        stunHandler.Stun(.7f);
    }
    else if (currentAttackPhase == "Attack2" && parryCountThisAttack >= 1)
    {
        stunHandler.Stun(.7f);
    }
    else if (currentAttackPhase == "ChargeAttack" && parryCountThisAttack >= 1)
    {
        stunHandler.Stun(.7f);
    }
}

    private void FacePlayer()
    {
        if (player == null) return;

        Vector3 scale = transform.localScale;
        scale.x = (player.position.x > transform.position.x) ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }
public bool IsCurrentlyAttacking()
{
    return animator.GetCurrentAnimatorStateInfo(0).IsName("Attack1") ||
           animator.GetCurrentAnimatorStateInfo(0).IsName("Attack2") ||
           animator.GetCurrentAnimatorStateInfo(0).IsName("Attack3") ||
           animator.GetCurrentAnimatorStateInfo(0).IsName("ChargeAttack");
}

public bool IsBlueFlashing()
{
    return spriteRenderer != null && spriteRenderer.color == Color.blue;
}






    public bool IsAttacking() => IsAttackingNow;
    public void Parry() => RegisterParry();
}
