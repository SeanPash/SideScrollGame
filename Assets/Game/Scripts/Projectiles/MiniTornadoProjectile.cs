using UnityEngine;

public class MiniTornadoProjectile : MonoBehaviour
{
    public float speed = 6f;
    public float lifetime = 4f;
    private Vector2 direction;

    void Start()
    {
        Destroy(gameObject, lifetime); // Auto-destroy after X seconds
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Warrior"))
        {
            Destroy(gameObject);
            return;
        }

        int layer = collision.gameObject.layer;
        if (LayerMask.LayerToName(layer) == "Ground" || LayerMask.LayerToName(layer) == "Wall")
        {
            Destroy(gameObject);
        }
    }
}
