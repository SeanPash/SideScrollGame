using System.Collections.Generic;

// Plain serializable container for everything persisted between sessions.
// JsonUtility serializes public fields and List<>; no properties allowed.
// Abilities are stored by enum name so reordering the enum does not corrupt saves.
[System.Serializable]
public class SaveData
{
    public List<string> unlockedAbilities = new List<string>();
    public List<int> clearedZones = new List<int>();

    // Mirrored upgradeable stats from PlayerStats.
    public int baseDamage = 3;
    public int maxHealth = 100;

    // Last checkpoint: scene name + spawn id within that scene.
    public string checkpointScene = "";
    public string checkpointSpawnId = "";
}
