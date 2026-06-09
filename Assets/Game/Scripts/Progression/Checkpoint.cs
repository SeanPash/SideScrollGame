using UnityEngine;
using UnityEngine.SceneManagement;

// A trigger that records the player's last safe spot. On entry it stores this
// scene plus a spawn id in PlayerProgress so respawn can return here. Place a
// matching SpawnPoint (same spawnId) where the player should reappear.
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Must match a SpawnPoint.spawnId in this scene.")]
    public string spawnId = "checkpoint";

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Warrior") || PlayerProgress.Instance == null) return;
        PlayerProgress.Instance.SetCheckpoint(SceneManager.GetActiveScene().name, spawnId);
        Debug.Log($"[Checkpoint] Saved at {spawnId}");
    }
}
