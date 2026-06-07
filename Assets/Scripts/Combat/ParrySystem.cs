using UnityEngine;

public class ParrySystem : MonoBehaviour
{
    [Header("Parry Settings")]
    public float regularParryCooldown = 4f;
    private float lastParryTime = -Mathf.Infinity;

    public bool didSuccessfulParry = false;

    public bool Parry(GameObject target)
    {
        didSuccessfulParry = false;
        if (target == null) return false;

        // Martial Hero Phase 1
        var phase1 = target.GetComponentInParent<MartialHeroBehavior>();
        if (phase1 != null && phase1.IsAttacking())
        {
            if (phase1.CanBeParried())
            {
                Debug.Log("[ParrySystem] Parried Phase1 attack.");
                phase1.Parry();
                didSuccessfulParry = true;
                lastParryTime = Time.time;
                return true;
            }
            else
            {
                Debug.Log("[ParrySystem] Phase1 is attacking but not parryable.");
            }
        }

        // Martial Hero Phase 2
        var phase2 = target.GetComponentInParent<MartialHeroPhase2>();
        if (phase2 != null && phase2.IsAttacking())
        {
            if (phase2.IsInParryableState()) // Ensure this method is public
            {
                Debug.Log("[ParrySystem] Parried Phase2 attack.");
                phase2.Parry();
                didSuccessfulParry = true;
                lastParryTime = Time.time;
                return true;
            }
            else
            {
                Debug.Log("[ParrySystem] Phase2 is attacking but not in parryable state.");
            }
        }

        // Boss Golem - always accepts parry
        var bossStun = target.GetComponentInParent<BossGolemStun>();
        if (bossStun != null)
        {
            bossStun.RegisterParry();
            Debug.Log("[ParrySystem] Parried Boss Golem.");
            didSuccessfulParry = true;
            lastParryTime = Time.time;
            return true;
        }

        // Crab Boss - only parryable if not flashing blue
        var crabBoss = target.GetComponentInParent<CrabBossBehaviorPhase1>();
        if (crabBoss != null && crabBoss.IsAttacking())
        {
            if (crabBoss.CanBeParried())
            {
                crabBoss.OnParried();
                Debug.Log("[ParrySystem] Parried Crab Boss.");
                didSuccessfulParry = true;
                lastParryTime = Time.time;
                return true;
            }
        }

// Samurai knife projectile
var knife = target.GetComponent<SamuraiProjectile>();
if (knife != null && !knife.IsParried() && knife.CanBeParried())
{
    knife.Parry();
    Debug.Log("[ParrySystem] Knife projectile parried!");
    didSuccessfulParry = true;
    lastParryTime = Time.time;
    return true;
}

        Debug.Log($"[ParrySystem] Target '{target.name}' is not parryable or not in a valid state.");
        return false;
    }

    public bool IsFacing(GameObject target)
    {
        Vector2 toTarget = target.transform.position - transform.position;
        float dot = Vector2.Dot(toTarget.normalized, transform.right);
        return dot > 0; // true if target is in front of player
    }
}
