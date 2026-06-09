using UnityEngine;
using System.Collections;

public class PurpleWizardStun : MonoBehaviour, IStunnable
{
    [Header("Stun Settings")]
    public float stunDuration = 1.2f;
    public float stunCooldown = 4f;

    [Header("Stun Icon")]
public GameObject stunIconPrefab;
public Vector3 iconOffset = new Vector3(0.8f, 1.2f, 0f);

    private GameObject stunIconInstance;



    private float lastStunTime = -Mathf.Infinity;
    private bool isStunned = false;

    private Animator animator;
    private SpriteRenderer sr;


    void Awake()
{
    animator = GetComponent<Animator>();
    sr = GetComponent<SpriteRenderer>();

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
        StartCoroutine(HandleStun());
    }

    private IEnumerator HandleStun()
{
    isStunned = true;

    if (animator != null)
        animator.Play("Hit");

    if (stunIconInstance != null)
        stunIconInstance.SetActive(true);

    yield return new WaitForSeconds(stunDuration);

    isStunned = false;

    if (stunIconInstance != null)
        stunIconInstance.SetActive(false);
}


    public void RegisterParry()
    {
        Debug.Log("[WizardStunHandler] Parry registered - triggering stun.");
        Stun(stunDuration);
    }

    public bool IsStunned()
    {
        return isStunned;
    }
    void Update()
{
    
    if (stunIconInstance != null && isStunned)
        {
            Vector3 bob = Vector3.up * Mathf.Sin(Time.time * 2f) * 0.1f;
            stunIconInstance.transform.position = transform.position + iconOffset + bob;
        }
}



    public bool IsInParryCooldown => Time.time - lastStunTime < stunCooldown;
}
