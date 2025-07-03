using UnityEngine;

public class ParrySystem : MonoBehaviour
{
    [Header("Parry Settings")]
    public float regularParryCooldown = 4f;
    private float lastParryTime = -Mathf.Infinity;

    [Header("Boss Parry Logic")]
    private int bossParryCount = 0;
    public int bossParryThreshold = 2; // e.g., 2 parries to stun
    public bool didSuccessfulParry = false;


    public void Parry(GameObject target)
    {
    didSuccessfulParry = true;
        Debug.Log("Trying to parry: " + target.name);
        if (target == null) return;

        IStunnable stunnable = target.GetComponent<IStunnable>();
        if (stunnable == null) return;

        // Check if target is already in parry cooldown
        var handler = target.GetComponent<GolemStunHandler>();
        if (handler != null && handler.IsInParryCooldown) return;

        if (target.CompareTag("Enemy"))
        {
            stunnable.Stun(1f);
            lastParryTime = Time.time;
        }
        else if (target.CompareTag("Boss"))
        {
            bossParryCount++;
            Debug.Log($"Parried Boss! Count: {bossParryCount}");

            if (bossParryCount >= bossParryThreshold)
            {
                stunnable.Stun(1.5f);
                bossParryCount = 0;
                lastParryTime = Time.time;
            }
        }
    }
}