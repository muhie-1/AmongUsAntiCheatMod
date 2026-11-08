using AntiCheat.Core;

internal static class MeetingSabotageChecks
{
    public static int Run()
    {
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new Exception(name);
            Console.WriteLine($"PASS: {name}");
            checks++;
        }

        const ulong reactor = 1UL << 0;
        const ulong oxygen = 1UL << 1;
        const ulong lights = 1UL << 2;
        const ulong highBitSystem = 1UL << 63;

        var tracker = new MeetingSabotageTracker();
        Check(tracker.Observe(reactor, true, false) == 0,
            "first sabotage sample establishes baseline");
        Check(tracker.Observe(reactor, true, true) == 0,
            "sabotage active before a meeting produces no notice");
        Check(tracker.Observe(reactor | oxygen, true, true) == oxygen,
            "activation during a meeting identifies only the newly active system");
        Check(tracker.Observe(reactor | oxygen, true, true) == 0,
            "continuing meeting sabotage does not repeat notices");
        Check(tracker.Observe(oxygen, true, true) == 0,
            "a repair during a meeting produces no notice");
        Check(tracker.Observe(reactor | oxygen, true, true) == reactor,
            "a repaired system activating again during a meeting produces a notice");

        tracker.Clear();
        Check(tracker.Observe(reactor | oxygen, true, true) == 0,
            "joining with a meeting in progress establishes a silent baseline");
        Check(tracker.Observe(reactor | oxygen | lights, true, true) == lights,
            "a new system after joining a meeting produces a notice");

        Check(tracker.Observe(reactor | oxygen | lights, false, true) == 0,
            "outside-game observations produce no notices and clear state");
        Check(tracker.Observe(reactor, true, true) == 0,
            "the first sample in a new game establishes a fresh baseline");

        tracker.Clear();
        Check(tracker.Observe(0, true, false) == 0,
            "empty in-game state establishes a baseline");
        Check(tracker.Observe(reactor | oxygen | highBitSystem, true, true) ==
              (reactor | oxygen | highBitSystem),
            "simultaneous activations retain every system including the highest bit");
        Check(tracker.Observe(0, true, false) == 0,
            "all systems repairing outside a meeting updates the baseline");
        Check(tracker.Observe(lights, true, false) == 0,
            "sabotage starting outside a meeting produces no notice");
        Check(tracker.Observe(lights, true, true) == 0,
            "outside-meeting activation is not re-reported when the meeting starts");
        Check(tracker.Observe(0, true, true) == 0,
            "all systems repairing during a meeting produces no notice");
        Check(tracker.Observe(oxygen, true, true) == oxygen,
            "meeting repair updates the baseline for subsequent activation");

        tracker.Clear();
        Check(tracker.Observe(oxygen, true, true) == 0,
            "explicit clear discards the previous game's baseline");
        return checks;
    }
}
