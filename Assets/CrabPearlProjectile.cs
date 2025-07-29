using UnityEngine;
using System.Collections;

public class CrabProjectileBehavior : MonoBehaviour
{
    public float speed = 6f;
    public float lifeTime = 5f;

    private Vector2 moveDirection;
    private bool hasHit = false;
    private Rigidbody2D body;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        StartCoroutine(LifetimeExpire());
    }

    public void Initialize(Vector2 targetPosition)
    {
Vector2 offset = targetPosition - (Vector2)transform.position;
moveDirection = new Vector2(offset.x, 0).normalized;  // Only move horizontally
        body.linearVelocity = moveDirection * speed;

        // Optional: Flip sprite based on direction
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.flipX = moveDirection.x < 0;
    }

    void Update()
    {
        if (!hasHit)
        {
            body.linearVelocity = moveDirection * speed;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
{
    if (hasHit) return;

    int layer = other.gameObject.layer;
    string layerName = LayerMask.LayerToName(layer);

    if (other.CompareTag("Warrior") || layerName == "Wall")
    {
        hasHit = true;
        Destroy(gameObject);
    }
}

    private IEnumerator LifetimeExpire()
    {
        yield return new WaitForSeconds(lifeTime);
        if (!hasHit)
        {
            Destroy(gameObject);
        }
    }
}
