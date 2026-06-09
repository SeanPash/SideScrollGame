// Interface for boss health scripts read by the phase manager.
public interface IBossHealth
{
    // Current health as a 0..1 fraction of max health.
    float HealthPercent { get; }

    // True once the death sequence has started.
    bool IsDead { get; }

    // While invulnerable, TakeDamage is ignored (used for benched or hidden bosses).
    void SetInvulnerable(bool value);
}
