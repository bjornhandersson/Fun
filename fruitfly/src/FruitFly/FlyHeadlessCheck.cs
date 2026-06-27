using System.Numerics;
using FruitFly.Living;

namespace FruitFly;

// Proof that the fly is a real, Godot-free creature, run with NO window. The fly is STOCHASTIC — its
// membrane noise (essential: it breaks the L/R tie so the fly never freezes against a wall) comes
// from Random.Shared, so every run differs. A single-run pass/fail would be a coin-flip; instead we
// run MANY trials and assert the POPULATION behaves: it always moves, and the memory-escape ALWAYS
// frees it (it never stays permanently jammed), and report how often it escapes quickly.
internal static class FlyHeadlessCheck
{
    private const int Trials = 25;
    private const int QuickEscape = 240; // steps (~4s): a "fast" break-free
    private const int CatastrophicJam = 600; // steps (~10s): longer than this = effectively stuck

    public static bool Run()
    {
        int movedCount = 0,
            quickCount = 0,
            worstJam = 0;

        for (int t = 0; t < Trials; t++)
        {
            (bool moved, int longestStuck) = RunOnce(seed: t);
            if (moved)
            {
                movedCount++;
            }
            if (longestStuck < QuickEscape)
            {
                quickCount++;
            }
            worstJam = Math.Max(worstJam, longestStuck);
        }

        // The real guarantees: it ALWAYS moves, and it ALWAYS eventually frees itself (no permanent
        // jam). "Quick escape" is reported as a quality signal, not asserted — a noisy fly is allowed
        // an occasional slow break-free.
        bool pass = movedCount == Trials && worstJam < CatastrophicJam;
        Console.WriteLine(
            $"[fly headless]  {Trials} trials: moved {movedCount}/{Trials}  escaped<4s {quickCount}/{Trials}  "
                + $"worst jam {worstJam} steps (~{worstJam / 60.0:0.0}s)  →  {(pass ? "PASS" : "FAIL")}"
        );
        return pass;
    }

    // One ~50s life. Drive it into a corner so it rams walls; measure movement and the longest jam.
    private static (bool moved, int longestStuck) RunOnce(int seed)
    {
        var world = new World(new Vector2(800f, 600f), new Random(seed));
        var fly = new Fly(new Vector2(640f, 220f));

        const double dt = 1.0 / 60.0;
        const int steps = 3000;

        double pathLength = 0;
        Vector2 prev = fly.Position;
        int streak = 0,
            maxStuckStreak = 0;

        for (int i = 0; i < steps; i++)
        {
            fly.Step(world, dt);
            pathLength += Vector2.Distance(fly.Position, prev);
            prev = fly.Position;

            if (fly.Colliding)
            {
                streak++;
                maxStuckStreak = Math.Max(maxStuckStreak, streak);
            }
            else
            {
                streak = 0;
            }
        }

        return (pathLength > 100, maxStuckStreak);
    }
}
