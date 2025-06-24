using UnityEngine;
public class DealDamage : MonoBehaviour
{
    public bool isFinalComboHit;
    public bool isChargeAttack;
    public float chargeTime;

    public int CalculateDamage()
    {
        if (isChargeAttack)
        {
            float maxDamage = 8f;
            float scaled = maxDamage - (chargeTime * 0.005f);
            return Mathf.Clamp(Mathf.RoundToInt(scaled), PlayerStats.Instance.baseDamage, (int)maxDamage);
        }

        return PlayerStats.Instance.GetDamage(isFinalComboHit);
    }
    private void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("Boss") || other.CompareTag("Enemy"))
    {
        DealDamage damageSource = GetComponent<DealDamage>();
        int damage = damageSource != null ? damageSource.CalculateDamage() : 1;

        // Tries RegularGolemHealth
        var regHealth = other.GetComponent<RegularGolemHealth>();
        if (regHealth != null)
        {
            regHealth.TakeDamage(damage);
        }

        // Tries GolemAI
        var ai = other.GetComponent<GolemAi>();
        if (ai != null)
        {
            ai.TakeDamage(damage);
        }

        // Optional: also try stun
        var stun = other.GetComponent<GolemStun>();
        if (stun != null)
        {
            stun.Stun(0.5f);
        }
    }
}

}
