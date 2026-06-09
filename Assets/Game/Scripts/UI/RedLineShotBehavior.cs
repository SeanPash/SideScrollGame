using UnityEngine;

public class RedLineShotBehavior : MonoBehaviour
{
    public float speed = 8f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // Drop straight down
        if (rb != null)
        {
            rb.linearVelocity = Vector2.down * speed;
        }
    }
}
