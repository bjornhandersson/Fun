namespace FruitFly.Living;

// A body that must FLY to stay up (Plan 0008, 2.5D). One vertical axis: gravity pulls it down, the
// wingbeat (the HalfCentreOscillator from Plan 0007) makes LIFT to push it up. This is where the
// beat finally does real work.
//
// At THIS step the beat runs at a FIXED command, so lift is roughly constant — which means the body
// can only fall or climb, never hold a steady hover. That failure is the point: a stable hover has
// to EMERGE from a reflex (sense vertical motion → adjust the beat), added in the next step, not
// from a hand-set thrust. Godot-free (ADR 0005): a viewer just reads Altitude and draws it.
public sealed class FlyingBody
{
    private readonly HalfCentreOscillator _wingbeat = new();

    // --- body + world: the bigger brain provides these (mass, gravity, the wing's lift transducer) ---
    private const double Gravity = 300.0; // downward acceleration (px/s²)
    private const double Mass = 1.0; // force and acceleration share units while Mass = 1
    private const double LiftGain = 520.0; // lift produced per unit of wingbeat vigour
    private const double NeuralStepMs = 1.0; // stable substep for the wingbeat, like the fly's brain

    private double _altitude; // px above the floor (0); up is positive
    private double _velocity; // px/s, up positive
    private double _vigor; // latest wingbeat vigour, 0..~1 (also exposed for the viewer)

    public FlyingBody(double startAltitude = 300.0)
    {
        _altitude = startAltitude;
    }

    public void Step(double dtSeconds)
    {
        // Tick the wingbeat in stable 1 ms substeps and AVERAGE its vigour across the frame, so the
        // body feels the cycle-averaged lift rather than each individual stroke. Vigour = total
        // oscillator activity = "how hard are the wings beating right now".
        int substeps = Math.Max(1, (int)Math.Round(dtSeconds * 1000.0 / NeuralStepMs));
        double sum = 0.0;
        for (int i = 0; i < substeps; i++)
        {
            _wingbeat.Step(NeuralStepMs);
            sum += _wingbeat.LeftActivity + _wingbeat.RightActivity;
        }
        _vigor = sum / substeps;

        // Newton on the vertical axis: lift up, gravity down. With a FIXED beat these never balance,
        // so the body accelerates one way and never settles — the control problem, made physical.
        double lift = LiftGain * _vigor;
        double accel = lift / Mass - Gravity;
        _velocity += accel * dtSeconds;
        _altitude += _velocity * dtSeconds;
    }

    // ---- Read-only state for viewers ----
    public double Altitude => _altitude;
    public double VerticalVelocity => _velocity;
    public double BeatVigor => _vigor;
}
