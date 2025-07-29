using UnityEngine;
using System.Collections;

public class ShootingWizardBehavior : MonoBehaviour
{
    public Transform player;
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;

    public float moveSpeed = 3f;
    public float attackRange = 10f;
    public float retreatDistance = 1f;
    public float shootCooldown = 5f;

    private bool canShoot = true;
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private bool isProjectileActive = false;
    private bool isRetreating = false;
    private bool canRetreat = true;
    public float retreatDuration = 1f;
    public float retreatCooldown = 10f;
    private ShootingWizardHealth health;



    private void Awake()
    {
        health = GetComponent<ShootingWizardHealth>();
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
          if (health != null && health.isDead) return;
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        Vector2 direction = (player.position - transform.position).normalized;

        if (distance < retreatDistance && canRetreat && !isRetreating)
        {
            if (health != null && health.isDead) return;
            StartCoroutine(Retreat(direction));
            return;
        }

        if (isRetreating && distance >= retreatDistance + 1f)
        {
            // Done retreating once far enough
            isRetreating = false;
        }

        if (isRetreating)
        {
            // Keep running away if not far enough yet
            rb.linearVelocity = new Vector2(-direction.x * moveSpeed, rb.linearVelocity.y);
            return;
        }

        // Normal behavior when in attack range
        rb.linearVelocity = Vector2.zero;
        sr.flipX = direction.x < 0;

        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            animator.Play("Idle");
        }

        if (canShoot && !isProjectileActive)
        {
            if (health != null && health.isDead) return;
            StartCoroutine(ShootProjectile());
        }
    }
    private IEnumerator ShootProjectile()
    {
        canShoot = false;
        isProjectileActive = true;
        animator.Play("Attack");

        yield return new WaitForSeconds(0.5f);

        if (projectilePrefab && projectileSpawnPoint)
        {
            Vector3 spawnOffset = projectileSpawnPoint.localPosition;
            spawnOffset.x = Mathf.Abs(spawnOffset.x) * (sr.flipX ? -1 : 1);
            projectileSpawnPoint.localPosition = spawnOffset;

            GameObject proj = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity);

            Collider2D playerCol = player.GetComponent<Collider2D>();
            Vector2 targetPos = playerCol != null ? playerCol.bounds.center : (Vector2)player.position;

            var projScript = proj.GetComponent<WizardProjectileBehavior>();
            projScript.Initialize(targetPos);

            projScript.ownerWizard = this;
            animator.Play("Idle");
        }
    }
    public void OnProjectileComplete()
    {
        isProjectileActive = false;
        if (health != null && health.isDead) return;
        StartCoroutine(ResetShootCooldown());
    }

    private IEnumerator ResetShootCooldown()
    {
        yield return new WaitForSeconds(shootCooldown);
        canShoot = true;
    }
    private IEnumerator Retreat(Vector2 direction)
    {
        isRetreating = true;
        canRetreat = false;

        float timer = 0f;
        animator.Play("Run");

        while (timer < retreatDuration)
        {
            rb.linearVelocity = new Vector2(-direction.x * moveSpeed, rb.linearVelocity.y);
            sr.flipX = direction.x > 0;
            timer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        isRetreating = false;
        animator.Play("Idle");
        if (health != null && health.isDead) yield break;
        StartCoroutine(ResetRetreatCooldown());
    }

    private IEnumerator ResetRetreatCooldown()
    {
        yield return new WaitForSeconds(retreatCooldown);
        canRetreat = true;
    }


}