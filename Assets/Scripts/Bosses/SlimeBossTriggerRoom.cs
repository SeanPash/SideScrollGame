using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class SlimeBossRoomTrigger : MonoBehaviour
{
    public CinemachineCamera slimeRoomCamera;
    private GameObject player;

    public Transform moveTarget;
    public float moveSpeed = 3f;
    public float delayBeforeControl = 2f;

    [Header("Wall Blockers")]
    public GameObject wallBlockerTop;
    public GameObject wallBlockerBottom;

    private bool hasTriggered = false;
    private WarriorController warriorController;

    private bool isInitialized = false;
    public SlimeBossBehavior slimeBoss;

    IEnumerator Start()
    {
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

        // Disable wall blockers at start
        if (wallBlockerTop != null) wallBlockerTop.SetActive(false);
        if (wallBlockerBottom != null) wallBlockerBottom.SetActive(false);

        isInitialized = true;
        Debug.Log("Player Found: " + player.name);
        Debug.Log("WarriorController from Trigger: " + warriorController);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized || hasTriggered || !other.CompareTag("Warrior")) return;

        hasTriggered = true;
        slimeRoomCamera.Priority = 20;
        warriorController.isControlEnabled = false;
        StartCoroutine(MovePlayerAndResumeControl());
    }

    private IEnumerator MovePlayerAndResumeControl()
    {
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        Vector3 target = moveTarget.position;
        target.y = player.transform.position.y; // Flatten movement to X only

        // Disable player control
        warriorController.isControlEnabled = false;

        // Stop movement
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Animator anim = player.GetComponent<Animator>();

        // Face the right direction
        float scaleX = target.x > player.transform.position.x
            ? Mathf.Abs(player.transform.localScale.x)
            : -Mathf.Abs(player.transform.localScale.x);
        player.transform.localScale = new Vector3(scaleX, player.transform.localScale.y, player.transform.localScale.z);

        // Run animation
        if (anim != null && anim.enabled && player.activeInHierarchy)
            anim.Play("Run");

        float threshold = 0.05f;
        float floorY = player.transform.position.y;

        while (Mathf.Abs(player.transform.position.x - target.x) > threshold)
        {
            Vector3 direction = (target - player.transform.position).normalized;
            Vector3 newPos = player.transform.position + direction * moveSpeed * Time.deltaTime;
            newPos.y = floorY;
            player.transform.position = newPos;
            yield return null;
        }

        // Snap into place
        player.transform.position = target;

        // Idle animation
        if (anim != null && anim.enabled && player.activeInHierarchy)
            anim.Play("Idle");

        // Activate wall blockers
        if (wallBlockerTop != null) wallBlockerTop.SetActive(true);
        if (wallBlockerBottom != null) wallBlockerBottom.SetActive(true);
        Debug.Log("[SlimeBossRoom] Wall blockers activated.");

        Debug.Log("[SlimeBossRoom] Boss intro complete. Activating boss...");
        yield return new WaitForSeconds(delayBeforeControl);

        if (slimeBoss != null)
        {
            slimeBoss.ActivateBoss();
            Debug.Log("[SlimeBossRoom] Slime Boss AI activated.");
        }

        warriorController.isControlEnabled = true;
    }

    public void AssignPlayer(GameObject spawnedPlayer)
    {
        player = spawnedPlayer;
        warriorController = player.GetComponent<WarriorController>();
    }
}
