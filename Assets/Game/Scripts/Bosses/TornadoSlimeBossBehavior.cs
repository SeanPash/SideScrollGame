using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TornadoSlimeBossBehavior : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Rigidbody2D rb;
    public Animator animator;
    public GameObject miniProjectilePrefab;
    public Transform[] wallBouncePoints;
    public SpriteRenderer spriteRenderer;
    public GameObject clonePrefab;


    [Header("Movement")]
    public float moveSpeed = 3f;
    public float dashSpeed = 10f;
    public float spinRange = 4f;
    public float wallBounceSpeed = 12f;

    [Header("Cooldowns")]
    public float cloneAttackInterval = 10f;
    public float wallBounceInterval = 20f;

    private float cloneTimer;
    private float wallBounceTimer;

    public bool isDead = false;
    public bool isAttacking = false;
    public bool canAttack = true;

    private Vector3 lastKnownPosition;

    void Update()
    {
        if (isDead || isAttacking || player == null) return;

        cloneTimer += Time.deltaTime;
        wallBounceTimer += Time.deltaTime;

        float distance = Vector2.Distance(transform.position, player.position);

        if (cloneTimer >= cloneAttackInterval)
        {
            cloneTimer = 0f;
            StartCoroutine(DoCloneSpinAttack());
            return;
        }

        if (wallBounceTimer >= wallBounceInterval)
        {
            wallBounceTimer = 0f;
            StartCoroutine(DoWallBounceAttack());
            return;
        }

        if (distance <= spinRange)
        {
            StartCoroutine(DoForwardSpinAttack());
        }
        else if (distance <= 6f)
        {
            StartCoroutine(DoMiniProjectileSpin());
        }
        else
        {
            MoveTowardsPlayer();
        }
    }

    void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
        animator.Play("Enemy Spin");
    }

    IEnumerator DoForwardSpinAttack()
    {
        isAttacking = true;
        canAttack = false;

        animator.Play("Enemy Spin");

        float dashDirection = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0);

        yield return new WaitForSeconds(0.8f);

        rb.linearVelocity = Vector2.zero;
        isAttacking = false;
        canAttack = true;
    }

    IEnumerator DoMiniProjectileSpin()
    {
        isAttacking = true;
        canAttack = false;

        animator.Play("Enemy Spin");
        rb.linearVelocity = Vector2.zero;

        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(1f);
            GameObject proj = Instantiate(miniProjectilePrefab, transform.position, Quaternion.identity);
            Vector2 dir = (player.position - transform.position).normalized;
            proj.GetComponent<MiniTornadoProjectile>().SetDirection(dir);
        }

        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
        canAttack = true;
    }
IEnumerator DoCloneSpinAttack()
{
    isAttacking = true;
    canAttack = false;
    lastKnownPosition = transform.position;

    // Disappear
    spriteRenderer.enabled = false;
    rb.linearVelocity = Vector2.zero;

    // Calculate spawn positions relative to player
    Vector3 playerPos = player.position;
    Vector3 leftSpawnPos = playerPos + new Vector3(-2f, 0f, 0f);
    Vector3 rightSpawnPos = playerPos + new Vector3(2f, 0f, 0f);

    // Spawn clones from a prefab (not self!) to avoid script conflicts
    GameObject leftClone = Instantiate(clonePrefab, leftSpawnPos, Quaternion.identity);
    GameObject rightClone = Instantiate(clonePrefab, rightSpawnPos, Quaternion.identity);

    // Play spin animation if animator exists
    Animator animL = leftClone.GetComponent<Animator>();
    Animator animR = rightClone.GetComponent<Animator>();
    if (animL != null) animL.Play("Enemy Attack 1");
    if (animR != null) animR.Play("Enemy Attack 1");

    // Move both clones toward player
    StartCoroutine(MoveCloneToPlayer(leftClone));
    StartCoroutine(MoveCloneToPlayer(rightClone));

    // Wait for attack to finish
    yield return new WaitForSeconds(1.2f);

    // Reappear at original position
    transform.position = lastKnownPosition;
    spriteRenderer.enabled = true;

    yield return new WaitForSeconds(0.3f);
    isAttacking = false;
    canAttack = true;
}
private IEnumerator MoveCloneToPlayer(GameObject clone)
{
    float speed = 6f;
    float duration = 1.2f;
    float timer = 0f;
    Vector2 direction = (player.position - clone.transform.position).normalized;

    Rigidbody2D rbClone = clone.GetComponent<Rigidbody2D>();
    if (rbClone != null)
        rbClone.gravityScale = 0; // Optional: if your clone falls, disable gravity

    while (timer < duration)
    {
        clone.transform.position += (Vector3)(direction * speed * Time.deltaTime);
        timer += Time.deltaTime;
        yield return null;
    }

    Destroy(clone);
}


    IEnumerator DoWallBounceAttack()
    {
        isAttacking = true;
        canAttack = false;
        animator.Play("Enemy Spin");

        Vector3 left = wallBouncePoints[0].position;
        Vector3 right = wallBouncePoints[1].position;

        int bounces = 3;
        for (int i = 0; i < bounces; i++)
        {
            Vector3 target = (i % 2 == 0) ? right : left;
            while (Vector2.Distance(transform.position, target) > 0.1f)
            {
                Vector2 moveDir = (target - transform.position).normalized;
                rb.linearVelocity = moveDir * wallBounceSpeed;
                yield return null;
            }
            rb.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(0.2f);
        }

        rb.linearVelocity = Vector2.zero;
        isAttacking = false;
        canAttack = true;
    }
}
