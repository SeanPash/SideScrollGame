using UnityEngine;

public class ParryHitbox : MonoBehaviour
{
    public ParrySystem parrySystem;
    public LayerMask targetMask;

    private bool parryTriggered = false;

    public bool parrySucceeded = false;

    void OnEnable()
    {
        parryTriggered = false;
        parrySucceeded = false;
        Debug.Log("ParryHitbox activated — checking manually.");

        ManualCheckParry();
    }

    private void ManualCheckParry()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            transform.position,
            GetComponent<Collider2D>().bounds.size + new Vector3(1f, 0, 0),
            0f,
            targetMask
        );

        foreach (var hit in hits)
        {
            if (parryTriggered) break;

            IAttackState attacker = hit.GetComponentInParent<IAttackState>();
            if (attacker != null)
            {
                bool attacking = attacker.IsAttacking();
                Debug.Log($"[ParryHitbox] Checking {hit.name} | Attacking: {attacking}");

                if (attacking)
                {
                    Debug.Log($"[ParryHitbox] Parrying: {hit.name} (Resolved to: {attacker})");
                    attacker.Parry(); // This should trigger the stun or effect
                    parrySucceeded = true;
                    parryTriggered = true;
                    return;
                }
                else
                {
                    Debug.Log($"[ParryHitbox] {hit.name} was not attacking — parry failed.");
                }
            }
            else
            {
                Debug.LogWarning($"[ParryHitbox] No IAttackState found on {hit.name} or parent.");
            }
        }
    }
}