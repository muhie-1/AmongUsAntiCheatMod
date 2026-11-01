using AntiCheat.Core;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine($"PASS: {name}");
    checks++;
}
MovementDetector Detector() => new() { GraceSeconds = 1, RequiredStrikes = 3 };
var normal = Detector();
for (int i = 0; i < 100; i++)
    Check(normal.Observe(1, i, 0, i * 0.5, 3, false) == null, $"normal movement {i}");
var fast = Detector();
fast.Observe(1, 0, 0, 0, 3, false);
Check(fast.Observe(1, 100, 0, 0.5, 3, false) == null, "spawn grace");
Check(fast.Observe(1, 200, 0, 1, 3, false) == null, "first strike");
Check(fast.Observe(1, 300, 0, 1.5, 3, false) == null, "second strike");
Check(fast.Observe(1, 400, 0, 2, 3, false) != null, "sustained speed alert");
Check(fast.Observe(1, 500, 0, 2.5, 3, false) == null, "alert cooldown");
var exempt = Detector();
for (int i = 0; i < 10; i++)
    Check(exempt.Observe(1, i * 100, 0, i * 0.5, 3, true) == null, "vent/transition exemption");
Check(exempt.Observe(1, 1200, 0, 5, 3, false) == null, "grace after transition");
Check(exempt.Observe(1, 2000, 0, 20, 3, false) == null, "network gap resets baseline");
Check(exempt.Observe(1, double.NaN, 0, 21, 3, false) == null, "invalid positions reset baseline");
var recovery = Detector();
recovery.Observe(1, 0, 0, 0, 3, false);
recovery.Observe(1, 100, 0, 1, 3, false);
recovery.Observe(1, 101, 0, 1.5, 3, false);
Check(recovery.Observe(1, 200, 0, 2, 3, false) == null, "legal sample clears strikes");
recovery.Clear();
Check(recovery.Observe(1, 900, 0, 3, 3, false) == null, "new round resets state");
Check(recovery.Observe(2, 900, 0, 3, 3, false) == null, "separate player baseline");
Console.WriteLine($"All {checks} checks passed.");
