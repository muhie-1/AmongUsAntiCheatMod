using System;
using System.Collections.Generic;

namespace AntiCheat.Core;

public sealed class MovementDetector
{
    private sealed class Track
    {
        public double X, Y, Time, GraceUntil;
        public int Strikes;
    }

    private readonly Dictionary<int, Track> tracks = new();
    public double GraceSeconds { get; set; } = 3;
    public double SampleSeconds { get; set; } = 0.5;
    public double MaximumGapSeconds { get; set; } = 2;
    public double PositionSlack { get; set; } = 1.25;
    public int RequiredStrikes { get; set; } = 4;

    public void Clear() => tracks.Clear();
    public void Remove(int player) => tracks.Remove(player);

    // Call with locally observed positions, monotonic seconds, and a speed in world units/sec.
    // Returns one alert after consecutive excessive samples, then starts a grace period.
    public string? Observe(int player, double x, double y, double now,
        double maximumSpeed, bool exempt)
    {
        if (!Finite(x) || !Finite(y) || !Finite(now) || !Finite(maximumSpeed) || maximumSpeed <= 0)
        {
            tracks.Remove(player);
            return null;
        }
        if (!tracks.TryGetValue(player, out var t))
        {
            tracks[player] = new Track { X = x, Y = y, Time = now, GraceUntil = now + GraceSeconds };
            return null;
        }
        var elapsed = now - t.Time;
        if (exempt || elapsed < 0 || elapsed > MaximumGapSeconds)
        {
            t.X = x; t.Y = y; t.Time = now;
            t.GraceUntil = now + GraceSeconds; t.Strikes = 0;
            return null;
        }
        if (elapsed < SampleSeconds) return null;
        var distance = Math.Sqrt((x - t.X) * (x - t.X) + (y - t.Y) * (y - t.Y));
        t.X = x; t.Y = y; t.Time = now;
        if (now < t.GraceUntil) return null;
        var allowed = maximumSpeed * elapsed + PositionSlack;
        t.Strikes = distance > allowed ? t.Strikes + 1 : 0;
        if (t.Strikes < RequiredStrikes) return null;
        t.Strikes = 0;
        t.GraceUntil = now + GraceSeconds;
        return FormattableString.Invariant($"Repeated excessive movement: {distance:F2} units in {elapsed:F2}s; limit {allowed:F2} units.");
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
