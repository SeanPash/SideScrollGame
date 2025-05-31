using UnityEngine;
using System.Collections;


public class EnemyAI : MonoBehaviour
{
    private bool isStunned = false;

    public void Stun(float duration)
    {
        if (isStunned) return;

        isStunned = true;
        StartCoroutine(StunRoutine(duration));
    }

    IEnumerator StunRoutine(float time)
    {
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(time);
        isStunned = false;
    }
    public void TakeDamage(float amount)
{
    // Add logic for applying damage, playing effects, etc.
    Debug.Log("Enemy took " + amount + " damage.");
}
}
