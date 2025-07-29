using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject warriorPrefab;
    public Transform spawnPoint;

    void Start()
    {
        if (GameObject.FindGameObjectWithTag("Warrior") == null)
        {
            Instantiate(warriorPrefab, spawnPoint.position, Quaternion.identity);
        }
    }
}
