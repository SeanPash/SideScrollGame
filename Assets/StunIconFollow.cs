using UnityEngine;

public class StunIconFollow : MonoBehaviour
{
    public Transform boss;           // Assign this manually in Inspector
    public Vector3 offset = new Vector3(1f, 1f, 0f); // Right + above boss

    private float floatSpeed = 2f;
    private float floatAmount = 0.1f;

    void Update()
    {
        
        Debug.DrawRay(transform.position, Vector3.up * 0.5f, Color.yellow);

        if (boss == null) return;

        // Floating effect based on boss's current Y
        float floatY = boss.position.y + offset.y + Mathf.Sin(Time.time * floatSpeed) * floatAmount;
        Vector3 targetPos = new Vector3(boss.position.x + offset.x, floatY, boss.position.z);
        transform.position = targetPos;
    }
}
