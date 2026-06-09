using UnityEngine;
using UnityEngine.SceneManagement;

// A doorway from the hub into a zone (or any scene-to-scene transition). Loads
// the target scene when the player enters, but only if its unlock condition is
// met. Assign lockedVisual to show a barrier while the portal is locked.
[RequireComponent(typeof(Collider2D))]
public class ZonePortal : MonoBehaviour
{
    public enum UnlockCondition { AlwaysOpen, RequiresZoneCleared, RequiresAbility }

    [Header("Destination")]
    public string targetScene;

    [Header("Lock")]
    public UnlockCondition condition = UnlockCondition.AlwaysOpen;
    public int requiredZoneCleared;    // used when condition = RequiresZoneCleared
    public AbilityId requiredAbility;  // used when condition = RequiresAbility
    public GameObject lockedVisual;    // optional barrier shown while locked

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    // Keep the locked barrier in sync with the current unlock state.
    void Update()
    {
        if (lockedVisual != null) lockedVisual.SetActive(!IsOpen());
    }

    // Whether the portal currently allows passage.
    private bool IsOpen()
    {
        var pp = PlayerProgress.Instance;
        switch (condition)
        {
            case UnlockCondition.RequiresZoneCleared:
                return pp != null && pp.IsZoneCleared(requiredZoneCleared);
            case UnlockCondition.RequiresAbility:
                return pp == null || pp.Has(requiredAbility);
            default:
                return true;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Warrior") || !IsOpen()) return;
        if (string.IsNullOrEmpty(targetScene)) return;
        SceneManager.LoadScene(targetScene);
    }
}
