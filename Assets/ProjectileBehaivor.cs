using UnityEngine;

public class BossGolemProjectile : MonoBehaviour
{
    public float speed = 8f;
    public float lifetime = 3f;
    public int damage = 10;

    private Vector2 direction;

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Damage logic goes here
            Debug.Log("Hit player for " + damage + " damage.");
            // other.GetComponent<PlayerHealth>()?.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (!other.isTrigger) // hits wall or ground
        {
            Destroy(gameObject);
        }
    }
}
