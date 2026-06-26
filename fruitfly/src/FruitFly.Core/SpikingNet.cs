namespace FruitFly;

// A DATA-ORIENTED spiking network, built to hold hundreds of thousands of neurons.
//
// Why this exists alongside the small `Network` class: `Network` stores each neuron as its
// own object in a List and looks synapses up through a Dictionary. That is perfect for a
// 4-neuron teaching circuit, but at 200,000 neurons it allocates a fortune and chases
// pointers all over memory. So here we flip the layout inside-out:
//
//   * STRUCT OF ARRAYS (SoA): there is no "Neuron" object. A neuron is just an index i, and
//     all of neuron i's data lives at slot i of plain flat arrays (V[i], Bias[i], ...). The
//     CPU loves this — one tight loop streaming straight through contiguous memory.
//
//   * CSR SPARSE SYNAPSES: 200k neurons can't use a 200k x 200k connection matrix (that's
//     40 billion cells, almost all zero). Instead we store only the synapses that exist, in
//     "compressed sparse row" form grouped by source neuron (explained at Build()).
//
//   * EVENT-DRIVEN: each tick we only touch synapses whose SOURCE actually fired. A silent
//     neuron costs nothing on the synapse side.
//
// The neuron math is IDENTICAL to LifNeuron — same leaky-integrate-and-fire Euler step. We
// changed the bookkeeping, not the biology.
public class SpikingNet
{
    public readonly int N;                      // number of neurons

    // ---- Shared LIF parameters (one personality for the whole brain) ----------------
    // Same meaning as LifNeuron's fields. Shared scalars, not per-neuron arrays, because all
    // 200k neurons obey the same equation — what makes them differ is Bias[] below.
    public double VRest = -65, VThreshold = -50, VReset = -70, Tau = 10, R = 1;

    // ---- Per-neuron state and inputs (the struct-of-arrays) --------------------------
    public readonly double[] V;                 // membrane voltage of every neuron
    public readonly double[] Bias;              // fixed per-neuron excitability offset (current units).
                                                //   Heterogeneity is ESSENTIAL: identical neurons given
                                                //   identical input would spike in lockstep, and the whole
                                                //   population would behave like one neuron. Spreading Bias
                                                //   staggers them, so the FRACTION of the pool firing rises
                                                //   smoothly with drive — that smooth fraction IS the rate code.
    public readonly double[] External;          // external input current you inject (sensory drive, tonic)
    private readonly double[] _synIn;           // synaptic current arriving THIS tick (loaded last tick)
    public readonly bool[] Fired;               // did neuron i fire on the most recent tick?

    // ---- Synapses, first gathered loosely, then frozen into CSR ----------------------
    private readonly List<int> _src = new();    // temporary edge lists, used only while wiring up
    private readonly List<int> _dst = new();
    private readonly List<double> _w = new();
    private int[] _rowStart = Array.Empty<int>();   // CSR: where source i's synapses begin
    private int[] _target = Array.Empty<int>();     // CSR: target neuron of each synapse
    private double[] _weight = Array.Empty<double>(); // CSR: weight of each synapse
    public int SynapseCount { get; private set; }

    public SpikingNet(int n)
    {
        N = n;
        V = new double[n];
        Bias = new double[n];
        External = new double[n];
        _synIn = new double[n];
        Fired = new bool[n];
        for (int i = 0; i < n; i++) V[i] = VRest;   // every neuron is born at rest
    }

    // Add one synapse source -> target with a weight (+ excitatory, - inhibitory). Cheap to
    // call hundreds of thousands of times; the real cost is paid once in Build().
    public void Connect(int source, int target, double weight)
    {
        _src.Add(source); _dst.Add(target); _w.Add(weight);
    }

    // Freeze the loose edge list into CSR (compressed sparse row), grouped by source.
    //
    // The whole trick of CSR: lay every synapse end-to-end in `_target`/`_weight`, sorted so
    // that all of source 0's synapses come first, then all of source 1's, and so on. Then a
    // single extra array `_rowStart` records where each source's run begins. So source i owns
    // the slice _target[_rowStart[i] .. _rowStart[i+1]] — found in O(1), no search, no
    // dictionary. When neuron i fires we walk exactly that slice and nothing else.
    public void Build()
    {
        int m = _src.Count;
        SynapseCount = m;
        _rowStart = new int[N + 1];
        _target = new int[m];
        _weight = new double[m];

        // Pass 1: count how many synapses leave each source (a histogram).
        for (int k = 0; k < m; k++) _rowStart[_src[k] + 1]++;
        // Turn counts into start offsets by a running sum: row i now begins where row i-1 ended.
        for (int i = 0; i < N; i++) _rowStart[i + 1] += _rowStart[i];
        // Pass 2: drop each synapse into its source's slice, advancing a per-source cursor.
        int[] cursor = (int[])_rowStart.Clone();
        for (int k = 0; k < m; k++)
        {
            int s = _src[k];
            int at = cursor[s]++;
            _target[at] = _dst[k];
            _weight[at] = _w[k];
        }
        _src.Clear(); _dst.Clear(); _w.Clear();   // edge lists no longer needed; free them
    }

    // Advance the WHOLE network by one tick of length dt (ms). Two phases, exactly mirroring
    // the small Network: read frozen last-tick synaptic input and integrate every neuron;
    // then scatter this tick's spikes forward so they arrive NEXT tick (the one-tick delay).
    public void Step(double dt)
    {
        double a = dt / Tau;
        // Phase 1 — INTEGRATE. One flat streaming loop over all neurons.
        for (int i = 0; i < N; i++)
        {
            double I = External[i] + Bias[i] + _synIn[i];      // everything pushing this neuron now
            double v = V[i] + a * (-(V[i] - VRest) + R * I);   // same Euler step as LifNeuron
            if (v >= VThreshold) { V[i] = VReset; Fired[i] = true; }
            else                 { V[i] = v;      Fired[i] = false; }
        }

        // Phase 2 — SCATTER. Clear the inbox, then for each neuron that fired, deposit its
        // weight into every target's inbox. Those deposits are what Phase 1 reads next tick.
        Array.Clear(_synIn, 0, N);
        for (int i = 0; i < N; i++)
        {
            if (!Fired[i]) continue;                           // event-driven: silent neurons cost nothing
            int end = _rowStart[i + 1];
            for (int k = _rowStart[i]; k < end; k++)
                _synIn[_target[k]] += _weight[k];
        }
    }

    // How many neurons in the index range [start, start+count) fired this tick? This is how a
    // viewer reads a POPULATION's firing RATE: spikes-per-tick across the pool, the population
    // analogue of one neuron's rate. Steering will come from the DIFFERENCE of two of these.
    public int CountFired(int start, int count)
    {
        int c = 0, end = start + count;
        for (int i = start; i < end; i++) if (Fired[i]) c++;
        return c;
    }
}
