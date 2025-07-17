using UnityEngine;
using System.Collections;

public class RegularGolemStun : MonoBehaviour, IStunnable
{
    [Header("Stun Settings")]
    public float stunDuration = 1f;
    public float stunCooldown = 4f;

    [Header("Icon")]
    public GameObject stunIconPrefab;
    public Vector3 iconOffset = new Vector3(0.8f, 1.2f, 0f);

    private GameObject stunIconInstance;
    private float lastStunTime = -Mathf.Infinity;
    private bool isStunned = false;
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();

        if (stunIconPrefab != null)
        {
            stunIconInstance = Instantiate(stunIconPrefab, transform.position + iconOffset, Quaternion.identity);
            stunIconInstance.transform.SetParent(null); // not a child — follows via script
            stunIconInstance.SetActive(false);
        }
    }

    void Update()
    {
        if (stunIconInstance != null && isStunned)
        {
            Vector3 bobbingOffset = Vector3.up * Mathf.Sin(Time.time * 2f) * 0.1f;
            stunIconInstance.transform.position = transform.position + iconOffset + bobbingOffset;
        }
    }

    public void Stun(float duration)
    {
        if (Time.time - lastStunTime < stunCooldown || isStunned)
            return;

        StartCoroutine(HandleStun(duration));
    }

    public void RegisterParry()
    {
        Debug.Log("[GolemStun] Parry registered → applying stun");
        Stun(stunDuration);
    }

    private IEnumerator HandleStun(float duration)
{
    isStunned = true;
    lastStunTime = Time.time;

    if (animator != null)
        animator.Play("Enemy Hit");

    if (stunIconInstance != null)
        stunIconInstance.SetActive(true);

    yield return new WaitForSeconds(duration);

    isStunned = false;

    if (stunIconInstance != null)
        stunIconInstance.SetActive(false);
}



 public bool IsStunned() => isStunned;

    public bool IsInParryCooldown => Time.time - lastStunTime < stunCooldown;
}
