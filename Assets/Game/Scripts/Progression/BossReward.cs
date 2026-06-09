using UnityEngine;

// Watches a boss and, once it is defeated (its GameObject destroyed or
// deactivated after having been active), grants the marquee ability and marks
// the zone cleared. Decoupled from boss internals: it only observes the boss
// reference, so no boss script needs editing. Place this on an always-active
// object in the boss room and assign the boss GameObject.
public class BossReward : MonoBehaviour
{
    [Header("Boss to watch")]
    public GameObject boss;

    [Header("Reward")]
    public bool grantAbility = true;
    public AbilityId rewardAbility;
    public int zoneToClear;

    private bool bossWasAlive = false;
    private bool rewarded = false;

    void Start()
    {
        bossWasAlive = boss != null && boss.activeInHierarchy;
    }

    void Update()
    {
        if (rewarded) return;

        bool bossGone = boss == null || !boss.activeInHierarchy;

        // Mark the boss as having been alive once it activates (BossRoomTrigger
        // enables it when the player enters the room).
        if (!bossWasAlive && !bossGone)
        {
            bossWasAlive = true;
            return;
        }

        // The boss was alive and is now gone: defeated.
        if (bossWasAlive && bossGone)
            GrantReward();
    }

    private void GrantReward()
    {
        rewarded = true;
        var pp = PlayerProgress.Instance;
        if (pp != null)
        {
            if (grantAbility) pp.Unlock(rewardAbility);
            pp.SetZoneCleared(zoneToClear);
        }
        Debug.Log($"[BossReward] Boss defeated. " +
                  $"{(grantAbility ? "Granted " + rewardAbility : "No ability")}, zone {zoneToClear} cleared.");
    }
}
