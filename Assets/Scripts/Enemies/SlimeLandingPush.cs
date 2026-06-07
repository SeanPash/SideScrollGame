using UnityEngine;

public class SlimeLandingPush : MonoBehaviour
{
    public float pushForce = 5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("LandingHitbox triggered with: " + other.name);
        if (other.CompareTag("Warrior"))
        {
            Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                float direction = Mathf.Sign(other.transform.position.x - transform.position.x);
                playerRb.linearVelocity = new Vector2(direction * pushForce, 2f); // or use AddForce

                // Tell movement script to pause
                var warriorScript = other.GetComponent<WarriorController>();
                if (warriorScript != null)
                {
                    warriorScript.isBeingPushed = true;
                    warriorScript.pushTimer = 0.25f;
                }
            }
        }
    }
}
