using UnityEngine;
using System.Collections;

public class SlimeBossPhaseManager : MonoBehaviour
{
    public SlimeBossBehavior slimeA;
    public SlimeBossBehavior slimeB;

    private SlimeBossBehavior activeBoss;
    private SlimeBossBehavior inactiveBoss;

    private bool hasSwapped = false;
    private bool bothActive = false;

    void Start()
    {
        // Randomly select which boss starts first
        if (Random.value < 0.5f)
        {
            activeBoss = slimeA;
            inactiveBoss = slimeB;
        }
        else
        {
            activeBoss = slimeB;
            inactiveBoss = slimeA;
        }

        // Activate first boss
        activeBoss.gameObject.SetActive(true);
        activeBoss.ActivateBoss();

        // Disable the other one
        inactiveBoss.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!hasSwapped && activeBoss != null && activeBoss.GetHealthPercent() <= 0.5f)
        {
            hasSwapped = true;
            StartCoroutine(SwapToSecondBoss());
        }

        if (hasSwapped && !bothActive && inactiveBoss != null && inactiveBoss.GetHealthPercent() <= 0.5f)
        {
            bothActive = true;
            ActivateBothBosses();
        }
    }

    IEnumerator SwapToSecondBoss()
    {
        Debug.Log("Swapping bosses...");

        activeBoss.EnterInactiveState(); // optional: animation or stun
        yield return new WaitForSeconds(2f);

        inactiveBoss.gameObject.SetActive(true);
        inactiveBoss.ActivateBoss();
    }

    void ActivateBothBosses()
    {
        Debug.Log("Both bosses active!");

        activeBoss.Reactivate();        // Resume behavior of first boss
        inactiveBoss.EnableDoublePhase();  // If second phase logic differs
    }
}
