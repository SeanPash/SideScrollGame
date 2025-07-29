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

       IAttackState attacker = target.GetComponentInParent<IAttackState>();
if (attacker != null && attacker.IsAttacking())
{
    // Extra protection — check if the attack is *actually* parryable
    var type = attacker.GetType();
    var canBeParriedMethod = type.GetMethod("CanBeParried");
    if (canBeParriedMethod != null)
    {
        bool canBeParried = (bool)canBeParriedMethod.Invoke(attacker, null);
        if (canBeParried)
        {
            attacker.Parry();
            didSuccessfulParry = true;
            lastParryTime = Time.time;
            return true;
        }
    }
}


        // 2. Boss Golem — always accepts parry regardless of attack state
        var bossStun = target.GetComponentInParent<BossGolemStun>();
        if (bossStun != null)
        {
            bossStun.RegisterParry();
            didSuccessfulParry = true;
            lastParryTime = Time.time;
            return true;
        }

        // 3. Martial Hero — ONLY parry if actively attacking AND in valid attack phase
        var martialHero = target.GetComponentInParent<MartialHeroBehavior>();
        if (martialHero != null && martialHero.IsAttacking())
        {
            // Must be in a parryable animation state (Attack1a/b or Attack2)
            var method = martialHero.GetType().GetMethod("IsInParryableState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool isParryable = (bool)method.Invoke(martialHero, null);

            if (isParryable)
            {
                martialHero.RegisterParry();
                didSuccessfulParry = true;
                lastParryTime = Time.time;
                return true;
            }
        }
        // 4. Crab Boss — Only parryable if using AttackB and not blue
var crabBoss = target.GetComponentInParent<CrabBossBehaviorPhase1>();
        if (crabBoss != null && crabBoss.IsAttacking())
        {
            if (crabBoss.CanBeParried())
            {
                crabBoss.OnParried(); // Or crabBoss.Stun() if that's your method
                didSuccessfulParry = true;
                lastParryTime = Time.time;
                return true;
            }
        }


        // 4. No valid parry target
        Debug.Log($"[ParrySystem] Target '{target.name}' is not parryable or not in a valid state.");
        return false;
    }
}
