using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class MartialBossTriggerRoom : MonoBehaviour
{
    public CinemachineCamera MartialHeroRoomCamera;
    private GameObject player;

    public Transform moveTarget;
    public float moveSpeed = 3f;
    public float delayBeforeControl = 2f;

    [Header("Wall Spawner")]
    public GameObject invisibleWalls;

    private bool hasTriggered = false;
    private WarriorController warriorController;

    private bool isInitialized = false;
    public MartialHeroBehavior martialHero;
    

    IEnumerator Start()
    {
        // Wait for Warrior to be spawned
        if (player == null)
        {
            while (player == null)
            {
                player = GameObject.FindWithTag("Warrior");
                yield return null;
            }
        }

        warriorController = player.GetComponent<WarriorController>();
        if (warriorController == null)
        {
            Debug.LogError("WarriorController not found on player!");
            yield break;
        }

        // Make sure invisible walls are off at start
        if (invisibleWalls != null)
            invisibleWalls.SetActive(false);

        isInitialized = true;
        Debug.Log("Player Found: " + player.name);
        Debug.Log("WarriorController from Trigger: " + warriorController);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized || hasTriggered || !other.CompareTag("Warrior")) return;

        hasTriggered = true;
        MartialHeroRoomCamera.Priority = 20;
        warriorController.isControlEnabled = false;
        StartCoroutine(MovePlayerAndResumeControl());
    }

    private IEnumerator MovePlayerAndResumeControl()
    {
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        Vector3 target = moveTarget.position;

        // Disable player control
        warriorController.isControlEnabled = false;

        // Stop previous movement
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Animator anim = player.GetComponent<Animator>();

        // Face correct direction
        float scaleX = target.x > player.transform.position.x
            ? Mathf.Abs(player.transform.localScale.x)
            : -Mathf.Abs(player.transform.localScale.x);
        player.transform.localScale = new Vector3(scaleX, player.transform.localScale.y, player.transform.localScale.z);

        // Play "Run" animation
        if (anim != null && anim.enabled && player.activeInHierarchy)
            anim.Play("Run");

        float threshold = 0.05f;
        float floorY = player.transform.position.y;

        // Move manually
        while (Vector2.Distance(player.transform.position, target) > threshold)
        {
            Vector3 direction = (target - player.transform.position).normalized;
            Vector3 newPos = player.transform.position + direction * moveSpeed * Time.deltaTime;
            newPos.y = floorY;
            player.transform.position = newPos;
            yield return null;
        }

        // Snap to final position
        player.transform.position = target;

        // Play "Idle" after reaching destination
        if (anim != null && anim.enabled && player.activeInHierarchy)
            anim.Play("Idle");

        // ✅ Activate invisible walls
        if (invisibleWalls != null)
        {
            invisibleWalls.SetActive(true);
            Debug.Log("[BossRoom] Invisible walls activated.");
        }

        Debug.Log("Boss: Prepare yourself!");
        yield return new WaitForSeconds(delayBeforeControl);

        if (martialHero != null)
        {
            martialHero.ActivateBoss();
            Debug.Log("CrabBoss AI activated after dialogue.");
        }

        // Re-enable player control
        warriorController.isControlEnabled = true;
    }

    public void AssignPlayer(GameObject spawnedPlayer)
    {
        player = spawnedPlayer;
        warriorController = player.GetComponent<WarriorController>();
    }
}
