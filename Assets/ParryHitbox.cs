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
        Debug.Log("ParryHitbox enabled — waiting for manual trigger.");

    }

    public void ManualCheckParry()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            transform.position,
            GetComponent<Collider2D>().bounds.size + new Vector3(1f, 0f, 0f),
            0f,
            targetMask
        );

        foreach (var hit in hits)
        {
            if (parryTriggered) break;

            GameObject target = hit.gameObject;
            Debug.Log($"[ParryHitbox] Checking {target.name}");

            bool result = parrySystem.Parry(target);  // Use centralized logic!

            if (result)
            {
                parrySucceeded = true;
                parryTriggered = true;
                Debug.Log($"[ParryHitbox] Parry SUCCESS on {target.name}");
                return;
            }
            else
            {
                Debug.Log($"[ParryHitbox] Parry FAILED on {target.name}");
            }
        }
    }
}
