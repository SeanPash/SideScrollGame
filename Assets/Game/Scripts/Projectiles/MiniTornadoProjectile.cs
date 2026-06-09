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

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    // Sets the travel direction once at spawn time.
    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
    }

    void Update()
    {
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
