namespace FruitFly;

// A one-way connection from a source neuron to a target neuron.
// The adapter between the two halves of a neuron: it turns the source's spike *event*
// into a chunk of input *current* for the target.
//
// Simplest possible model for now (see plan 0001 for deferred realism):
//   - same-step: the target feels the current on the same step the source fired.
//   - one-step kick: a spike delivers `Weight` for exactly that step, then it's gone.
public class Synapse
{
    public LifNeuron Source { get; init; }   // whose spikes drive this connection
    public LifNeuron Target { get; init; }   // who receives the current
    public double Weight { get; init; }      // current delivered per source spike;
                                             // + = excitatory (push target up), - = inhibitory (push down)

    public Synapse(LifNeuron source, LifNeuron target, double weight)
    {
        Source = source;
        Target = target;
        Weight = weight;
    }

    // The current this synapse delivers to its target this step.
    // `sourceFired` is what Source.Step(...) returned this step — a spike delivers
    // `Weight`, no spike delivers nothing. This single line *is* the event-to-current
    // conversion the whole synapse exists to do.
    public double Current(bool sourceFired) => sourceFired ? Weight : 0.0;
}
