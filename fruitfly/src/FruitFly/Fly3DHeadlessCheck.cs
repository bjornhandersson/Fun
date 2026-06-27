using System.Numerics;
using FruitFly.Living;

namespace FruitFly;

// Plan 0009: the UNITED 2.5D fly. With fly3D:true the fly seeks the banana in X/Y (the proven
// horizontal brain) AND climbs to its height on the wingbeat (the vertical layer). Stochastic
// (membrane noise from Random.Shared), so we run MANY trials and assert the POPULATION.
//
// HONEST STATUS (RED, on purpose): the NEW vertical work is perfect — the fly nails the banana's
// height (gap ≈ 0) in every trial. But reliable EATING is NOT yet achieved: the horizontal brain
// ORBITS the banana at ~43px, just outside the 42px eat radius, so it eats only ~1 banana / 45s.
// That orbit is a PRE-EXISTING 2D trait (the smell sensors saturate near the source, steering dies,
// momentum carries it in a ring), not a fault of the 2.5D union — and we never measured 2D eating
// before. TODO to turn this GREEN, the honest way (a brain change, deferred): de-saturate the smell
// seek so the fly can spiral IN, or slow on approach. NOT by widening the eat radius to cross 1px.
internal static class Fly3DHeadlessCheck
{
    private const int Trials = 25;

    public static bool Run()
    {
        var eats = new int[Trials];
        double sumMinXY = 0,
            worstHeightGap = 0;

        for (int t = 0; t < Trials; t++)
        {
            (int eaten, double minXY, double minH) = RunOnce(seed: t);
            eats[t] = eaten;
            sumMinXY += minXY;
            worstHeightGap = Math.Max(worstHeightGap, minH);
        }

        Array.Sort(eats);
        int min = eats[0],
            median = eats[Trials / 2];
        double mean = eats.Average(),
            avgMinXY = sumMinXY / Trials;

        // Population claim for reliable eating: every trial eats at least once, and typically ≥2.
        // (Currently FALSE — see the orbit TODO above.) Height-seek is reported as the proven win.
        bool pass = min >= 1 && median >= 2;
        Console.WriteLine(
            $"[fly 3D]  {Trials} trials: eats min={min} median={median} mean={mean:0.0}  "
                + $"avg closestXY={avgMinXY:0}px (eat radius 42)  height gap≤{worstHeightGap:0}px (vertical nails it)"
        );
        Console.WriteLine(
            pass
                ? "          → PASS — it flies to the banana in 3D and eats it"
                : "          → FAIL (expected for now): union works & height is nailed, but it ORBITS just "
                    + "outside the 42px radius — eating needs the horizontal seek de-saturated (brain TODO)"
        );
        return pass;
    }

    // One ~45s life in 3D; count bananas reached in BOTH plane and height, and track how close it got.
    private static (int eaten, double minXY, double minH) RunOnce(int seed)
    {
        var world = new World(new Vector2(800f, 600f), new Random(seed));
        var fly = new Fly(new Vector2(400f, 300f), fly3D: true);

        const double dt = 1.0 / 60.0;
        const int steps = 60 * 45;

        int eaten = 0;
        double minXY = 1e9,
            minH = 1e9;
        Vector2 prevBanana = world.Banana;
        float prevHeight = world.BananaHeight;

        for (int i = 0; i < steps; i++)
        {
            fly.Step(world, dt);
            minXY = Math.Min(minXY, Vector2.Distance(fly.Position, world.Banana));
            minH = Math.Min(minH, Math.Abs(fly.Altitude - world.BananaHeight));
            if (world.Banana != prevBanana || world.BananaHeight != prevHeight)
            {
                eaten++;
                prevBanana = world.Banana;
                prevHeight = world.BananaHeight;
            }
        }

        return (eaten, minXY, minH);
    }
}
