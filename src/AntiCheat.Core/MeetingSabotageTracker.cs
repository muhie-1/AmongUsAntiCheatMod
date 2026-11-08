namespace AntiCheat.Core;

/// <summary>
/// Tracks locally observed sabotage state. Notices identify systems, never players.
/// </summary>
public sealed class MeetingSabotageTracker
{
    private ulong previousActiveSystems;
    private bool hasBaseline;

    public void Clear()
    {
        previousActiveSystems = 0;
        hasBaseline = false;
    }

    /// <summary>
    /// Returns bits for systems first observed becoming active during a meeting.
    /// The first in-game sample only establishes a baseline, even in a meeting.
    /// </summary>
    public ulong Observe(ulong activeSystems, bool inGame, bool meetingActive)
    {
        if (!inGame)
        {
            Clear();
            return 0;
        }

        var newlyActive = hasBaseline ? activeSystems & ~previousActiveSystems : 0UL;
        previousActiveSystems = activeSystems;
        hasBaseline = true;
        return meetingActive ? newlyActive : 0UL;
    }
}
