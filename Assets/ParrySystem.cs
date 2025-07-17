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

        IStunnable stunnable = target.GetComponent<IStunnable>();
        if (stunnable == null) return false;

        if (target.CompareTag("Enemy"))
        {
            var handler = target.GetComponent<RegularGolemStun>();
            if (handler != null)
            {
                if (!handler.IsInParryCooldown)
                {
                    stunnable.Stun(1f);
                }

                // Always mark the parry as successful if it was a valid attack
                didSuccessfulParry = true;
                lastParryTime = Time.time;
                return true;
            }
        }
        else if (target.CompareTag("Boss"))
        {
            var bossStun = target.GetComponent<BossGolemStun>();
            if (bossStun != null)
            {
                bossStun.RegisterParry();
                didSuccessfulParry = true;
                return true;
            }
        }

        return false;
    }
}
