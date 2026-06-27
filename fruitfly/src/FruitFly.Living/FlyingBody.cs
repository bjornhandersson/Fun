namespace FruitFly.Living;

// A body that must FLY to stay up (Plan 0008, 2.5D). One vertical axis: gravity pulls it down, the
// wingbeat (HalfCentreOscillator, Plan 0007) makes LIFT to push it up. This is where the beat does
// real work.
//
// A FIXED beat can only fall or climb (see FlyingBodyCheck's "no reflex" run). A steady hover
// EMERGES from a reflex: two opponent receptors sense the body's vertical motion, and dropping makes
// it beat HARDER (more lift), rising eases it off — negative feedback. The "hold" lives in that loop,
// not in a hand-set thrust. The baseline beat is sized so the wings roughly lift the body (a fly's
// wings ARE sized to its weight — morphology, the body); the reflex supplies the correction that
// actually stabilises it. Godot-free (ADR 0005): a viewer just reads Altitude and draws it.
public sealed class FlyingBody
{
    private readonly HalfCentreOscillator _wingbeat = new();

    // Vertical-motion sense: two opponent receptors. _descent fires while the body drops, _ascent
    // while it rises — a stand-in for the optic flow (the world rushing past) and airflow a real fly
    // uses to feel itself moving. They TRANSDUCE vertical velocity into spikes (the body providing a
    // sense organ); the correction they drive lives in the wingbeat command (the tiny brain).
    private readonly LifNeuron _descent = new();
    private readonly LifNeuron _ascent = new();
    private double _descAct,
        _ascAct; // smoothed receptor activity — the reflex's signal

    // --- body + world (the bigger brain provides these) ---
    private const double Gravity = 300.0; // downward acceleration (px/s²)
    private const double Mass = 1.0; // force and acceleration share units while Mass = 1
    private const double LiftGain = 1470.0; // lift per unit vigour; sized so the BASELINE beat ≈ weight
    // (measured: baseline vigour ≈ 0.204, so 1470 × 0.204 ≈ 300 = weight → v=0 is the hover point)
    private const double NeuralStepMs = 1.0; // stable substep for the brain

    // --- the reflex ---
    private const double BaselineCommand = 25.0; // steady "fly" command — wings sized to the body
    private const double SenseGain = 0.65; // vertical speed (px/s) → receptor input current (sensitive
    // enough that it keeps correcting down to a gentle drift, not just fast falls)
    private const double CmdGain = 45.0; // how hard the reflex pushes the beat per unit of sensed motion
    private const double ActTauMs = 30.0;
    private const double ActKick = 0.1;

    // --- vertical chemotaxis (opt-in): climb the smell of a food source toward its height ---
    // A food source emits an odour that peaks at its altitude. Two smell receptors sit a little
    // ABOVE and BELOW the body and compare it: food above ⇒ climb, food below ⇒ descend. This is
    // the vertical twin of the 2D fly's banana-seeking — same idea, same honesty (real receptors,
    // climb the gradient). At the food the two smell equally ⇒ no bias ⇒ the hover reflex holds.
    private readonly LifNeuron _foodAbove = new();
    private readonly LifNeuron _foodBelow = new();
    private double _aboveAct,
        _belowAct;
    private bool _seeking;
    private double _targetAlt;

    private const double SensorSpan = 40.0; // vertical gap between the two smell receptors (px) — wider
    // span samples a bigger odour difference, so the climb stays strong right up to the food
    private const double SmellFalloff = 210.0; // px at which the odour halves (sharp enough near the peak)
    private const double SmellSenseGain = 95.0; // odour (0..1) → receptor current (clears rheobase far out)
    private const double ClimbGain = 95.0; // how hard the smell difference biases the beat command

    private readonly bool _reflex;
    private double _altitude,
        _velocity,
        _vigor,
        _command;

    public FlyingBody(double startAltitude = 300.0, bool reflex = true, double startVelocity = 0.0)
    {
        _altitude = startAltitude;
        _velocity = startVelocity;
        _reflex = reflex;
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
        int substeps = Math.Max(1, (int)Math.Round(dtSeconds * 1000.0 / NeuralStepMs));
        double vigorSum = 0.0;
        for (int i = 0; i < substeps; i++)
        {
            // 1. SENSE vertical motion → two opponent receptors (velocity → input current → spikes).
            double down = Math.Max(0.0, -_velocity);
            double up = Math.Max(0.0, _velocity);
            if (_descent.Step(SenseGain * down, NeuralStepMs))
            {
                _descAct += ActKick;
            }
            if (_ascent.Step(SenseGain * up, NeuralStepMs))
            {
                _ascAct += ActKick;
            }
            _descAct += (NeuralStepMs / ActTauMs) * (-_descAct);
            _ascAct += (NeuralStepMs / ActTauMs) * (-_ascAct);

            // 2. SMELL the food above/below (vertical chemotaxis) — climb the odour gradient.
            double climb = 0.0;
            if (_seeking)
            {
                if (_foodAbove.Step(SmellSenseGain * Smell(_altitude + SensorSpan), NeuralStepMs))
                {
                    _aboveAct += ActKick;
                }
                if (_foodBelow.Step(SmellSenseGain * Smell(_altitude - SensorSpan), NeuralStepMs))
                {
                    _belowAct += ActKick;
                }
                _aboveAct += (NeuralStepMs / ActTauMs) * (-_aboveAct);
                _belowAct += (NeuralStepMs / ActTauMs) * (-_belowAct);
                climb = ClimbGain * (_aboveAct - _belowAct); // food above ⇒ + ⇒ beat harder ⇒ rise
            }

            // 3. REFLEX: dropping → beat harder, rising → ease off (the hover). Plus the climb bias.
            double hover = _reflex ? CmdGain * (_descAct - _ascAct) : 0.0;
            _command = BaselineCommand + hover + climb;
            _wingbeat.SetCommand(_command);

            // 3. Beat, and gather the vigour the lift comes from.
            _wingbeat.Step(NeuralStepMs);
            vigorSum += _wingbeat.LeftActivity + _wingbeat.RightActivity;
        }
        _vigor = vigorSum / substeps;

        // 4. Newton on the vertical axis: lift up, gravity down.
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
    public double Command => _command; // the reflex's current "fly" command to the wingbeat
    public double DescentSense => Math.Clamp(_descAct, 0.0, 1.0); // receptor: "I'm dropping"
    public double AscentSense => Math.Clamp(_ascAct, 0.0, 1.0); // receptor: "I'm rising"
    public double WingPhase => _wingbeat.LeftActivity - _wingbeat.RightActivity; // for drawing the flap
    public bool Seeking => _seeking;
    public double TargetAltitude => _targetAlt; // the food's height (for the viewer)
}
