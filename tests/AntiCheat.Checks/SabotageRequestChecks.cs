using AntiCheat.Core;

internal static class SabotageRequestChecks
{
    public static int Run()
    {
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new Exception(name);
            checks++;
        }

        // Every critical/utility sabotage supported by the stock maps can be requested
        // through the root Sabotage system. Doors and arbitrary system IDs cannot.
        byte[] targets = { 3, 7, 8, 14, 21, 57, 58 };
        for (int target = 0; target <= byte.MaxValue; target++)
        {
            bool expected = Array.IndexOf(targets, (byte)target) >= 0;
            Check(SabotageRequestPolicy.IsSabotageStart(17, (byte)target) == expected,
                $"root sabotage classification for target {target}");
        }

        byte[] flaggedSystems = { 3, 7, 8, 14, 21, 58 };
        byte[] startOperations = { 0x80, 0x81, 0x9F, 0xFF };
        byte[] repairOperations = { 0, 1, 2, 3, 4, 0x10, 0x11, 0x20, 0x21, 0x40, 0x41, 0x7F };
        foreach (byte system in flaggedSystems)
        {
            // Direct UpdateSystem calls must not bypass protection by avoiding the root command.
            foreach (byte operation in startOperations)
                Check(SabotageRequestPolicy.IsSabotageStart(system, operation),
                    $"direct start for system {system}, operation {operation}");

            foreach (byte operation in repairOperations)
                Check(!SabotageRequestPolicy.IsSabotageStart(system, operation),
                    $"allow repair/console operation {operation} on system {system}");
        }

        Check(SabotageRequestPolicy.IsSabotageStart(57, 1), "Fungle mixup trigger");
        foreach (byte operation in new byte[] { 0, 2, 0x10, 0x20, 0x40, 0x80, 0xFF })
            Check(!SabotageRequestPolicy.IsSabotageStart(57, operation),
                $"ignore non-trigger mixup operation {operation}");

        foreach (byte system in new byte[] { 0, 10, 16, 37, 48, 59, 0xFF })
        {
            Check(!SabotageRequestPolicy.IsSabotageStart(system, 0x80),
                $"ignore doors/unrelated/unknown system {system}");
            Check(!SabotageRequestPolicy.IsSabotageStart(system, 1),
                $"ignore low operation on unrelated system {system}");
        }

        return checks;
    }
}
