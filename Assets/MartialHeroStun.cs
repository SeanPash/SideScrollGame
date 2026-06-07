using UnityEngine;
using System.Collections;

public class MartialHeroStun : MonoBehaviour, IStunnable
{
    public float stunDuration = 1.2f;
    public float stunCooldown = 4f;

    [Header("Stun Icon")]
    public GameObject stunIconPrefab;
    public Vector3 iconOffset = new Vector3(0.8f, 1.2f, 0f);

    private GameObject stunIconInstance;

    private bool isStunned = false;
    private float lastStunTime = -Mathf.Infinity;

    public MartialHeroBehavior behavior;

    void Awake()
    {
        if (stunIconPrefab != null)
        {
            stunIconInstance = Instantiate(stunIconPrefab, transform.position + iconOffset, Quaternion.identity);
            stunIconInstance.SetActive(false);

            var follow = stunIconInstance.GetComponent<StunIconFollow>();
            if (follow != null)
            {
                follow.boss = this.transform;
                follow.offset = iconOffset;
            }
        }

        if (behavior == null)
            behavior = GetComponent<MartialHeroBehavior>();

        if (stunIconPrefab != null)
        {
            stunIconInstance = Instantiate(stunIconPrefab, transform.position + iconOffset, Quaternion.identity);
            stunIconInstance.SetActive(false);

            var follow = stunIconInstance.GetComponent<PurpleStunIconFollow>();
            if (follow != null)
            {
                follow.target = this.transform;
                follow.offset = iconOffset;
            }
        }
    }

    public void Stun(float duration)
    {
        if (Time.time - lastStunTime < stunCooldown || isStunned)
            return;

        lastStunTime = Time.time;
        StartCoroutine(DoStun(duration));
    }

private IEnumerator DoStun(float duration)
{
    isStunned = true;

    behavior.rb.linearVelocity = Vector2.zero;

    if (stunIconInstance != null)
        stunIconInstance.SetActive(true);

    behavior.animator.Play("Parried");

    yield return new WaitForSeconds(duration); // total stun duration

    behavior.animator.Play("Idle");

    isStunned = false;

    if (stunIconInstance != null)
        stunIconInstance.SetActive(false);
}




    public bool IsStunned() => isStunned;

    void Update()
    {
        if (stunIconInstance != null && isStunned)
        {
            Vector3 bob = Vector3.up * Mathf.Sin(Time.time * 2f) * 0.1f;
            stunIconInstance.transform.position = transform.position + iconOffset + bob;
        }
    }

    public void RegisterParry()
    {
        Debug.Log("[MartialHeroStun] Parry registered — triggering stun.");
        Stun(stunDuration);
    }

    public bool IsInParryCooldown => Time.time - lastStunTime < stunCooldown;
}
