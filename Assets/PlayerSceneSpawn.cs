using UnityEngine;

public class PlayerSceneSpawn : MonoBehaviour
{
    public GameObject playerPrefab;
    public Transform spawnPoint;

    void Start()
    {
        GameObject existingPlayer = GameObject.FindWithTag("Warrior");

        if (existingPlayer != null)
        {
            Debug.Log("Warrior already exists — using existing player.");
            existingPlayer.transform.position = spawnPoint.position;
            existingPlayer.transform.localScale = new Vector3(2f, 2f, 1f);
            existingPlayer.SetActive(true);

            // ✅ Always re-enable control
            WarriorController warriorController = existingPlayer.GetComponent<WarriorController>();
            if (warriorController != null)
            {
                warriorController.isControlEnabled = true;
                Debug.Log("Control re-enabled for existing Warrior.");
            }

            AssignToBossTrigger(existingPlayer);
            return;
        }

        // Spawn new Warrior
        if (playerPrefab != null && spawnPoint != null)
        {
            GameObject newPlayer = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);
            newPlayer.name = "Warrior";
            newPlayer.transform.localScale = new Vector3(2f, 2f, 1f);

            // ✅ Re-enable control
            WarriorController warriorController = newPlayer.GetComponent<WarriorController>();
            if (warriorController != null)
            {
                warriorController.isControlEnabled = true;
                Debug.Log("Control enabled for new Warrior.");
            }

            AssignToBossTrigger(newPlayer);
        }
        else
        {
            Debug.LogWarning("Player prefab or spawnPoint is null!");
        }
    }

    void AssignToBossTrigger(GameObject player)
    {
        BossRoomTrigger crabTrigger = Object.FindFirstObjectByType<BossRoomTrigger>();
        if (crabTrigger != null)
        {
            crabTrigger.AssignPlayer(player);
        }

        MartialBossTriggerRoom martialTrigger = Object.FindFirstObjectByType<MartialBossTriggerRoom>();
        if (martialTrigger != null)
        {
            martialTrigger.AssignPlayer(player);
        }
    }
}
