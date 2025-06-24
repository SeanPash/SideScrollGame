using UnityEngine;
using System.Collections;

public class GolemStun : MonoBehaviour
{
    private bool isStunned = false;

    private Animator animator;
    private MonoBehaviour aiScript; // For general use
    private Rigidbody2D rb;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        // Try to find and store any one of the known AI scripts
        aiScript = GetComponent<RegularGolemBehavior>() as MonoBehaviour;
        if (aiScript == null)
            aiScript = GetComponent<BossGolemBehavior>() as MonoBehaviour;
        if (aiScript == null)
            aiScript = GetComponent<GolemAi>() as MonoBehaviour;
    }

    public void Stun(float duration)
    {
        if (isStunned) return;

        isStunned = true;
        StartCoroutine(StunCoroutine(duration));
    }

    IEnumerator StunCoroutine(float time)
    {
        // Stop enemy logic
        if (aiScript != null) aiScript.enabled = false;
        if (rb != null) rb.velocity = Vector2.zero;

        // Play Hurt animation
        if (animator != null) animator.Play("Enemy Hurt");

        yield return new WaitForSeconds(time);

        // Resume behavior
        if (aiScript != null) aiScript.enabled = true;

        isStunned = false;
    }
}
