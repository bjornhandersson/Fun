using System.Numerics;
using FruitFly.Living;

namespace FruitFly;

// Plan 0009: the UNITED 2.5D fly. With fly3D:true the fly seeks the banana in X/Y (the proven
// horizontal brain) AND climbs to its height on the wingbeat (the vertical layer). Headless proof
// that it eats the banana in 3D — i.e. it really arrives in both the plane and the height — and
// repeatedly, as the banana respawns at fresh corners and heights.
internal static class Fly3DHeadlessCheck
{
    public static bool Run()
    {
        var world = new World(new Vector2(800f, 600f), new Random(2));
        var fly = new Fly(new Vector2(400f, 300f), fly3D: true); // released in the middle

        const double dt = 1.0 / 60.0;
        const int steps = 60 * 45; // 45 seconds of fly-life

        int eaten = 0;
        double closestHeightGap = 1e9;
        double closestXY = 1e9;
        Vector2 prevBanana = world.Banana;
        float prevHeight = world.BananaHeight;

        for (int i = 0; i < steps; i++)
        {
            fly.Step(world, dt);

            // An eat = the banana jumped to a new spot/height.
            if (world.Banana != prevBanana || world.BananaHeight != prevHeight)
            {
                eaten++;
                prevBanana = world.Banana;
                prevHeight = world.BananaHeight;
            }
            closestHeightGap = Math.Min(closestHeightGap, Math.Abs(fly.Altitude - world.BananaHeight));
            closestXY = Math.Min(closestXY, Vector2.Distance(fly.Position, world.Banana));
        }

        bool pass = eaten >= 2; // it reached the banana in 3D, more than once
        Console.WriteLine(
            $"[fly 3D]  eaten={eaten}  closest X/Y={closestXY:0}px  closest height gap={closestHeightGap:0}px  "
                + $"final altitude={fly.Altitude:0}px  →  {(pass ? "PASS — it flies to the banana in 3D" : "FAIL")}"
        );
        return pass;
    }
}
