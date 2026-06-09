using UnityEngine;

// A trigger volume that teaches the player something on entry.
//  - grantAbility = true: unlock the chosen ability and show a teaching prompt.
//  - grantAbility = false: hint only (for always-on moves like basic movement).
// Fires once, then disables itself so it cannot retrigger.
[RequireComponent(typeof(Collider2D))]
public class AbilityGrantTrigger : MonoBehaviour
{
    [Header("What this trigger does")]
    public bool grantAbility = true;
    public AbilityId ability;

    [Header("Prompt")]
    [TextArea] public string promptText = "Press a key";
    public float promptDuration = 3f;

    private bool fired = false;

    // Default the collider to a trigger when first added.
    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (fired || !other.CompareTag("Warrior")) return;
        fired = true;

        if (grantAbility && PlayerProgress.Instance != null)
            PlayerProgress.Instance.Unlock(ability);

        ProgressionHud.Show(promptText, promptDuration);
        gameObject.SetActive(false);
    }
}
