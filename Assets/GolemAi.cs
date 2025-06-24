using UnityEngine;
using System.Collections;


public class GolemAi : MonoBehaviour
{
     private bool isStunned = false;
    private Animator animator;
    private Rigidbody2D rb;

    public float health = 20f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void Stun(float duration)
    {
        if (isStunned) return;

        isStunned = true;

        // Stop movement
        if (rb != null)
            rb.velocity = Vector2.zero;

        // Play hurt animation
        if (animator != null)
            animator.Play("Enemy Hurt");

        StartCoroutine(StunRoutine(duration));
    }

    IEnumerator StunRoutine(float time)
    {
        yield return new WaitForSeconds(time);
        isStunned = false;
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log("Enemy took " + amount + " damage.");

        if (health <= 0)
        {
            Die();
        }
        else
        {
            // Optional: small stun on hit
            Stun(0.3f);
        }
    }

    private void Die()
    {
        isStunned = true; // lock further behavior
        if (animator != null)
            animator.Play("Enemy Death");

        Destroy(gameObject, 2f); // wait for animation before destroy
    }
}