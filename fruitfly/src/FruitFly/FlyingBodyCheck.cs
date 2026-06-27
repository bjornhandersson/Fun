using FruitFly.Living; // the FlyingBody lives here; this is just a thin viewer of it

namespace FruitFly;

// Plan 0008: the wingbeat holds the body up. We knock a body DOWNWARD (start falling at 150 px/s)
// twice — once with a FIXED beat (it keeps falling, can't hover) and once with the REFLEX (it
// senses the drop, beats harder, arrests the fall, and settles into a hover). The hold emerges from
// the loop, nothing scripted.
internal static class FlyingBodyCheck
{
    public static bool Run()
    {
        bool offDiverged = RunOne("fixed beat — no reflex", reflex: false);
        Console.WriteLine();
        bool onDiverged = RunOne("with reflex — senses the drop, beats harder", reflex: true);

        bool pass = offDiverged && !onDiverged; // fixed falls away; reflex holds
        Console.WriteLine(
            $"[altitude]  → {(pass ? "PASS — the reflex makes a hover emerge" : "FAIL")}"
        );
        return pass;
    }

    // Returns true if the body did NOT hold (crashed through the floor or never settled).
    private static bool RunOne(string label, bool reflex)
    {
        var body = new FlyingBody(startAltitude: 300.0, reflex: reflex, startVelocity: -150.0);

        const double dt = 1.0 / 60.0;
        const double totalS = 4.0;
        double sinceSample = 0.0;

        Console.WriteLine($"[altitude]  {label}  (start 300px, knocked down at 150px/s):");
        for (double t = 0.0; t < totalS; t += dt)
        {
            body.Step(dt);
            sinceSample += dt;
            if (sinceSample >= 0.2)
            {
                sinceSample = 0.0;
                Console.WriteLine(AltRow(body.Altitude));
            }
        }

        bool crashed = body.Altitude < 20.0;
        bool stillMoving = Math.Abs(body.VerticalVelocity) > 40.0;
        bool diverged = crashed || stillMoving;
        Console.WriteLine(
            $"            end {body.Altitude:0}px, vel {body.VerticalVelocity:+0;-0}px/s  →  "
                + $"{(diverged ? "did NOT hold" : "HOLDING a hover")}"
        );
        return diverged;
    }

    // Altitude as a horizontal bar: longer = higher. Falling shrinks it; a hover holds it steady.
    private static string AltRow(double alt)
    {
        const int w = 40;
        int n = (int)Math.Round(Math.Clamp(alt, 0.0, 400.0) / 400.0 * w);
        return new string('#', n).PadRight(w) + $"  {alt,5:0}px";
    }
}
