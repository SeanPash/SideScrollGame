using UnityEngine;

public class PurpleStunIconFollow : MonoBehaviour
{
    public Transform target;  
    public Vector3 offset = new Vector3(-.4f, .4f, 0f);  // Default offset (will flip if needed)

    private float floatSpeed = 2f;
    private float floatAmount = 0.1f;

    private SpriteRenderer targetSprite;

    void Start()
    {
        if (target != null)
        {
            targetSprite = target.GetComponent<SpriteRenderer>();
        }
    }

    void Update()
    {
        if (target == null) return;

        float floatY = Mathf.Sin(Time.time * floatSpeed) * floatAmount;

        Vector3 adjustedOffset = offset;

        // Flip X offset if target is flipped
        if (targetSprite != null && targetSprite.flipX)
        {
            adjustedOffset.x = -offset.x;
        }

        transform.position = target.position + adjustedOffset + Vector3.up * floatY;
    }
}
