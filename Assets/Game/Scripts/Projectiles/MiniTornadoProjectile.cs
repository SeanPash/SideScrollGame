using UnityEngine;

// Mini tornado fired by the Tornado Slime Boss. Travels in a straight line toward
// where the player was when fired. Despawns on Warrior, ground, or wall contact,
// or when its lifetime expires.
public class MiniTornadoProjectile : MonoBehaviour
{
    public float speed = 6f;
    public float lifetime = 4f;
    public int damage = 1;

    [Header("Homing")]
    // Optional steering toward a target for a limited time after spawn, with
    // a capped turn rate so it can still be outrun or sidestepped. Used by
    // the radial burst so the ring curves in on the player.
    public float homingTurnRate = 140f;
    public float homingDuration = 1.1f;

    private Vector2 direction;
    private Transform homingTarget;
    private float homingTime;
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    // Sets the travel direction once at spawn time and faces the sprite that way.
    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        if (sr != null) sr.flipX = direction.x < 0f;
    }

    // Enables homing toward the target for the configured duration.
    public void SetHomingTarget(Transform target)
    {
        homingTarget = target;
        homingTime = homingDuration;
    }

    void Update()
    {
        // Steer toward the target while the homing window lasts, then fly
        // straight along the final heading.
        if (homingTarget != null && homingTime > 0f)
        {
            homingTime -= Time.deltaTime;
            Vector2 toTarget = ((Vector2)homingTarget.position - (Vector2)transform.position).normalized;
            float maxRadians = homingTurnRate * Mathf.Deg2Rad * Time.deltaTime;
            direction = ((Vector2)Vector3.RotateTowards(direction, toTarget, maxRadians, 0f)).normalized;
            if (sr != null) sr.flipX = direction.x < 0f;
        }

        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Warrior"))
        {
            // Damage only on the body collider; Warrior-tagged trigger hitboxes
            // still destroy the projectile (lets attacks swat it down).
            if (!collision.isTrigger)
            {
                PlayerHealth health = collision.GetComponentInParent<PlayerHealth>();
                if (health != null) health.TakeDamage(damage);
            }
            Destroy(gameObject);
            return;
        }

        int layer = collision.gameObject.layer;
        if (LayerMask.LayerToName(layer) == "Ground" || LayerMask.LayerToName(layer) == "Wall")
        {
            Destroy(gameObject);
        }
    }
}
