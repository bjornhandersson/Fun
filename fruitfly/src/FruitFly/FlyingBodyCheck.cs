using FruitFly.Living; // the FlyingBody lives here; this is just a thin viewer of it

namespace FruitFly;

// Plan 0008, step 1: SHOW that a fixed wingbeat cannot hover. We drop a FlyingBody at 300px and
// watch its altitude — with a constant beat, lift ≠ weight, so it sinks (or climbs) and never holds.
// This failure is what motivates the reflex (sense vertical motion → adjust the beat) added next.
internal static class FlyingBodyCheck
{
    public static bool Run()
    {
        var body = new FlyingBody(startAltitude: 300.0);
        double startAlt = body.Altitude;

        const double dt = 1.0 / 60.0;
        const double totalS = 3.0;
        double sinceSample = 0.0;

        Console.WriteLine("[altitude]  fixed beat — bar length = height (start 300px).  Watch it sink:");
        for (double t = 0.0; t < totalS; t += dt)
        {
            body.Step(dt);
            sinceSample += dt;
            if (sinceSample >= 0.15)
            {
                sinceSample = 0.0;
                Console.WriteLine(AltRow(body.Altitude));
            }
        }

        bool diverged = Math.Abs(body.Altitude - startAlt) > 100.0; // it did NOT hold
        Console.WriteLine(
            $"[altitude]  start={startAlt:0}px  end={body.Altitude:0}px  →  "
                + $"{(diverged ? "DIVERGED — a fixed beat can't hold altitude (reflex needed next)" : "held?! (unexpected)")}"
        );
        return diverged;
    }

    // Altitude as a horizontal bar: longer = higher. As the body falls, the bar shrinks.
    private static string AltRow(double alt)
    {
        const int w = 40;
        int n = (int)Math.Round(Math.Clamp(alt, 0.0, 400.0) / 400.0 * w);
        return new string('#', n).PadRight(w) + $"  {alt,5:0}px";
    }
}
