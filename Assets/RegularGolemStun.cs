using UnityEngine;
using System.Collections;

public class GolemStunHandler : MonoBehaviour, IStunnable
{
    [Header("Stun Settings")]
    public float stunDuration = 1f;
    public float stunCooldown = 5f;

    private float lastStunTime = -Mathf.Infinity;
    private bool isStunned = false;

    private Animator animator;
    private SpriteRenderer sr;

    void Awake()
    {
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    public void Stun(float duration)
    {
        if (Time.time - lastStunTime < stunCooldown || isStunned)
            return;

        lastStunTime = Time.time;
Debug.Log("Golem stunned! Caller:\n" + UnityEngine.StackTraceUtility.ExtractStackTrace());
        StartCoroutine(HandleStun());
    }

    private IEnumerator HandleStun()
    {
        isStunned = true;

        if (animator != null)
            animator.Play("Enemy Hit");

        if (sr != null)
            sr.color = Color.red;

        yield return new WaitForSeconds(stunDuration);

        if (sr != null)
            sr.color = Color.white;

        isStunned = false;
    }

    public bool IsStunned()
    {
        return isStunned;
    }
    public bool IsInParryCooldown => Time.time - lastStunTime < stunCooldown;
}
