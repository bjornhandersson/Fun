namespace FruitFly;

// A single Leaky Integrate-and-Fire neuron.
public class LifNeuron
{
    // State — the only thing that changes while the sim runs.
    public double V { get; private set; } // membrane voltage (mV); the neuron's whole "mind" at any instant

    // Parameters — fixed personality, set once at construction (init-only). Same equation for all neurons.
    public double VRest { get; init; } = -65; // resting level (mV); V drifts back here when there's no input
    public double VThreshold { get; init; } = -50; // fires when V crosses this (mV); ~15 mV above rest, so inputs must add up
    public double VReset { get; init; } = -70; // V slammed here right after a spike (mV); below rest = brief "cool-down"
    public double Tau { get; init; } = 10; // time constant (ms); how fast V responds and leaks
    public double R { get; init; } = 1; // membrane resistance; pinned to 1 so input I is already in voltage units

    // Membrane noise: the standard deviation of the random voltage kick the membrane feels
    // each step, in mV per √ms. 0 (default) = the old, perfectly deterministic neuron, so
    // every circuit built so far behaves EXACTLY as before. A small positive value (~1) models
    // the ceaseless jitter of real ion channels + synaptic background — and it's what breaks a
    // left/right tie so the fly never sits balanced, driving straight into a wall.
    public double NoiseSigma { get; init; } = 0;

    public LifNeuron()
    {
        V = VRest; // a neuron is born at rest
    }

    // Advance the neuron by one time step of length dt (ms), given input current I.
    // Returns true if the neuron fired (spiked) this step.
    public bool Step(double I, double dt)
    {
        // Euler step: nudge V a fraction (dt/Tau) of the way toward where the
        // leak (-(V - VRest)) and the input (R * I) are pushing it.
        V += (dt / Tau) * (-(V - VRest) + R * I);

        // Stochastic part: a random push, up or down. It scales with √dt (NOT dt) because
        // independent kicks accumulate like a random walk — halve the step and you take twice
        // as many kicks, but each is 1/√2 as big, so the total wander over a fixed slice of
        // real time is unchanged. (Scaling with dt would secretly shrink the noise on finer steps.)
        if (NoiseSigma != 0)
        {
            V += NoiseSigma * Math.Sqrt(dt) * Gaussian();
        }

        if (V >= VThreshold) // crossed the firing line?
        {
            V = VReset; // spike: drop straight to the reset level
            return true;
        }

        return false; // no spike this step
    }

    // One draw from a standard normal (mean 0, SD 1) via the Box-Muller transform: it turns
    // two uniform randoms into one bell-curved random. Random.Shared hands every call an
    // independent draw, so two neurons NEVER get the same jitter — exactly what we need to
    // break a perfect left/right tie.
    private static double Gaussian()
    {
        double u1 = 1.0 - Random.Shared.NextDouble(); // in (0,1], so Log is always safe
        double u2 = Random.Shared.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
