using UnityEngine;
using System.Collections;

public class BossGolemStun : MonoBehaviour
{
    public BossGolemBehavior bossBehavior;

    [Header("Stun Settings")]
    public float stunDuration = 1.5f;
    public float parryWindow = 2.0f;      // Time allowed between parries
    public float stunCooldown = 4.0f;

    private int parryCount = 0;
    private float lastParryTime = -Mathf.Infinity;
    private bool isOnCooldown = false;

    private void Awake()
    {
        if (bossBehavior == null)
            bossBehavior = GetComponent<BossGolemBehavior>();
    }

    public void RegisterParry()
    {
        if (isOnCooldown || bossBehavior == null) return;

        float currentTime = Time.time;

        // If too much time passed between parries, reset
        if (currentTime - lastParryTime > parryWindow)
        {
            parryCount = 1; // start fresh
        }
        else
        {
            parryCount++;
        }

        lastParryTime = currentTime;

        if (parryCount >= 2)
        {
            StartCoroutine(ApplyStun());
        }
    }

    private IEnumerator ApplyStun()
    {
        Debug.Log("Boss Golem is STUNNED!");

        // Reset logic
        parryCount = 0;
        isOnCooldown = true;

        // Set hurting/stunned state
        bossBehavior.isHurting = true;
        bossBehavior.rb.linearVelocity = Vector2.zero;
        bossBehavior.animator.Play("Enemy Hit"); // This should match current form's hurt animation

        // Wait during stun
        yield return new WaitForSeconds(stunDuration);

        bossBehavior.isHurting = false;

        // Start cooldown
        yield return new WaitForSeconds(stunCooldown);
        isOnCooldown = false;
    }
}
