using System.Numerics;

namespace FruitFly.Living;

// A Braitenberg fruit fly with MEMORY and NOCICEPTION — the assembled creature (ADR 0005).
// It owns its BRAIN (a Network of LIF neurons + the wiring) and its BODY (sense organs and
// muscles that transduce world ↔ neural signals). Godot-free: a viewer constructs one, calls
// Step(world, dt) each frame, and reads its state to draw. Nothing here decides anything outside
// the neurons — steering, seeking, avoiding and escaping all EMERGE from the wiring.
//
// Smell sensors are CROSS-wired to the motors (seeking); touch receptors are UNCROSSED (avoiding);
// both sum at the shared motors. A self-exciting memory neuron, charged by repeated wall
// contact, latches "I'm stuck" and INHIBITS one wing to break a head-on deadlock, then releases
// via spike-frequency adaptation. A nociceptor fires only on a real collision (not mere nearness).
public sealed class Fly
{
    // ===== BRAIN — the tiny brain: neurons + wiring ==============================================
    // A little membrane noise keeps the fly off the "pencil tip": a perfectly symmetric head-on
    // wall would give equal L/R drive only in a noiseless sim; here one side always wins.
    private const double MembraneNoise = 1.0;

    private readonly Network _net = new();
    private readonly LifNeuron _sensorL = new() { NoiseSigma = MembraneNoise };
    private readonly LifNeuron _sensorR = new() { NoiseSigma = MembraneNoise };
    private readonly LifNeuron _motorL = new() { NoiseSigma = MembraneNoise };
    private readonly LifNeuron _motorR = new() { NoiseSigma = MembraneNoise };

    // Touch receptors: contact mechanoreceptors at the antennae. Each fires only while its antenna
    // is physically touching a wall — zero range, no "how near". Honest replacement for the old
    // proximity field (Plan 0004). UNCROSSED to the motors (below) so contact turns the fly away.
    private readonly LifNeuron _touchL = new() { NoiseSigma = MembraneNoise };
    private readonly LifNeuron _touchR = new() { NoiseSigma = MembraneNoise };

    // Nociceptor: a HARM sensor (not "pain" — flies have nociception, but felt suffering is
    // unknown). Fires only on a real ram, never on proximity. The clean "something is wrong" event.
    private readonly LifNeuron _noci = new() { NoiseSigma = MembraneNoise };

    // Memory interneuron: a self-synapse (wired below) makes it latch; adaptation makes it release.
    private readonly LifNeuron _memory = new()
    {
        NoiseSigma = 0.0,
        AdaptKick = MemAdaptKick,
        TauAdapt = MemAdaptTau,
    };

    public Fly(Vector2 start, float heading = 0f)
    {
        _pos = start;
        _heading = heading;

        _net.Add(_sensorL);
        _net.Add(_sensorR);
        _net.Add(_motorL);
        _net.Add(_motorR);
        _net.Add(_touchL);
        _net.Add(_touchR);
        _net.Add(_memory);
        _net.Add(_noci);

        // CROSSED smell → motor = SEEKING. UNCROSSED touch → motor = AVOIDING. Both sum.
        _net.Connect(_sensorL, _motorR, 45.0);
        _net.Connect(_sensorR, _motorL, 45.0);
        _net.Connect(_touchL, _motorL, 45.0);
        _net.Connect(_touchR, _motorR, 45.0);

        // Self-synapse = the latch. Touch receptors charge it (repeated contact → "I've been stuck").
        _net.Connect(_memory, _memory, MemorySelfWeight);
        _net.Connect(_touchL, _memory, WallToMemoryWeight);
        _net.Connect(_touchR, _memory, WallToMemoryWeight);

        // Memory → motor is INHIBITORY: a head-on jam saturates both wings, so only pulling one
        // DOWN out of saturation makes the difference that turns the fly. Negative weight.
        _net.Connect(_memory, _motorR, -MemoryToMotorWeight);
    }

    // ===== BODY state — the bigger brain provides the body ======================================
    private Vector2 _pos;
    private float _heading; // radians; 0 = +x

    private double _actL,
        _actR; // motor "muscle" activations: leaky integrals of motor spikes
    private double _actMem,
        _actNoci; // leaky integrals for the memory / nociceptor display
    private double _pokeMsLeft; // ms of manual "poke" still owed to the memory neuron
    private bool _colliding; // did the body ram a wall last step? drives the nociceptor next step

    private double _smellL,
        _smellR,
        _turnSignal; // latest readings, for the viewer
    private bool _touchedL,
        _touchedR; // did each antenna touch a wall this step? (for transduction + the viewer)

    // ===== Tunables (body + sensors + memory) ===================================================
    private const float AntennaSpread = 0.7f;
    private const float AntennaDist = 38f;

    private const double SensorTonic = 0.0;
    private const double SensorGain = 40.0;
    private const double MotorTonic = 0.0;

    private const double TouchCurrent = 40.0; // fixed depolarising kick while an antenna is in contact
    private const double NociGain = 60.0; // jolt while actually colliding — a sharp "ouch"

    private const double MotorTauMs = 60.0;
    private const double MotorKick = 1.0;
    private const double ActMax = 3.0;

    private const float CruiseSpeed = 130f; // px/sec at full combined activation
    private const float TurnSpeed = 3.2f; // rad/sec at full activation difference

    private const double MemorySelfWeight = 60.0; // the latch
    private const double MemAdaptKick = 0.6; // fatigue per spike → the release
    private const double MemAdaptTau = 400.0; // ms; how long fatigue lingers (hold + recovery)
    private const double WallToMemoryWeight = 25.0; // sustained wall contact charges the memory
    private const double MemoryToMotorWeight = 60.0; // inhibitory committed-turn strength
    private const double MemoryPokeCurrent = 30.0; // a manual SPACE poke
    private const double MemoryPokeMs = 40.0;
    private const double MemoryTauMs = 120.0; // memory/noci display smoothing

    private const double NeuralStepMs = 1.0; // fixed stable Euler step for the brain
    private const int SubstepsPerFrame = 4;

    // A manual poke (SPACE in the viewer): owe the memory neuron a brief strong input.
    public void Poke() => _pokeMsLeft = MemoryPokeMs;

    // Advance the whole creature by one frame of `dtSeconds`. The brain runs a fixed number of
    // stable substeps regardless of frame length; the body moves by the real frame time.
    public void Step(World world, double dtSeconds)
    {
        // 1. SENSE at each antenna.
        Vector2 antL = AntennaLeft;
        Vector2 antR = AntennaRight;
        _smellL = world.Smell(antL);
        _smellR = world.Smell(antR);
        _touchedL = world.Touching(antL);
        _touchedR = world.Touching(antR);

        // 2. TRANSDUCE world → input currents (sense organs feeding the brain). Touch is on/off:
        //    a fixed current while the antenna is in contact, nothing otherwise — no proximity grade.
        _net.SetInput(_sensorL, SensorTonic + SensorGain * _smellL);
        _net.SetInput(_sensorR, SensorTonic + SensorGain * _smellR);
        _net.SetInput(_touchL, _touchedL ? TouchCurrent : 0.0);
        _net.SetInput(_touchR, _touchedR ? TouchCurrent : 0.0);
        _net.SetInput(_motorL, MotorTonic);
        _net.SetInput(_motorR, MotorTonic);
        _net.SetInput(_noci, _colliding ? NociGain : 0.0); // only a real ram, set last step

        // 3. Run the brain in fixed substeps; integrate spikes into smooth muscle activations.
        for (int k = 0; k < SubstepsPerFrame; k++)
        {
            _net.SetInput(_memory, _pokeMsLeft > 0.0 ? MemoryPokeCurrent : 0.0);
            if (_pokeMsLeft > 0.0)
            {
                _pokeMsLeft -= NeuralStepMs;
            }

            _net.Step(NeuralStepMs);
            _actL += (NeuralStepMs / MotorTauMs) * (-_actL);
            _actR += (NeuralStepMs / MotorTauMs) * (-_actR);
            _actMem += (NeuralStepMs / MemoryTauMs) * (-_actMem);
            _actNoci += (NeuralStepMs / MemoryTauMs) * (-_actNoci);
            if (_net.Fired(_motorL))
            {
                _actL += MotorKick;
            }
            if (_net.Fired(_motorR))
            {
                _actR += MotorKick;
            }
            if (_net.Fired(_memory))
            {
                _actMem += MotorKick;
            }
            if (_net.Fired(_noci))
            {
                _actNoci += MotorKick;
            }
        }

        // 4. Activations → wings → motion. A fly steers by wingbeat: both wings beating = forward
        //    thrust (average), one beating harder than the other = yaw (difference).
        float wingL = (float)Math.Clamp(_actL / ActMax, 0.0, 1.0);
        float wingR = (float)Math.Clamp(_actR / ActMax, 0.0, 1.0);
        float forward = (wingL + wingR) * 0.5f * CruiseSpeed;
        float turn = (wingL - wingR) * TurnSpeed; // left wing beats harder → yaw right
        _turnSignal = wingL - wingR;

        _heading += turn * (float)dtSeconds;
        _pos += Heading(_heading) * forward * (float)dtSeconds;

        // 5. Keep inside the world; a clamp that moved us = a real collision (drives nociception
        //    next step). 6. Eat the banana if we reached it.
        _pos = world.Clamp(_pos, out _colliding);
        world.TryEat(_pos);
    }

    // ===== Read-only state for viewers ==========================================================
    public Vector2 Position => _pos;
    public float HeadingRadians => _heading;
    public Vector2 AntennaLeft => _pos + AntennaDist * Heading(_heading - AntennaSpread);
    public Vector2 AntennaRight => _pos + AntennaDist * Heading(_heading + AntennaSpread);

    public double SmellL => _smellL;
    public double SmellR => _smellR;
    public double TouchL => _touchedL ? 1.0 : 0.0; // 1 = that antenna is touching a wall right now
    public double TouchR => _touchedR ? 1.0 : 0.0;
    public double WingL => Math.Clamp(_actL / ActMax, 0.0, 1.0);
    public double WingR => Math.Clamp(_actR / ActMax, 0.0, 1.0);
    public double MemoryActivity => _actMem / ActMax;
    public double Nociception => _actNoci / ActMax;
    public double TurnSignal => _turnSignal;
    public bool Colliding => _colliding;

    private static Vector2 Heading(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));
}
