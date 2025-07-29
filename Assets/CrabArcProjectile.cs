using UnityEngine;

public class CrabArcProjectile : MonoBehaviour
{
    public float launchForce = 7f;
    public float arcHeight = 3f;
    public float lifetime = 5f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

   public void Initialize(Vector2 targetPosition)
{
    Vector2 start = transform.position;
    Vector2 toTarget = targetPosition - start;

    float gravity = Mathf.Abs(Physics2D.gravity.y);
    float heightOffset = arcHeight;

    // Split into horizontal and vertical components
    float horizontalDistance = toTarget.x;
    float verticalDistance = toTarget.y;

    // Make sure we're not dividing by zero
    if (Mathf.Approximately(horizontalDistance, 0f))
        horizontalDistance = 0.01f;

    // Calculate time based on apex height
    float apexHeight = Mathf.Max(arcHeight, verticalDistance + arcHeight);
    float timeToApex = Mathf.Sqrt(2 * apexHeight / gravity);
    float totalTime = timeToApex + Mathf.Sqrt(2 * (apexHeight - verticalDistance) / gravity);

    // Calculate initial velocity
    float vx = horizontalDistance / totalTime;
    float vy = gravity * timeToApex;

    rb.gravityScale = 1f;
    rb.linearVelocity = new Vector2(vx, vy);

    Debug.Log("[ArcProjectile] Final Velocity: " + rb.linearVelocity);
}


    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
{
    int groundLayer = LayerMask.NameToLayer("Ground");
    int wallLayer = LayerMask.NameToLayer("Wall");

    if (other.CompareTag("Warrior") || 
        other.gameObject.layer == groundLayer || 
        other.gameObject.layer == wallLayer)
    {
        Destroy(gameObject);
    }
}

}
