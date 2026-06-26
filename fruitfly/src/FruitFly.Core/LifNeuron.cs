namespace FruitFly;

// A single Leaky Integrate-and-Fire neuron.
public class LifNeuron
{
    // State — the only thing that changes while the sim runs.
    public double V { get; private set; }    // membrane voltage (mV); the neuron's whole "mind" at any instant

    // Parameters — fixed personality, set once at construction (init-only). Same equation for all neurons.
    public double VRest { get; init; } = -65;       // resting level (mV); V drifts back here when there's no input
    public double VThreshold { get; init; } = -50;  // fires when V crosses this (mV); ~15 mV above rest, so inputs must add up
    public double VReset { get; init; } = -70;      // V slammed here right after a spike (mV); below rest = brief "cool-down"
    public double Tau { get; init; } = 10;          // time constant (ms); how fast V responds and leaks
    public double R { get; init; } = 1;             // membrane resistance; pinned to 1 so input I is already in voltage units

    public LifNeuron()
    {
        V = VRest;                           // a neuron is born at rest
    }

    // Advance the neuron by one time step of length dt (ms), given input current I.
    // Returns true if the neuron fired (spiked) this step.
    public bool Step(double I, double dt)
    {
        // Euler step: nudge V a fraction (dt/Tau) of the way toward where the
        // leak (-(V - VRest)) and the input (R * I) are pushing it.
        V += (dt / Tau) * (-(V - VRest) + R * I);

        if (V >= VThreshold)                 // crossed the firing line?
        {
            V = VReset;                      // spike: drop straight to the reset level
            return true;
        }

        return false;                        // no spike this step
    }
}
