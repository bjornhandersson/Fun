namespace FruitFly.Living;

// A body that must FLY to stay up (Plan 0008, 2.5D). One vertical axis: gravity pulls it down, the
// wingbeat (HalfCentreOscillator, Plan 0007) makes LIFT to push it up. This is where the beat does
// real work.
//
// THE RULE (hardened after review): nothing enters the brain except transduced current at a
// receptor's membrane — becoming charge is what a sense organ IS. Past the receptors, only spikes
// through synapses move. There is no command variable, no gain × (a − b) arithmetic, no injected
// modulation: the whole vertical brain is four receptors and four synapses onto the wing cells.
//
// A FIXED beat can only fall or climb (see FlyingBodyCheck's "no reflex" run). A steady hover
// EMERGES from wiring: the descent receptor EXCITES the wing cells (dropping ⇒ beat harder), the
// ascent receptor INHIBITS them (rising ⇒ ease off) — negative feedback, summed where a real fly
// sums it: at the membrane. The baseline beat is the wingbeat's own pacemaker (Plan 0010), sized so
// the wings roughly lift the body; the reflex supplies the correction that actually stabilises it.
// Godot-free (ADR 0005): a viewer just reads Altitude and draws it.
public sealed class FlyingBody
{
    private readonly HalfCentreOscillator _wingbeat = new();

    // Vertical-motion sense: two opponent receptors. _descent fires while the body drops, _ascent
    // while it rises — a stand-in for the optic flow (the world rushing past) and airflow a real fly
    // uses to feel itself moving. They TRANSDUCE vertical velocity into spikes (the body providing a
    // sense organ); everything they cause downstream, they cause through their synapses.
    private readonly LifNeuron _descent = new();
    private readonly LifNeuron _ascent = new();
    private double _descAct,
        _ascAct; // smoothed spike traces — viewer GAUGES only; the brain never reads these

    // --- body + world (the bigger brain provides these) ---
    private const double Gravity = 300.0; // downward acceleration (px/s²)
    private const double Mass = 1.0; // force and acceleration share units while Mass = 1
    private const double LiftGain = 1470.0; // lift per unit vigour; sized so the BASELINE beat ≈ weight
    // (measured: baseline vigour ≈ 0.204, so 1470 × 0.204 ≈ 300 = weight → v=0 is the hover point)
    private const double NeuralStepMs = 1.0; // stable substep for the brain

    // --- the reflex, as WIRING ---
    // descent → wing cells is EXCITATORY, ascent → wing cells is INHIBITORY. The old
    // CmdGain × (descAct − ascAct) subtraction now happens at the wing cells' membranes, where the
    // two synaptic currents sum with opposite signs — no code computes it.
    private const double SenseGain = 0.65; // transduction: vertical speed (px/s) → receptor current
    // (sensitive enough that it keeps correcting down to a gentle drift, not just fast falls)
    private const double ReflexWeight = 27.0; // receptor→wing synapse strength. Sized to hand the
    // wing cells the same AVERAGE current the proven injected reflex did: a synapse kick w decaying
    // over TauSyn=5ms at spike rate f delivers ≈ w·5·f, and the old path delivered
    // 45 × (0.1 × 30 × f) = 135·f — so w = 135/5 = 27. Verified by FlyingBodyCheck.
    private const double ActTauMs = 30.0; // gauge smoothing (viewer only)
    private const double ActKick = 0.1;

    // --- vertical chemotaxis (opt-in): climb the smell of a food source toward its height ---
    // A food source emits an odour that peaks at its altitude. Two smell receptors sit a little
    // ABOVE and BELOW the body: _foodAbove EXCITES the wing cells (food above ⇒ climb), _foodBelow
    // INHIBITS them (food below ⇒ descend). At the food they smell equally, their opposite synaptic
    // currents cancel at the membrane ⇒ no bias ⇒ the hover reflex holds. The vertical twin of the
    // 2D fly's crossed/uncrossed wiring — the comparison IS the wiring.
    private readonly LifNeuron _foodAbove = new();
    private readonly LifNeuron _foodBelow = new();
    private double _aboveAct,
        _belowAct; // viewer gauges only
    private bool _seeking;
    private double _targetAlt;

    private const double SensorSpan = 40.0; // vertical gap between the two smell receptors (px) — wider
    // span samples a bigger odour difference, so the climb stays strong right up to the food
    private const double SmellFalloff = 210.0; // px at which the odour halves (sharp enough near the peak)
    private const double SmellSenseGain = 95.0; // odour (0..1) → receptor current (clears rheobase far out)
    private const double SmellWeight = 80.0; // smell→wing synapse strength. Average-current sizing
    // (old path 285·f ⇒ w = 57) proved too weak on the DESCENT leg — spiky inhibition only bites
    // between wing-cell spikes, and receptor rates compress near the food — so it is tuned up until
    // the body follows the food down as well as up (measured in AltitudeSeekCheck).

    private readonly bool _reflex;
    private double _altitude,
        _velocity,
        _vigor;

    public FlyingBody(double startAltitude = 300.0, bool reflex = true, double startVelocity = 0.0)
    {
        _altitude = startAltitude;
        _velocity = startVelocity;
        _reflex = reflex;

        // The brain is wired HERE, once — the behaviour lives in these four synapses.
        // reflex:false is an ABLATION experiment: the motion receptors still spike, but their
        // nerve to the wings is cut (weight 0) — the "fixed beat" control run in FlyingBodyCheck.
        _wingbeat.AddReceptor(_descent, _reflex ? +ReflexWeight : 0.0);
        _wingbeat.AddReceptor(_ascent, _reflex ? -ReflexWeight : 0.0);
        _wingbeat.AddReceptor(_foodAbove, +SmellWeight);
        _wingbeat.AddReceptor(_foodBelow, -SmellWeight);
    }

    // Turn on (or move) the food source the body climbs toward. Until called, the body just hovers.
    public void SeekAltitude(double targetAltitude)
    {
        _seeking = true;
        _targetAlt = targetAltitude;
    }

    private double Smell(double atAltitude)
    {
        double r = (atAltitude - _targetAlt) / SmellFalloff;
        return 1.0 / (1.0 + r * r); // 1 at the food's height, halving every SmellFalloff
    }

    public void Step(double dtSeconds)
    {
        // 1. TRANSDUCE — the only world→brain doorway: what each receptor's membrane feels.
        //    Vertical motion → the opponent motion receptors; odour → the smell receptors
        //    (silent until there is food to smell: no odour, no current, no spikes).
        double down = Math.Max(0.0, -_velocity);
        double up = Math.Max(0.0, _velocity);
        _wingbeat.SetReceptorInput(_descent, SenseGain * down);
        _wingbeat.SetReceptorInput(_ascent, SenseGain * up);
        _wingbeat.SetReceptorInput(
            _foodAbove,
            _seeking ? SmellSenseGain * Smell(_altitude + SensorSpan) : 0.0
        );
        _wingbeat.SetReceptorInput(
            _foodBelow,
            _seeking ? SmellSenseGain * Smell(_altitude - SensorSpan) : 0.0
        );

        // 2. Run the brain. Hover ("dropping ⇒ beat harder") and climb ("food above ⇒ beat
        //    harder") are not computed anywhere — the four synaptic currents just sum at the
        //    wing cells' membranes, and the beat that comes out is the decision.
        int substeps = Math.Max(1, (int)Math.Round(dtSeconds * 1000.0 / NeuralStepMs));
        double vigorSum = 0.0;
        for (int i = 0; i < substeps; i++)
        {
            _wingbeat.Step(NeuralStepMs);
            vigorSum += _wingbeat.LeftActivity + _wingbeat.RightActivity;

            // Viewer gauges: smoothed spike traces (observation only — nothing feeds back).
            if (_wingbeat.ReceptorFired(_descent))
            {
                _descAct += ActKick;
            }
            if (_wingbeat.ReceptorFired(_ascent))
            {
                _ascAct += ActKick;
            }
            if (_wingbeat.ReceptorFired(_foodAbove))
            {
                _aboveAct += ActKick;
            }
            if (_wingbeat.ReceptorFired(_foodBelow))
            {
                _belowAct += ActKick;
            }
            _descAct += (NeuralStepMs / ActTauMs) * (-_descAct);
            _ascAct += (NeuralStepMs / ActTauMs) * (-_ascAct);
            _aboveAct += (NeuralStepMs / ActTauMs) * (-_aboveAct);
            _belowAct += (NeuralStepMs / ActTauMs) * (-_belowAct);
        }
        _vigor = vigorSum / substeps;

        // 3. Newton on the vertical axis: lift up, gravity down.
        double lift = LiftGain * _vigor;
        double accel = lift / Mass - Gravity;
        _velocity += accel * dtSeconds;
        _altitude += _velocity * dtSeconds;
    }

    // An external shove (impulse): a viewer can knock the body to test the reflex. +up, -down. This
    // is a disturbance ON the body — wind, a flick — NOT a command to the neurons; the reflex still
    // has to recover on its own. (Same spirit as the memory "poke".)
    public void Nudge(double deltaVelocityUp) => _velocity += deltaVelocityUp;

    // ---- Read-only state for viewers ----
    public double Altitude => _altitude;
    public double VerticalVelocity => _velocity;
    public double BeatVigor => _vigor; // how hard the wings are beating ⇒ lift
    public double DescentSense => Math.Clamp(_descAct, 0.0, 1.0); // receptor: "I'm dropping"
    public double AscentSense => Math.Clamp(_ascAct, 0.0, 1.0); // receptor: "I'm rising"
    public double WingPhase => _wingbeat.LeftActivity - _wingbeat.RightActivity; // for drawing the flap
    public bool Seeking => _seeking;
    public double TargetAltitude => _targetAlt; // the food's height (for the viewer)
}
