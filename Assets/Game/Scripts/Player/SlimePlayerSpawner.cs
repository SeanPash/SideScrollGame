using UnityEngine;
using System.Collections;

public class SlimePlayerSpawner : MonoBehaviour
{
    public GameObject playerPrefab;
    public Transform spawnPoint;

    private IEnumerator Start()
    {
        yield return null;

        GameObject existingPlayer = GameObject.FindWithTag("Warrior");

        if (existingPlayer != null)
        {
            Debug.Log("Warrior already exists - using existing player.");

            if (spawnPoint != null)
            {
                existingPlayer.transform.position = spawnPoint.position;
                existingPlayer.transform.localScale = new Vector3(2f, 2f, 1f);
            }
            else
            {
                Debug.LogWarning("Spawn point is missing!");
            }

            WarriorController warriorController = existingPlayer.GetComponent<WarriorController>();
            if (warriorController != null)
            {
                warriorController.isControlEnabled = true;
                Debug.Log("Control re-enabled for existing Warrior.");
            }

            SlimeBossRoomTrigger bossTrigger = Object.FindFirstObjectByType<SlimeBossRoomTrigger>();
            if (bossTrigger != null)
            {
                bossTrigger.AssignPlayer(existingPlayer);
            }
            else
            {
                Debug.LogWarning("SlimeBossRoomTrigger not found!");
            }

            yield break;
        }

        // no existing player - spawn new one
        if (playerPrefab != null && spawnPoint != null)
        {
            GameObject newPlayer = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);
            newPlayer.name = "Warrior";
            newPlayer.transform.localScale = new Vector3(2f, 2f, 1f);

            WarriorController warriorController = newPlayer.GetComponent<WarriorController>();
            if (warriorController != null)
            {
                warriorController.isControlEnabled = true;
                Debug.Log("Control enabled for new Warrior.");
            }

            SlimeBossRoomTrigger bossTrigger = Object.FindFirstObjectByType<SlimeBossRoomTrigger>();
            if (bossTrigger != null)
            {
                bossTrigger.AssignPlayer(newPlayer);
            }
            else
            {
                Debug.LogWarning("SlimeBossRoomTrigger not found!");
            }
        }
        else
        {
            Debug.LogError("Player prefab or spawn point is missing!");
        }
    }
}
