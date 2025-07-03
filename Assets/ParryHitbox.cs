using UnityEngine;

public class ParryHitbox : MonoBehaviour
{
    public ParrySystem parrySystem;
    public LayerMask targetMask;

    private bool parryTriggered = false;

    void OnEnable()
    {
        parryTriggered = false;
        Debug.Log("ParryHitbox activated — checking manually.");

        ManualCheckParry(); // do a manual overlap check right when it's enabled
    }

    private void ManualCheckParry()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            transform.position,
            GetComponent<Collider2D>().bounds.size,
            0f,
            targetMask
        );

        foreach (var hit in hits)
        {
            if (parryTriggered) break;

            if (hit.CompareTag("Enemy") || hit.CompareTag("Boss"))
            {
                var attackState = hit.GetComponentInParent<IAttackState>();
                if (attackState != null && attackState.IsAttacking())
                {
                    Debug.Log("[ParryHitbox] Parrying: " + hit.name);
                    parrySystem.Parry(hit.gameObject);
                    parryTriggered = true;
                }
                else
                {
                    Debug.Log($"[ParryHitbox] Ignored parry — {hit.name} not attacking.");
                }
            }
        }
    }
}