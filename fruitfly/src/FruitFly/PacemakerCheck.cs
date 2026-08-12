namespace FruitFly;

// Plan 0010, step 1 — ISOLATION PROOF that a COMMAND neuron can fire on its OWN, with no input.
//
// This is the honest source of the "command to fly": not a constant current the designer injects
// into the wingbeat, but a real neuron whose INTRINSIC biophysics never let it rest — a pacemaker.
// The whole trick is ONE init-only parameter: VRest set ABOVE VThreshold. The membrane always
// drifts toward a level that is already past the firing line, so it charges, fires, resets, and
// charges again — forever, with ZERO external input. Real pacemaker/command neurons do exactly
// this (persistent depolarising currents hold them above threshold). It is a PROPERTY of the cell,
// not a current the world pushes in — so the drive now lives INSIDE the graph, as spikes.
internal static class PacemakerCheck
{
    public static void Run()
    {
        // Rests at -40 mV — 10 mV ABOVE its own -50 mV threshold, so it can never settle.
        // (-40 = the old baseline drive expressed as the cell's own rest: -65 rest + 25 drive.)
        var command = new LifNeuron { VRest = -40.0 };

        const double dt = 1.0; // ms per step
        const int steps = 200; // 200 ms observation window
        int spikes = 0;
        int firstSpikeAt = -1;

        for (int t = 0; t < steps; t++)
        {
            bool fired = command.Step(0.0, dt); // 0.0 = NO external input; any spike is intrinsic
            if (fired)
            {
                spikes++;
                if (firstSpikeAt < 0)
                {
                    firstSpikeAt = t;
                }
            }
        }

        double hz = spikes / (steps * dt / 1000.0);
        string verdict =
            spikes > 0
                ? $"PASS — fires on its own at ~{hz:0} Hz (first spike at {firstSpikeAt} ms)"
                : "FAIL — silent (no intrinsic drive)";
        Console.WriteLine(
            $"[pacemaker]  command neuron, ZERO input, {steps} ms  ->  {spikes} spikes  =>  {verdict}"
        );
    }
}
