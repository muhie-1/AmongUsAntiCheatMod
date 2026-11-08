namespace AntiCheat.Core;

/// <summary>
/// Recognizes sabotage initiation operations without treating repair operations as starts.
/// The caller must separately check the game state and whether it has host authority.
/// </summary>
public static class SabotageRequestPolicy
{
    public static bool IsSabotageStart(byte system, byte amount)
    {
        // Sabotage commands contain the requested target system rather than an operation flag.
        if (system == 17)
            return IsKnownSabotageSystem(amount);

        // Fungle mixup has its own operation enum: None=0, TriggerSabotage=1.
        if (system == 57)
            return amount == 1;

        // Reactor, lights, oxygen, comms, laboratory and heli use the damage/start flag.
        // Console use, releasing consoles and repair/clear operations use lower bits.
        return IsKnownSabotageSystem(system) && (amount & 0x80) != 0;
    }

    private static bool IsKnownSabotageSystem(byte system) => system is
        3 or 7 or 8 or 14 or 21 or 57 or 58;
}
