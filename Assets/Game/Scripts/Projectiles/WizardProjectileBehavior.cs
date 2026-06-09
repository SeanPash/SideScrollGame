using UnityEngine;
using System.Collections;

public class WizardProjectileBehavior : MonoBehaviour
{
    public float speed = 6f;
    public float lifeTime = 5f;

    public ShootingWizardBehavior ownerWizard;

    private Vector2 moveDirection;
    private Animator animator;
    private bool hasExploded = false;
    Rigidbody2D body;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }


    void Start()
    {
        animator = GetComponent<Animator>();
        animator.Play("Projectile");
        StartCoroutine(LifetimeExpire());
    }

public void Initialize(Vector2 targetPosition)
{
    StartCoroutine(DelayedInitialize(targetPosition));
}
 private IEnumerator DelayedInitialize(Vector2 targetPosition)
{
    yield return null;

    moveDirection = (targetPosition - (Vector2)transform.position).normalized;

    body.linearVelocity = moveDirection * speed;

    SpriteRenderer sr = GetComponent<SpriteRenderer>();
    if (sr != null)
        sr.flipX = moveDirection.x < 0;
}



   void Update()
{
    if (!hasExploded && moveDirection != Vector2.zero)
    {
        body.linearVelocity = moveDirection * speed;
    }
}

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded) return;

        if (other.CompareTag("Warrior"))
        {
            StartCoroutine(ExplodeAndDestroy());
        }
    }

    private IEnumerator ExplodeAndDestroy()
    {
        hasExploded = true;
        speed = 0;
        GetComponent<Collider2D>().enabled = false;
GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

        animator.Play("Explode");

        yield return new WaitForSeconds(0.5f);

        ownerWizard?.OnProjectileComplete();

        Destroy(gameObject);
    }

    private IEnumerator LifetimeExpire()
    {
        yield return new WaitForSeconds(lifeTime);
        if (!hasExploded)
        {
            StartCoroutine(ExplodeAndDestroy());
        }
    }
}
