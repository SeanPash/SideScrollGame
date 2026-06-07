using UnityEngine;

public class CrabBossBehavior : MonoBehaviour
{
    public CrabBossBehaviorPhase1 phase1;
    public CrabBossBehaviorPhase2 phase2;
    public CrabBossDeathHandler deathHandler;
    private bool isActivated = false;

    private void Start()
    {

        // Start with all logic disabled
        phase1.enabled = false;
        phase2.enabled = false;
        deathHandler.enabled = false;
    }

    // Call this AFTER dialogue to activate the boss
    public void ActivateBoss()
    {
        if (isActivated) return;

        isActivated = true;
        phase1.enabled = true;
        phase1.ActivatePhase();
        Debug.Log("[CrabBoss] Activated!");
    }

}