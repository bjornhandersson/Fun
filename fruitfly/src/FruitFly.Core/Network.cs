namespace FruitFly;

// A network of neurons connected by synapses. It owns the "tick": every Step advances the
// WHOLE network by one time step, updating all neurons SIMULTANEOUSLY from the previous
// tick's signals. That built-in one-tick transmission delay is what makes loops well-defined
// and makes update order irrelevant.
//
// Crucially, it changes NOTHING inside LifNeuron or Synapse — it only conducts them, in two
// phases each tick: (1) read what the synapses are currently delivering and step every
// neuron; (2) advance every synapse from whoever fired this tick, loading what it will
// deliver next tick.
public class Network
{
    private readonly List<LifNeuron> _neurons = new();
    private readonly List<Synapse> _synapses = new();
    private readonly Dictionary<LifNeuron, int> _slot = new();   // neuron -> its index in the buffers below

    // Per-neuron working buffers, indexed by slot. Reused every tick (no per-frame allocation).
    private double[] _external = Array.Empty<double>();   // external input current you inject (e.g. a sensory drive)
    private double[] _input = Array.Empty<double>();      // total current a neuron feels this tick
    private bool[] _fired = Array.Empty<bool>();          // did each neuron fire this tick

    // Register a neuron with the network. Returns it so you can keep a handle.
    public LifNeuron Add(LifNeuron neuron)
    {
        _slot[neuron] = _neurons.Count;
        _neurons.Add(neuron);
        int n = _neurons.Count;
        Array.Resize(ref _external, n);
        Array.Resize(ref _input, n);
        Array.Resize(ref _fired, n);
        return neuron;
    }

    // Wire source -> target with a weight (+ excitatory, - inhibitory). Returns the synapse.
    public Synapse Connect(LifNeuron source, LifNeuron target, double weight)
    {
        var synapse = new Synapse(source, target, weight);
        _synapses.Add(synapse);
        return synapse;
    }

    // Inject (or clear, with 0) a constant external input current into one neuron.
    public void SetInput(LifNeuron neuron, double current) => _external[_slot[neuron]] = current;

    // Did this neuron fire on the most recent tick? (For viewers.)
    public bool Fired(LifNeuron neuron) => _fired[_slot[neuron]];

    // Advance the entire network by one tick of length dt (ms).
    public void Step(double dt)
    {
        // Phase 1 — READ. Each neuron gathers the current its incoming synapses are holding
        // RIGHT NOW (loaded by spikes from the PREVIOUS tick) plus any external input, then
        // integrates and maybe fires. Because every neuron reads the same frozen
        // previous-tick synapse state, the order we loop in cannot change the result.
        for (int i = 0; i < _neurons.Count; i++)
            _input[i] = _external[i];
        foreach (var s in _synapses)
            _input[_slot[s.Target]] += s.Current;       // deliver last tick's current to the target
        for (int i = 0; i < _neurons.Count; i++)
            _fired[i] = _neurons[i].Step(_input[i], dt);

        // Phase 2 — ADVANCE. Each synapse decays and, if its source fired THIS tick, takes a
        // fresh kick — loading the current it will deliver on the NEXT tick. The gap between
        // a source firing now and the target feeling it next tick IS the transmission delay.
        foreach (var s in _synapses)
            s.Step(_fired[_slot[s.Source]], dt);
    }
}
