using UnityEngine;
using System.Collections;

public class BossGolemStun : MonoBehaviour, IStunnable
{
    public BossGolemBehavior bossBehavior;

    [Header("Stun Settings")]
    public float stunDuration = 1.5f;
    public float parryWindow = 2.0f;      // Time allowed between parries
    public float stunCooldown = 4.0f;

    private int parryCount = 0;
    private float lastParryTime = -Mathf.Infinity;
    private bool isOnCooldown = false;
    public GameObject stunIcon;

    private void Awake()
    {
        if (bossBehavior == null)
            bossBehavior = GetComponent<BossGolemBehavior>();
        if (stunIcon != null)
            stunIcon.SetActive(false); 
        
    }

    public void RegisterParry()
{
    Debug.Log("BossGolemStun: RegisterParry called");

    if (isOnCooldown || bossBehavior == null)
    {
        Debug.Log("Parry ignored — on cooldown or no bossBehavior.");
        return;
    }

    float currentTime = Time.time;

    // If too much time passed since last parry, reset counter
    if (currentTime - lastParryTime > parryWindow)
    {
        Debug.Log("Resetting parryCount due to timeout.");
        parryCount = 1; // Start fresh
    }
    else
    {
        parryCount++;
        Debug.Log("Parry count incremented to: " + parryCount);
    }

    lastParryTime = currentTime;

    if (parryCount >= 2)
    {
        Debug.Log("Parry threshold reached — applying stun.");
        StartCoroutine(ApplyStun());
    }
}


 private IEnumerator ApplyStun()
{
    Debug.Log("STUN STARTED");
    parryCount = 0;
    isOnCooldown = true;

    bossBehavior.isHurting = true;
    bossBehavior.rb.linearVelocity = Vector2.zero;

    // 1. Play Hurt animation
    bossBehavior.animator.Play("Enemy Hit");

    // 2. Wait briefly for Hurt animation to finish (~0.4 seconds)
    yield return new WaitForSeconds(0.4f); // adjust based on your animation length

    // 3. Transition to Idle while staying stunned
    bossBehavior.animator.Play("Enemy Idle");

    // 4. Show stun icon
    if (stunIcon != null)
    {
        Debug.Log("Enabling stun icon");
        stunIcon.SetActive(true);
    }

    // 5. Wait out the full stun duration
    yield return new WaitForSeconds(stunDuration - 0.4f);

    // 6. End stun
    bossBehavior.isHurting = false;
    if (stunIcon != null)
    {
        Debug.Log("Disabling stun icon");
        stunIcon.SetActive(false);
    }

    yield return new WaitForSeconds(stunCooldown);
    isOnCooldown = false;
}


    public void Stun(float duration)
{
    Debug.LogWarning("BossGolemStun.Stun(float) was called but is not used directly. Use RegisterParry instead.");
}
}
