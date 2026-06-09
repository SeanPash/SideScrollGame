using UnityEngine;

// Deals contact damage to the Warrior while this object's collider touches them.
// Reusable for boss bodies, landing hitboxes, and clones. A per-hit cooldown
// prevents damage from applying every physics frame. Toggle the component's
// enabled flag to arm/disarm it (checked manually because Unity still delivers
// trigger messages to disabled MonoBehaviours).
public class DamagePlayerOnContact : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 1;
    public float hitCooldown = 1f;

    private float lastHitTime = -999f;

    void OnTriggerEnter2D(Collider2D other) { TryDamage(other); }
    void OnTriggerStay2D(Collider2D other) { TryDamage(other); }
    void OnCollisionEnter2D(Collision2D collision) { TryDamage(collision.collider); }
    void OnCollisionStay2D(Collision2D collision) { TryDamage(collision.collider); }

    // Applies damage if armed, the collider is the Warrior's body, and the cooldown elapsed.
    private void TryDamage(Collider2D other)
    {
        if (!enabled) return;
        if (other.isTrigger) return;
        if (!other.CompareTag("Warrior")) return;
        if (Time.time - lastHitTime < hitCooldown) return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        lastHitTime = Time.time;
        health.TakeDamage(damage);
    }
}
