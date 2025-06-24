using UnityEngine;

public class ParrySystem : MonoBehaviour
{
    public float regularParryCooldown = 4f;
    private float lastParryTime = -Mathf.Infinity;
    private int bossParryCount = 5;

    public int bossParryThreshold = 5;

    public void Parry(GameObject Target)
    {
        if (Time.time - lastParryTime < regularParryCooldown) return;

        if (Target.CompareTag("Enemy"))
        {
            Target.GetComponent<GolemStun>()?.Stun(1f);
            lastParryTime = Time.time;
        }
        else if (Target.CompareTag("Boss"))
        {
            bossParryCount++;
            if (bossParryCount >= bossParryThreshold)
            {
                Target.GetComponent<GolemStun>()?.Stun(1.5f);
                bossParryCount = 0;
            }
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
