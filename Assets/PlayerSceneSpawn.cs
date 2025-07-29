using UnityEngine;

public class PlayerSceneSpawn : MonoBehaviour
{
    public GameObject playerPrefab;
    public Transform spawnPoint;

    void Start()
    {
        // If a Warrior already exists in the scene (carried from a previous scene), don't spawn a new one
        if (GameObject.FindWithTag("Warrior") != null)
        {
            Debug.Log("Warrior already exists — skipping spawn.");
            return;
        }

        if (playerPrefab != null && spawnPoint != null)
        {
            GameObject newPlayer = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);
            newPlayer.transform.localScale = new Vector3(2f, 2f, 1f);

            // Optional: Rename for clarity in Hierarchy
            newPlayer.name = "Warrior";

            // Hook up to BossRoomTrigger if present
            BossRoomTrigger bossTrigger = Object.FindFirstObjectByType<BossRoomTrigger>();
            if (bossTrigger != null)
            {
                bossTrigger.AssignPlayer(newPlayer);
            }
            else
            {
                Debug.LogWarning("BossRoomTrigger not found in scene!");
            }
        }
    }
}
