using System.IO;
using UnityEngine;

// Reads and writes the single JSON save file in persistentDataPath.
// A missing or corrupt file is treated as "no save" (fresh run), never a crash.
public static class SaveSystem
{
    private const string FileName = "savegame.json";

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    // Serialize the given data to disk as pretty JSON. Returns true on success.
    public static bool Save(SaveData data)
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Failed to save: {e.Message}");
            return false;
        }
    }

    // Load save data from disk. Returns null when no valid save exists.
    public static SaveData Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Failed to load (starting fresh): {e.Message}");
            return null;
        }
    }

    // Delete the save file (used by New Game). Safe when the file is absent.
    public static void Delete()
    {
        try { if (File.Exists(FilePath)) File.Delete(FilePath); }
        catch (System.Exception e) { Debug.LogWarning($"[SaveSystem] Failed to delete: {e.Message}"); }
    }

    // True when a save file exists on disk (drives the Continue option).
    public static bool HasSave() => File.Exists(FilePath);
}
