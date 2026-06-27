namespace FruitFly;

// A one-way connection from a source neuron to a target neuron.
// The adapter between two neurons: it turns the source's spike *events* into input
// *current* for the target.
//
// This is the "single-exponential decay" rung of the fidelity ladder (see FOUNDATIONS):
// a presynaptic spike no longer delivers an instantaneous one-step kick. Instead the
// synapse carries its own current that DECAYS over several steps, just like a real
// post-synaptic current. Two spikes that land close in time therefore overlap and add up.
public class Synapse
{
    public LifNeuron Source { get; init; } // whose spikes drive this connection
    public LifNeuron Target { get; init; } // who receives the current
    public double Weight { get; init; } // current added per source spike;

    // + = excitatory (push target up), - = inhibitory (push down)

    // Synaptic time constant (ms): how fast the post-synaptic current decays back toward
    // zero after a spike. ~5 ms is realistic for a fast excitatory synapse. Bigger = the
    // current lingers longer, which widens the window over which inputs can summate.
    public double TauSyn { get; init; } = 5;

    // STATE — the synapse's "memory". The live post-synaptic current it is delivering
    // right now. Persists between steps; born silent at 0.
    public double Current { get; private set; }

    public Synapse(LifNeuron source, LifNeuron target, double weight)
    {
        Source = source;
        Target = target;
        Weight = weight;
    }

    // Advance the synapse by one time step of length dt (ms) and return the current it
    // delivers to its target this step. `sourceFired` is what Source.Step(...) returned.
    public double Step(bool sourceFired, double dt)
    {
        // 1. Leak: nudge the current a fraction (dt/TauSyn) of the way toward zero —
        //    the same Euler-leak shape the neuron uses for V toward VRest. With no new
        //    spikes it fades away on its own.
        Current += (dt / TauSyn) * (-Current);

        // 2. Kick: a presynaptic spike dumps a fresh Weight of current on TOP of whatever
        //    is still lingering. That stacking is exactly how inputs summate over time.
        if (sourceFired)
        {
            Current += Weight;
        }

        return Current;
    }
}
