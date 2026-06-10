using UnityEngine;

// Mini tornado fired by the Tornado Slime Boss. Travels in a straight line toward
// where the player was when fired. Despawns on Warrior, ground, or wall contact,
// or when its lifetime expires.
public class MiniTornadoProjectile : MonoBehaviour
{
    public float speed = 6f;
    public float lifetime = 4f;
    public int damage = 1;

    private Vector2 direction;
    private SpriteRenderer sr;

    // Ground sweep mode: when descending past the sweep height, the shot
    // levels out and runs horizontally, like a small tornado on the floor.
    private bool sweepEnabled;
    private float sweepLevelY;

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

    // Arms the ground sweep: a descending shot flattens into horizontal
    // travel once it falls to the given height.
    public void SetSweepLevel(float levelY)
    {
        sweepEnabled = true;
        sweepLevelY = levelY;
    }

    void Update()
    {
        // Level out into the floor sweep once the shot descends far enough,
        // keeping whatever horizontal direction it was thrown with.
        if (sweepEnabled && direction.y < 0f && transform.position.y <= sweepLevelY)
        {
            float dirX = direction.x != 0f ? Mathf.Sign(direction.x) : 1f;
            direction = new Vector2(dirX, 0f);
            transform.position = new Vector3(transform.position.x, sweepLevelY, transform.position.z);
            if (sr != null) sr.flipX = direction.x < 0f;
            sweepEnabled = false;
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
