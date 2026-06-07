using UnityEngine;
using System.Collections;

public class SpearProjectile : MonoBehaviour
{
    public float travelTime = 0.3f;

    public void Launch(Vector2 start, Vector2 end)
    {
        StartCoroutine(MoveStraight(start, end));
    }

    public void LaunchParabola(Vector2 start, Vector2 end, float arcHeight, float travelTime = 0.5f)
    {
        StartCoroutine(MoveParabola(start, end, arcHeight, travelTime));
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("[SpearProjectile] Hit " + collision.gameObject.name);

        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") ||
            collision.CompareTag("Warrior"))
        {
            Destroy(gameObject); // 💥 Destroy on ground or Warrior hit
        }
    }

    private IEnumerator MoveParabola(Vector2 start, Vector2 end, float arcHeight, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float height = Mathf.Sin(Mathf.PI * t) * arcHeight;

            Vector2 midPoint = Vector2.Lerp(start, end, t);
            Vector2 newPos = new Vector2(midPoint.x, midPoint.y + height);

            Vector2 dir = newPos - (Vector2)transform.position;
            if (dir != Vector2.zero)
                transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            transform.position = newPos;
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = end;
        Destroy(gameObject); // 🧨 Destroy after reaching destination
    }

    private IEnumerator MoveStraight(Vector2 start, Vector2 end)
    {
        float elapsed = 0f;
        float startTime = Time.time;
        Vector2 lastPos = start;

        while (elapsed < travelTime)
        {
            elapsed = Time.time - startTime;
            float t = Mathf.Clamp01(elapsed / travelTime);

            Vector2 newPos = Vector2.Lerp(start, end, t);
            Vector2 dir = newPos - lastPos;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            transform.position = newPos;
            lastPos = newPos;

            yield return null;
        }

        transform.position = end;
        Destroy(gameObject); // 🧨 Destroy after reaching destination
    }
}
