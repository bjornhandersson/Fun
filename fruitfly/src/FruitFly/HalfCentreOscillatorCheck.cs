using FruitFly.Living; // the HalfCentreOscillator circuit lives here now, not in this file

namespace FruitFly;

// Headless proof that the half-centre oscillator (Plan 0007, rung 1) makes its OWN rhythm on the
// real neural substrate — no timer, no sin(t). PURE VIEWER: it builds NO neurons and wires NO
// synapses; it just Steps a FruitFly.Living.HalfCentreOscillator and watches the two sides take
// turns. (Same role FlyHeadlessCheck plays for the Fly.)
internal static class HalfCentreOscillatorCheck
{
    public static bool Run()
    {
        var cpg = new HalfCentreOscillator();

        const double dt = 0.5; // ms per tick
        const double totalMs = 800.0; // how long to watch
        const double sampleEveryMs = 10.0; // print one trace row this often

        int spikesL = 0;
        int spikesR = 0;
        int switches = 0;
        int leader = 0; // -1 = L ahead, +1 = R ahead, 0 = undecided yet
        double sinceSample = 0.0;

        Console.WriteLine("[half-centre CPG]   L  <——  |  ——>  R     (one rhythm, made by two neurons)");

        for (double t = 0.0; t < totalMs; t += dt)
        {
            cpg.Step(dt); // ONE tick of the real brain — the rhythm is generated in there
            if (cpg.FiredLeft)
            {
                spikesL++;
            }
            if (cpg.FiredRight)
            {
                spikesR++;
            }

            double aL = cpg.LeftActivity;
            double aR = cpg.RightActivity;

            // Count a "switch" when the lead flips, with a deadband so jitter doesn't flicker it.
            int now = aR - aL > 0.15 ? +1 : aL - aR > 0.15 ? -1 : leader;
            if (now != leader && leader != 0)
            {
                switches++;
            }
            if (now != 0)
            {
                leader = now;
            }

            sinceSample += dt;
            if (sinceSample >= sampleEveryMs)
            {
                sinceSample = 0.0;
                Console.WriteLine(Trace(aL, aR));
            }
        }

        bool bothFired = spikesL > 5 && spikesR > 5; // neither side is dead
        bool alternated = switches >= 4; // it genuinely took turns several times
        bool pass = bothFired && alternated;
        Console.WriteLine(
            $"[half-centre CPG]  L-spikes={spikesL}  R-spikes={spikesR}  switches={switches}  →  "
                + $"{(pass ? "PASS — it oscillates" : "FAIL")}"
        );
        return pass;
    }

    // One row of the time trace: L grows LEFT from the centre, R grows RIGHT. Read top-to-bottom,
    // the bar swings side to side — that swing is the beat.
    private static string Trace(double actL, double actR)
    {
        const int half = 20;
        var line = new char[half * 2 + 1];
        Array.Fill(line, ' ');
        line[half] = '|';
        int nL = (int)Math.Round(Math.Clamp(actL, 0.0, 1.0) * half);
        int nR = (int)Math.Round(Math.Clamp(actR, 0.0, 1.0) * half);
        for (int i = 1; i <= nL; i++)
        {
            line[half - i] = '#';
        }
        for (int i = 1; i <= nR; i++)
        {
            line[half + i] = '#';
        }
        return new string(line);
    }
}
