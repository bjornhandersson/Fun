using System.Numerics;
using FruitFly.Living;

namespace FruitFly;

// Proof that the fly is a real, Godot-free creature: we run it with NO window, drive it toward a
// corner so it rams the walls, and assert two things behaviourally —
//   1. it actually MOVES (the brain+body run headless), and
//   2. it never stays STUCK for long (the memory-escape works whenever it does jam).
// This is the payoff of ADR 0005: the fly's behaviour can be *asserted*, not just eyeballed.
internal static class FlyHeadlessCheck
{
    public static bool Run()
    {
        var world = new World(new Vector2(800f, 600f), new Random(1)); // banana sits in the top-right corner
        var fly = new Fly(new Vector2(640f, 220f)); // released in the open; it will seek into the corner

        const double dt = 1.0 / 60.0; // simulate at 60 "frames" per second
        const int steps = 3000; // ~50 seconds of fly-life

        double pathLength = 0;
        Vector2 prev = fly.Position;
        int streak = 0,
            maxStuckStreak = 0,
            collisions = 0;

        for (int i = 0; i < steps; i++)
        {
            fly.Step(world, dt);
            pathLength += Vector2.Distance(fly.Position, prev);
            prev = fly.Position;

            if (fly.Colliding)
            {
                collisions++;
                streak++;
                maxStuckStreak = Math.Max(maxStuckStreak, streak);
            }
            else
            {
                streak = 0;
            }
        }

        bool moved = pathLength > 100; // it's alive and steering, not frozen
        bool neverStuckLong = maxStuckStreak < 240; // never jammed > ~4 s → the memory frees it
        bool pass = moved && neverStuckLong;

        Console.WriteLine(
            $"[fly headless]  path={pathLength:0}px  collisions={collisions}  "
                + $"longestStuck={maxStuckStreak} steps  →  {(pass ? "PASS" : "FAIL")}"
        );
        return pass;
    }
}
