using UnityEngine;

public class SamuraiProjectile : MonoBehaviour
{
    public float speed = 8f;
    public float lifetime = 3f;

    [SerializeField] private int reflectDamage = 10;  // Damage dealt to the boss by a reflected knife

    private Vector2 direction;
    private SpriteRenderer sr;
    private bool wasParried = false;
    private bool canBeParried = true;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        Destroy(gameObject, lifetime);  // Destroy the projectile after lifetime
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (sr != null)
            sr.flipX = direction.x < 0 && Mathf.Abs(direction.x) > Mathf.Abs(direction.y);
    }

    public void Parry()
    {
        if (wasParried) return;

        wasParried = true;
        direction = -direction;  // Reflect the direction of the projectile

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (sr != null)
            sr.flipX = direction.x < 0 && Mathf.Abs(direction.x) > Mathf.Abs(direction.y);

        Debug.Log("[Knife] Deflected back!");

        // The reflected knife keeps flying; the lifetime timer from Start() cleans it up
    }

    public bool IsParried() => wasParried;
    public void SetParryable(bool value) => canBeParried = value;
    public bool CanBeParried() => canBeParried;

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // A parried knife ignores everything except the Boss: damage it, then destroy the knife
        if (wasParried && other.CompareTag("Boss"))
        {
            IDamageable boss = other.GetComponentInParent<IDamageable>();
            if (boss != null)
                boss.TakeDamage(reflectDamage);

            Debug.Log("[Knife] Reflected knife hit the Boss for " + reflectDamage + " damage.");
            Destroy(gameObject);
        }
        else if (!wasParried && !other.isTrigger)
        {
            // If not parried, just destroy it after hitting something that is not a trigger
            Destroy(gameObject);
        }
    }
}
