namespace FruitFly;

internal static class Program
{
    // Visualization scale (mV) for each ASCII panel.
    private const double ScaleLow = -75.0;
    private const double ScaleHigh = -45.0;
    private const int Width = 28;

    private static void Main()
    {
        // Headless proof that the assembled fly runs and behaves without Godot (ADR 0005).
        FlyHeadlessCheck.Run();
        Console.WriteLine();

        // Plan 0009: the united 2.5D fly flies to the banana in X/Y AND height.
        Fly3DHeadlessCheck.Run();
        Console.WriteLine();

        // Rung 1 of the flight roadmap: two neurons making their own rhythm (Plan 0007).
        HalfCentreOscillatorCheck.Run();
        Console.WriteLine();

        // Plan 0008 step 1: gravity + lift from the wingbeat — a fixed beat can't hover (yet).
        FlyingBodyCheck.Run();
        Console.WriteLine();

        // Plan 0008+: vertical chemotaxis — the body climbs the odour gradient to the food's height.
        AltitudeSeekCheck.Run();
        Console.WriteLine();

        // Two neurons wired in a line: A drives B through one synapse.
        var a = new LifNeuron();
        var b = new LifNeuron();
        var synapse = new Synapse(a, b, weight: 160.0); // big: one A-spike alone pushes B over threshold

        const double dt = 1.0; // ms per step
        const double drive = 20.0; // constant input to A (above rheobase → A fires periodically)
        const int steps = 60;

        Console.WriteLine(
            $"Each panel: V from {ScaleLow} to {ScaleHigh} mV;  '|' = threshold,  '*' = V"
        );
        Console.WriteLine(
            "   A (driven by constant input)        B (fed by A through the synapse)"
        );

        for (int step = 0; step < steps; step++)
        {
            bool aFired = a.Step(drive, dt); // 1. run A on its external drive
            double toB = synapse.Step(aFired, dt); // 2. advance the synapse → current for B (now decays over steps)
            bool bFired = b.Step(toB, dt); // 3. run B on whatever the synapse delivered

            Console.WriteLine(
                Panel(a.V, a.VThreshold, aFired) + "   " + Panel(b.V, b.VThreshold, bFired)
            );
        }
    }

    // Render one neuron as a fixed-width panel: '|' threshold, '*' V, trailing SPIKE flag.
    private static string Panel(double v, double vThreshold, bool spiked)
    {
        var line = new char[Width];
        Array.Fill(line, ' ');
        line[Col(vThreshold)] = '|'; // the firing line, for reference
        line[Col(v)] = '*'; // the membrane voltage right now
        return new string(line) + (spiked ? " SPIKE" : "      ");
    }

    // Map a voltage to a column on the scale.
    private static int Col(double v) =>
        Math.Clamp((int)((v - ScaleLow) / (ScaleHigh - ScaleLow) * Width), 0, Width - 1);
}
