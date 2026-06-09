using UnityEngine;

// Hazard zone left by the Slime Boss slam. Damages the Warrior on a tick interval
// and slows their movement while inside. Despawns after a lifetime. Static
// bookkeeping keeps the slow correct when zones overlap.
public class SlimeGooZone : MonoBehaviour
{
    [Header("Damage")]
    public int damagePerTick = 1;
    public float tickInterval = 0.75f;

    [Header("Slow")]
    [Range(0.1f, 1f)]
    public float slowMultiplier = 0.5f;

    [Header("Lifetime")]
    public float lifetime = 6f;

    // Shared slow bookkeeping so overlapping zones do not fight over the speed value.
    private static int zonesContainingPlayer = 0;
    private static float originalMoveSpeed = -1f;

    private float tickTimer = 0f;
    private bool playerInside = false;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger || !other.CompareTag("Warrior")) return;
        playerInside = true;
        ApplySlow(other.GetComponentInParent<WarriorController>());
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.isTrigger || !other.CompareTag("Warrior")) return;

        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            tickTimer = 0f;
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health != null) health.TakeDamage(damagePerTick);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.isTrigger || !other.CompareTag("Warrior")) return;
        playerInside = false;
        RemoveSlow(other.GetComponentInParent<WarriorController>());
    }

    // Restore the slow if the zone despawns while the player is still inside.
    void OnDestroy()
    {
        if (!playerInside) return;

        GameObject warrior = GameObject.FindWithTag("Warrior");
        if (warrior != null)
            RemoveSlow(warrior.GetComponent<WarriorController>());
    }

    // First zone entered stores the original speed and applies the slow.
    private void ApplySlow(WarriorController controller)
    {
        if (controller == null) return;

        if (zonesContainingPlayer == 0)
        {
            originalMoveSpeed = controller.moveSpeed;
            controller.moveSpeed = originalMoveSpeed * slowMultiplier;
        }
        zonesContainingPlayer++;
    }

    // Last zone exited restores the original speed.
    private void RemoveSlow(WarriorController controller)
    {
        if (controller == null) return;

        zonesContainingPlayer = Mathf.Max(0, zonesContainingPlayer - 1);
        if (zonesContainingPlayer == 0 && originalMoveSpeed > 0f)
        {
            controller.moveSpeed = originalMoveSpeed;
        }
    }
}
