using UnityEngine;
using System.Collections;

// Drives the dual-boss encounter: picks a random starter, swaps bosses at 50% HP,
// activates both for the final phase, and ends the encounter when both are dead.
// Started by SlimeBossRoomTrigger.StartEncounter, never at scene load.
public class SlimeBossPhaseManager : MonoBehaviour
{
    // Encounter phases in order.
    private enum Phase { NotStarted, FirstBoss, Swapping, SecondBoss, Final, Complete }

    [Header("References")]
    public GameObject slimeBossObject;
    public GameObject tornadoBossObject;
    public SlimeBossRoomTrigger roomTrigger;

    [Header("Health Bars")]
    public BossHealthBar slimeBossHealthBar;
    public BossHealthBar tornadoBossHealthBar;

    [Header("Timing")]
    public float swapDelay = 2f;

    private Phase phase = Phase.NotStarted;

    // First/second assignment, decided randomly at encounter start.
    private GameObject firstObject;
    private GameObject secondObject;
    private IBoss firstBoss;
    private IBoss secondBoss;
    private IBossHealth firstHealth;
    private IBossHealth secondHealth;
    private BossHealthBar firstBar;
    private BossHealthBar secondBar;

    // Protect both bosses from damage before the encounter starts, and hide the bars.
    void Awake()
    {
        if (slimeBossObject != null)
        {
            IBossHealth h = slimeBossObject.GetComponent<IBossHealth>();
            if (h != null) h.SetInvulnerable(true);
        }
        if (tornadoBossObject != null)
        {
            IBossHealth h = tornadoBossObject.GetComponent<IBossHealth>();
            if (h != null) h.SetInvulnerable(true);
        }
        if (slimeBossHealthBar != null) slimeBossHealthBar.Hide();
        if (tornadoBossHealthBar != null) tornadoBossHealthBar.Hide();
    }

    // Called by the room trigger once the player is locked in. Picks a random
    // starter, activates it, and hides the other boss.
    public void StartEncounter()
    {
        if (phase != Phase.NotStarted) return;

        bool slimeFirst = Random.value < 0.5f;
        firstObject = slimeFirst ? slimeBossObject : tornadoBossObject;
        secondObject = slimeFirst ? tornadoBossObject : slimeBossObject;
        firstBar = slimeFirst ? slimeBossHealthBar : tornadoBossHealthBar;
        secondBar = slimeFirst ? tornadoBossHealthBar : slimeBossHealthBar;

        firstBoss = firstObject.GetComponent<IBoss>();
        secondBoss = secondObject.GetComponent<IBoss>();
        firstHealth = firstObject.GetComponent<IBossHealth>();
        secondHealth = secondObject.GetComponent<IBossHealth>();

        // Hide and protect the second boss until it enters the arena.
        secondObject.SetActive(false);
        secondHealth.SetInvulnerable(true);

        firstHealth.SetInvulnerable(false);
        firstBoss.ActivateBoss();
        if (firstBar != null) firstBar.Show();
        if (secondBar != null) secondBar.Hide();

        phase = Phase.FirstBoss;
        Debug.Log("[PhaseManager] Encounter started. First boss: " + firstObject.name);
    }

    void Update()
    {
        if (phase == Phase.NotStarted || phase == Phase.Complete) return;

        UpdateHealthBars();

        switch (phase)
        {
            case Phase.FirstBoss:
                // Swap when the first boss is half dead (or dies outright).
                if (BossGone(firstObject, firstHealth) || firstHealth.HealthPercent <= 0.5f)
                    StartCoroutine(SwapToSecondBoss());
                break;

            case Phase.SecondBoss:
                // Final phase when the second boss is half dead (or dies outright).
                if (BossGone(secondObject, secondHealth) || secondHealth.HealthPercent <= 0.5f)
                    EnterFinalPhase();
                break;

            case Phase.Final:
                if (BossGone(firstObject, firstHealth) && BossGone(secondObject, secondHealth))
                    CompleteEncounter();
                break;
        }
    }

    // Benches the first boss and brings in the second after a short delay.
    private IEnumerator SwapToSecondBoss()
    {
        phase = Phase.Swapping;
        Debug.Log("[PhaseManager] Swapping to second boss.");

        if (!BossGone(firstObject, firstHealth))
        {
            firstBoss.EnterInactiveState();
            firstHealth.SetInvulnerable(true);
        }
        if (firstBar != null) firstBar.Hide();

        yield return new WaitForSeconds(swapDelay);

        secondObject.SetActive(true);
        secondHealth.SetInvulnerable(false);
        secondBoss.ActivateBoss();
        if (secondBar != null) secondBar.Show();

        phase = Phase.SecondBoss;
    }

    // Reactivates the benched boss and enables aggressive behavior on both.
    private void EnterFinalPhase()
    {
        Debug.Log("[PhaseManager] Final phase: both bosses active.");

        if (!BossGone(firstObject, firstHealth))
        {
            firstHealth.SetInvulnerable(false);
            firstBoss.Reactivate();
            firstBoss.EnableDoublePhase();
            if (firstBar != null) firstBar.Show();
        }
        if (!BossGone(secondObject, secondHealth))
        {
            secondBoss.EnableDoublePhase();
        }

        phase = Phase.Final;
    }

    // Both bosses are dead: hide the bars and unlock the arena.
    private void CompleteEncounter()
    {
        phase = Phase.Complete;
        Debug.Log("[PhaseManager] Encounter complete.");
        if (firstBar != null) firstBar.Hide();
        if (secondBar != null) secondBar.Hide();
        if (roomTrigger != null) roomTrigger.OnEncounterComplete();
    }

    // Pushes current health fractions to the visible bars; hides a dead boss's bar.
    private void UpdateHealthBars()
    {
        if (firstBar != null)
        {
            if (BossGone(firstObject, firstHealth)) firstBar.Hide();
            else firstBar.SetPercent(firstHealth.HealthPercent);
        }
        if (secondBar != null)
        {
            if (BossGone(secondObject, secondHealth)) secondBar.Hide();
            else secondBar.SetPercent(secondHealth.HealthPercent);
        }
    }

    // True when a boss is dead or its object has been destroyed. The GameObject
    // reference is used for the destroyed check because Unity's overloaded null
    // does not apply to interface references.
    private bool BossGone(GameObject obj, IBossHealth health)
    {
        return obj == null || health == null || health.IsDead;
    }
}
