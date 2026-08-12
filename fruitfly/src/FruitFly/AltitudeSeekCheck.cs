using FruitFly.Living;

namespace FruitFly;

// Plan 0008+: vertical chemotaxis. A FlyingBody starts low, a food source sits HIGH; the body should
// climb the odour gradient to the food's height and hold there (hover reflex). Then we move the food
// DOWN and it should follow. Proof the climb is real seeking, not a scripted setpoint.
internal static class AltitudeSeekCheck
{
    public static bool Run()
    {
        var body = new FlyingBody(startAltitude: 120.0, reflex: true);

        double reached = Phase(body, "food HIGH at 460px — climb to it", target: 460.0);
        double followed = Phase(body, "food moved LOW to 200px — follow it down", target: 200.0);

        bool climbed = Math.Abs(reached - 460.0) < 65.0; // within a chemotaxis "capture radius"
        bool descended = Math.Abs(followed - 200.0) < 65.0;
        bool pass = climbed && descended;
        Console.WriteLine($"[alt-seek]  reached {reached:0}/460, then {followed:0}/200  →  {(pass ? "PASS — it flies to the food" : "FAIL")}");
        return pass;
    }

    // Seek `target` for 6 s, printing height every 0.5 s; return the settled altitude.
    private static double Phase(FlyingBody body, string label, double target)
    {
        body.SeekAltitude(target);
        const double dt = 1.0 / 60.0;
        double sinceSample = 0.0;

        Console.WriteLine($"[alt-seek]  {label}:");
        for (double t = 0.0; t < 8.0; t += dt)
        {
            body.Step(dt);
            sinceSample += dt;
            if (sinceSample >= 0.5)
            {
                sinceSample = 0.0;
                Console.WriteLine(Row(body.Altitude, target));
            }
        }
        return body.Altitude;
    }

    // Body height '#', the food's height marked 'F', on a 0..550px scale.
    private static string Row(double alt, double target)
    {
        const int w = 46;
        var line = new char[w];
        Array.Fill(line, ' ');
        int a = (int)Math.Round(Math.Clamp(alt, 0.0, 550.0) / 550.0 * (w - 1));
        int f = (int)Math.Round(Math.Clamp(target, 0.0, 550.0) / 550.0 * (w - 1));
        line[f] = 'F';
        line[a] = '#';
        return new string(line) + $"  {alt,4:0}px";
    }
}
