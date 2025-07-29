using UnityEngine;

public class HorizontalProjectile : MonoBehaviour
{
    public float speed = 10f;
    public Vector2 direction = Vector2.left; // Default direction

    void Update()
    {
        transform.Translate(direction.normalized * speed * Time.deltaTime);
    }

    public void Initialize(Vector2 dir)
    {
        direction = dir;
    }
}
