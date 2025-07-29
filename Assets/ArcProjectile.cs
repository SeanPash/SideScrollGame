using UnityEngine;

public class ArcProjectile : MonoBehaviour
{
    public float damage = 10f;
    public float lifetime = 5f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Warrior"))
        {
            IDamageable dmg = collision.collider.GetComponent<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage((int)damage);
            }
        }

        // Destroy on any collision (ground, player, wall)
        Destroy(gameObject);
    }
   public void Initialize(Vector2 direction, float force)
{
    if (rb != null)
    {
        // Add an upward curve to the projectile's direction
        Vector2 arcDirection = (direction + Vector2.up * 0.75f).normalized;
        rb.AddForce(arcDirection * force, ForceMode2D.Impulse);
    }
}


}
