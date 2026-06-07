using UnityEngine;

public class RedLineBlink : MonoBehaviour
{
    public float blinkSpeed = 5f; // how fast it blinks
    public float blinkStartDelay = 2f; // start blinking at 2.5s (3s total)
    private SpriteRenderer sr;
    private float spawnTime;

    // Alpha bounds
    private float minAlpha = 0.2f;
    private float maxAlpha = 0.6f;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        spawnTime = Time.time;

        if (sr != null)
        {
            // Keep initial red tone but enforce alpha range
            Color c = sr.color;
            c.a = minAlpha;
            sr.color = c;
        }
    }

    private void Update()
    {
        float timeSinceSpawn = Time.time - spawnTime;

        if (timeSinceSpawn >= blinkStartDelay && sr != null)
        {
            float t = Mathf.PingPong((timeSinceSpawn - blinkStartDelay) * blinkSpeed, 1f);
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}
