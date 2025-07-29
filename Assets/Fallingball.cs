using UnityEngine;

public class FallingBall : MonoBehaviour
{
    public float speed = 8f;
    public float destroyY = -6f; // Y-level below arena floor

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // Force it downward at start
        rb.linearVelocity = Vector2.down * speed;
    }

    void Update()
    {
        // Destroy if it goes below the arena
        if (transform.position.y < destroyY)
        {
            Destroy(gameObject);
        }
    }
}
