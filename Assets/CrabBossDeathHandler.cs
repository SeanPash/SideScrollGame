    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;


    public class CrabBossDeathHandler : MonoBehaviour
{
    [Header("Prefabs and Transforms")]
    public GameObject redLinePrefab;
    public GameObject fallingBallPrefab;
    public Transform[] verticalZones; // Hardcoded: ZoneLeft, ZoneMiddle, ZoneRight

    private Animator animator;
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private string currentAnim = "";



    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void TriggerDeathSequence()
    {
        Debug.Log("[CrabDeathHandler] TriggerDeathSequence called");

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        CleanUpProjectilesByLayer();

        PlayAnimation("Crab_Ability");
        yield return new WaitForSeconds(0.6f);

        PlayAnimation("Crab_Death");
        yield return new WaitForSeconds(0.6f);

        GetComponent<SpriteRenderer>().enabled = false;

        yield return StartCoroutine(HardcodedMemorySequence());

        yield return new WaitForSeconds(0.5f);

        Destroy(gameObject);
    }




    private IEnumerator HardcodedMemorySequence()
    {
        List<int[]> hardcodedSteps = new List<int[]>();

        // Generate 5 steps: 1 or 2 danger zones, randomly chosen
        List<int> lastDangerIndices = new List<int>();

        for (int i = 0; i < 5; i++)
        {
            int attempts = 0;
            int[] dangerArray;

            do
            {
                List<int> danger = new List<int> { 0, 1, 2 };
                int safeCount = Random.value < 0.2f ? 2 : 1; // mostly 1 safe

                for (int j = 0; j < safeCount; j++)
                    danger.RemoveAt(Random.Range(0, danger.Count)); // remove safe zones

                dangerArray = danger.ToArray();
                attempts++;

                // Repeat until different enough from last pattern or timeout
            } while (Enumerable.SequenceEqual(dangerArray, lastDangerIndices.ToArray()) && attempts < 10);

            hardcodedSteps.Add(dangerArray);
            lastDangerIndices = new List<int>(dangerArray); // track last pattern
        }


        // Preview danger zones
        foreach (int[] dangerIndices in hardcodedSteps)
        {
            List<GameObject> redLines = new List<GameObject>();

            foreach (int index in dangerIndices)
            {
                Vector3 pos = verticalZones[index].position;
                GameObject redLine = Instantiate(redLinePrefab, pos, Quaternion.identity);
                redLine.transform.localScale = new Vector3(6f, 30f, 1f);
                spawnedObjects.Add(redLine);
                redLines.Add(redLine);
            }

            yield return new WaitForSeconds(1.5f);

            foreach (GameObject rl in redLines)
                if (rl != null) Destroy(rl);
        }

        // Delay before firing projectiles
        yield return new WaitForSeconds(.6f);

        // Fire falling balls at the same positions
        foreach (int[] dangerIndices in hardcodedSteps)
        {
            foreach (int index in dangerIndices)
            {
                Vector3 spawnPos = verticalZones[index].position + Vector3.up * 6f;
                GameObject ball = Instantiate(fallingBallPrefab, spawnPos, Quaternion.identity);
                spawnedObjects.Add(ball);

                Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
                rb.linearVelocity = new Vector2(0f, -5f);
            }

            yield return new WaitForSeconds(3f);
        }
    }
    private void PlayAnimation(string animName)
    {
        if (currentAnim != animName)
        {
            animator.Play(animName, -1, 0f);
            currentAnim = animName;
            Debug.Log("[CrabBossDeathHandler] Playing animation: " + animName);
        }
    }
    private void CleanUpProjectilesByLayer()
    {
        string[] redLineNames = {
            "RedLine",
            "RedLine(Clone)",
            "RedLineStatic",
            "RedLineStatic(Clone)",
            "RedLineVerticalShot(Clone)",
            "RedLineHorizontalShot(Clone)"
        };

foreach (GameObject obj in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            foreach (string name in redLineNames)
            {
                if (obj.name == name)
                {
                    Destroy(obj);
                    break; // skip to next object after match
                }
            }
        }
    }


}
