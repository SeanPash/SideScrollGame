// Interface for boss behaviors that participate in the multi-boss phase system.
// The phase manager drives bosses through these calls without knowing concrete types.
public interface IBoss
{
    // Starts the boss AI when the encounter begins or this boss enters the arena.
    void ActivateBoss();

    // Benches the boss: stops AI, timers, and any in-progress attack.
    void EnterInactiveState();

    // Resumes the boss AI after being benched.
    void Reactivate();

    // Enables the more aggressive final-phase behavior.
    void EnableDoublePhase();
}
