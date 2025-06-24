using UnityEngine;

public class ParrySystem : MonoBehaviour
{
    [Header("Parry Settings")]
    public float regularParryCooldown = 4f;
    private float lastParryTime = -Mathf.Infinity;

    [Header("Boss Parry Logic")]
    private int bossParryCount = 0;
    public int bossParryThreshold = 2; // e.g., 2 parries to stun

    public void Parry(GameObject target)
    {
        if (target == null) return;

        IStunnable stunnable = target.GetComponent<IStunnable>();
        if (stunnable == null) return;

        if (target.CompareTag("Enemy"))
        {
            if (Time.time - lastParryTime >= regularParryCooldown)
            {
                stunnable.Stun(1f); // regular enemies
                lastParryTime = Time.time;
            }
        }
        else if (target.CompareTag("Boss"))
        {
            bossParryCount++;
            Debug.Log($"Parried Boss! Count: {bossParryCount}");

            if (bossParryCount >= bossParryThreshold)
            {
                stunnable.Stun(1.5f); // boss stun duration
                bossParryCount = 0;

                // Optional: also trigger cooldown to prevent spam
                lastParryTime = Time.time;
            }
        }
    }
}
