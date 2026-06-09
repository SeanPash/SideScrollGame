using System;
using System.Collections.Generic;
using UnityEngine;

// Persistent record of what the player has unlocked and how far they have
// progressed. Sibling to PlayerStats: PlayerStats owns numbers, PlayerProgress
// owns unlocks and run state. Survives scene loads via DontDestroyOnLoad and
// persists between sessions through SaveSystem.
public class PlayerProgress : MonoBehaviour
{
    public static PlayerProgress Instance;

    // Fired whenever a new ability is unlocked (the HUD banner listens to this).
    public event Action<AbilityId> OnUnlock;

    private readonly HashSet<AbilityId> unlocked = new HashSet<AbilityId>();
    private readonly HashSet<int> clearedZones = new HashSet<int>();

    // Last checkpoint, consumed by the respawn flow in GameFlow.
    public string CheckpointScene { get; private set; } = "";
    public string CheckpointSpawnId { get; private set; } = "";

    // Standard singleton setup; load saved progress on first creation.
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadFromDisk();
    }

    // ---- Ability unlock API ----

    // True if the ability has been unlocked.
    public bool Has(AbilityId id) => unlocked.Contains(id);

    // Unlock an ability, announce it, and persist. No-op if already unlocked.
    public void Unlock(AbilityId id)
    {
        if (!unlocked.Add(id)) return;
        Debug.Log($"[PlayerProgress] Unlocked {id}");
        OnUnlock?.Invoke(id);
        SaveToDisk();
    }

    // ---- Zone progress API ----

    public bool IsZoneCleared(int zone) => clearedZones.Contains(zone);

    public void SetZoneCleared(int zone)
    {
        if (clearedZones.Add(zone))
        {
            Debug.Log($"[PlayerProgress] Zone {zone} cleared");
            SaveToDisk();
        }
    }

    // ---- Checkpoint API ----

    public void SetCheckpoint(string scene, string spawnId)
    {
        CheckpointScene = scene;
        CheckpointSpawnId = spawnId;
        SaveToDisk();
    }

    // ---- Persistence ----

    // Snapshot all state (plus mirrored PlayerStats numbers) and write to disk.
    public void SaveToDisk()
    {
        SaveData data = new SaveData();
        foreach (AbilityId id in unlocked) data.unlockedAbilities.Add(id.ToString());
        foreach (int z in clearedZones) data.clearedZones.Add(z);
        data.checkpointScene = CheckpointScene;
        data.checkpointSpawnId = CheckpointSpawnId;
        if (PlayerStats.Instance != null)
        {
            data.baseDamage = PlayerStats.Instance.baseDamage;
            data.maxHealth = PlayerStats.Instance.maxHealth;
        }
        SaveSystem.Save(data);
    }

    // Load state from disk; missing/corrupt save leaves a clean fresh run.
    private void LoadFromDisk()
    {
        SaveData data = SaveSystem.Load();
        if (data == null) return;

        unlocked.Clear();
        foreach (string name in data.unlockedAbilities)
            if (Enum.TryParse(name, out AbilityId id)) unlocked.Add(id);

        clearedZones.Clear();
        foreach (int z in data.clearedZones) clearedZones.Add(z);

        CheckpointScene = data.checkpointScene;
        CheckpointSpawnId = data.checkpointSpawnId;

        // Apply saved upgradeable stats if PlayerStats is already present.
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.baseDamage = data.baseDamage;
            PlayerStats.Instance.maxHealth = data.maxHealth;
        }
    }

    // Wipe everything for a New Game.
    public void ResetProgress()
    {
        unlocked.Clear();
        clearedZones.Clear();
        CheckpointScene = "";
        CheckpointSpawnId = "";
        SaveSystem.Delete();
    }
}
