using UnityEngine;

public class RegularStunIconFollow : MonoBehaviour
{
     public Transform target;  // Regular Golem this icon follows
    public Vector3 offset = new Vector3(0.8f, 1.2f, 0f);  // Position relative to golem

    private float floatSpeed = 2f;
    private float floatAmount = 0.1f;

    void Update()
    {
        if (target == null) return;

        float floatY = Mathf.Sin(Time.time * floatSpeed) * floatAmount;
        transform.position = target.position + offset + Vector3.up * floatY;
    }
}