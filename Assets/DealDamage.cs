using UnityEngine;
public class DealDamage : MonoBehaviour
{
    public bool isFinalComboHit;
    public bool isChargeAttack;
    public float chargeTime;
    private bool hasHit = false;
    private int attackFrame = 0;
    public LayerMask targetMask;


    public bool damageWindowActive = false;
    public int overrideDamage = -1;


    public int CalculateDamage()
    {
        if (overrideDamage >= 0)
            return overrideDamage;

        if (isChargeAttack)
        {
            float maxDamage = 8f;
            float scaled = maxDamage - (chargeTime * 0.005f);
            return Mathf.Clamp(Mathf.RoundToInt(scaled), PlayerStats.Instance.baseDamage, (int)maxDamage);
        }

        return PlayerStats.Instance.GetDamage(isFinalComboHit);
    }
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!damageWindowActive || hasHit) return;

    if (other.CompareTag("Boss") || other.CompareTag("Enemy"))
    {
        Debug.Log($"[DealDamage] Hit {other.name} on attackFrame {attackFrame}");
        int damage = CalculateDamage();

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            hasHit = true;
        }

        var stunnable = other.GetComponentInParent<IStunnable>();
        if (stunnable != null)
        {
            stunnable.Stun(1f);
        }
    }
}



    public void ResetHit()
    {
        hasHit = false;
    }
    public void EndHitWindow()
    {
        damageWindowActive = false;
    }

    public void BeginHitWindow(int frame)
    {
        attackFrame = frame;
        hasHit = false;
        damageWindowActive = true;
    }
    public void ManualCheckHits()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, GetComponent<Collider2D>().bounds.size, 0f, targetMask);

        foreach (var hit in hits)
        {
            if (hasHit) break; // Only one enemy per swing (optional)

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(CalculateDamage());
                hasHit = true;
            }

            IStunnable stunnable = hit.GetComponentInParent<IStunnable>();
            if (stunnable != null)
            {
                stunnable.Stun(1f);
            }
        }
    }

    


}
