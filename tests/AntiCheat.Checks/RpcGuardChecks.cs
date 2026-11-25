using AntiCheat.Core;

internal static class RpcGuardChecks
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

        foreach (byte id in new byte[] { 2, 3, 4, 6, 8, 12, 14, 22, 23, 44, 45, 46, 52, 56, 63, 65 })
            Check(RpcGuardPolicy.IsHostOnly(id), $"{RpcGuardPolicy.Describe(id)} is host-only");
        // Client requests and self-owned actions must pass untouched.
        foreach (byte id in new byte[] { 0, 1, 5, 7, 11, 13, 19, 20, 24, 31, 33, 47, 48, 49, 51, 55, 62, 64, 67, 200 })
            Check(!RpcGuardPolicy.IsHostOnly(id), $"RPC {id} is allowed from clients");
        Check(RpcGuardPolicy.Describe(12) == "MurderPlayer", "names murder RPC");
        Check(RpcGuardPolicy.Describe(201) == "RPC 201", "names unknown RPC by id");

        var rate = new RpcRateTracker();
        for (int i = 0; i < 5; i++)
            Check(rate.Observe(1, i * 0.1, 5) == 0, $"within limit {i}");
        Check(rate.Observe(1, 0.6, 5) == 6, "flood reported with count");
        Check(rate.Observe(1, 0.7, 5) == 0, "flood reported once per window");
        Check(rate.Observe(2, 0.7, 5) == 0, "separate keys counted separately");
        Check(rate.Observe(1, 1.6, 5) == 0, "new window resets count");
        Check(rate.Observe(1, 0.5, 5) == 0, "clock going backwards starts a new window");
        Check(rate.Observe(1, 0.6, 0) == 0, "zero limit disables flood checks");
        rate.Clear();
        Check(rate.Observe(1, 5, 1) == 0, "clear resets windows");
        return checks;
    }
}
