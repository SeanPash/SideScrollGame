using UnityEngine;
using UnityEngine.SceneManagement;

// Central helper for respawn, new game, and continue. Keeps scene-loading
// concerns out of PlayerHealth and the menu.
public static class GameFlow
{
    private static string pendingSpawnId;
    private static bool repositionHooked;

    // Reload the checkpoint scene and place the player at the saved spawn point.
    // With no checkpoint, just reloads the current scene.
    public static void RespawnAtCheckpoint()
    {
        var pp = PlayerProgress.Instance;
        string scene = SceneManager.GetActiveScene().name;
        string spawnId = "";

        if (pp != null && !string.IsNullOrEmpty(pp.CheckpointScene))
        {
            scene = pp.CheckpointScene;
            spawnId = pp.CheckpointSpawnId;
        }

        pendingSpawnId = spawnId;
        if (!repositionHooked)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            repositionHooked = true;
        }
        SceneManager.LoadScene(scene);
    }

    // After the scene loads, move the player to the spawn point and revive it.
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrEmpty(pendingSpawnId)) return;

        var player = GameObject.FindGameObjectWithTag("Warrior");
        var spawn = SpawnPoint.Find(pendingSpawnId);
        if (player != null && spawn != null)
            player.transform.position = spawn.transform.position;

        if (player != null)
        {
            var health = player.GetComponent<PlayerHealth>();
            if (health != null) health.ReviveFull();
        }
        pendingSpawnId = null;
    }
}
