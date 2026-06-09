using UnityEngine;

// Marks a named location the respawn/checkpoint flow can return the player to.
public class SpawnPoint : MonoBehaviour
{
    public string spawnId = "checkpoint";

    // Find a spawn point by id in the active scene; null if none match.
    public static SpawnPoint Find(string id)
    {
        foreach (var sp in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
            if (sp.spawnId == id) return sp;
        return null;
    }
}
