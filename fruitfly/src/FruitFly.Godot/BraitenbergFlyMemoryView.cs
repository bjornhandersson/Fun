using FruitFly; // LifNeuron + Synapse + Network from FruitFly.Core
using Godot;

// Play 6b — Fruit fly + banana + a first piece of MEMORY (compare against Play 6).
//
// This starts as an exact copy of Play 6 (the resting Braitenberg fly). On top of that body we
// are growing ONE extra interneuron whose job is to *remember*. A LifNeuron that excites
// ITSELF (a self-synapse) can keep firing after its input stops: its own spikes loop back and
// re-trigger it. That persistent firing IS the memory — a leaky, graded ATTRACTOR, not a
// digital latch. The eventual goal: a fly pinned head-on against a wall remembers it is stuck
// and keeps turning to escape even after the wall stops pushing.
//
// Built one tiny piece at a time. The memory neuron is charged by the WALL SENSORS: grind the
// fly head-on into a wall and its memory bar latches (it "remembers it's stuck"). That latched
// memory now drives ONE wheel — a COMMITTED turn that persists past the wall pressure and breaks
// the head-on deadlock, then releases on its own via fatigue once clear. SPACE pokes it manually.
//
// The original Play-6 description still applies to the body:
//
// The fly has two smell sensors (antennae) and two motor neurons (wheels). Smell from the
// banana drives the sensors; the sensors are CROSS-wired to the motors:
//
//     left sensor  ──► RIGHT motor
//     right sensor ──► LEFT  motor
//
// Nothing scripts the steering. When the banana is to the fly's left, the left antenna
// smells more → it drives the RIGHT wheel harder → the fly yaws left, toward the banana.
// Seeking EMERGES from the crossing. (Swap the two crossed lines and you'd get a fly that
// FLEES — Braitenberg's "fear" vehicle. Same parts, opposite sign of behavior.)
//
// Everything stays continuous and rate-coded: motor firing RATE sets wheel speed; the turn
// is the DIFFERENCE of two rates. No bits, no gates — a brain steering a body.
//
// WALLS are handled the same way — by the brain, not by scripted geometry. Two extra
// proximity sensors feel how near the nearest wall is at each antenna and are wired UNCROSSED
// to the motors (left wall sensor → left motor). A wall close on the left speeds the left
// wheel → the fly yaws right, away from it. Seeking (crossed) and avoiding (uncrossed) feed
// the SAME two motors, so the behaviors just SUM — no arbitration code. The only non-neural
// thing left is a hard clamp so the fly physically can't leave the screen.
public partial class BraitenbergFlyMemoryView : Node2D
{
    // ---- Brain (FruitFly.Core) ----------------------------------------------------
    // Every membrane carries a little noise (mV per √ms). Real neurons jitter — ion channels
    // flicker, synapses bombard — and that jitter is what keeps the fly off the "pencil tip":
    // a perfectly symmetric head-on wall gives equal left/right drive only in a noiseless sim;
    // here the two sides never read exactly equal, so one wheel always wins and the fly turns
    // away. ~1.0 ≈ 2 mV of wobble: enough to break ties, far too little to fire on noise alone.
    private const double MembraneNoise = 1.0;

    private readonly Network _net = new();
    private readonly LifNeuron _sensorL = new() { NoiseSigma = MembraneNoise };
    private readonly LifNeuron _sensorR = new() { NoiseSigma = MembraneNoise };
    private readonly LifNeuron _motorL = new() { NoiseSigma = MembraneNoise };
    private readonly LifNeuron _motorR = new() { NoiseSigma = MembraneNoise };

    // Wall-proximity sensors. Same kind of neuron as the antennae, but they smell WALLS
    // instead of bananas, and they're wired UNCROSSED — which makes the fly turn AWAY.
    private readonly LifNeuron _wallL = new() { NoiseSigma = MembraneNoise };
    private readonly LifNeuron _wallR = new() { NoiseSigma = MembraneNoise };

    // The NOCICEPTOR — a HARM sensor, and deliberately not called a "pain" sensor: flies clearly
    // have nociception (detecting damage and escaping it), but whether they consciously SUFFER is
    // unknown, so we claim only what's real. Unlike the wall sensors, which fire on PROXIMITY
    // ("a wall is near"), this fires only when the body actually RAMS a wall ("that hurt"). It is
    // the clean "something is wrong" event we'll later learn from. Not wired to behavior yet.
    private readonly LifNeuron _noci = new() { NoiseSigma = MembraneNoise };

    // The MEMORY interneuron. An ordinary LifNeuron — no special "memory" machinery. What makes
    // it remember is the self-synapse wired in the constructor: its spikes feed back into
    // itself. We give it NO noise so its hold/decay is clean to read while we tune it (the
    // recurrent loop, not jitter, should drive it). Everything else is the default LIF
    // personality: born at VRest (-65), fires at -50, leaks with Tau = 10 ms.
    private readonly LifNeuron _memory = new()
    {
        NoiseSigma = 0.0,
        AdaptKick = MemAdaptKick, // fatigue — lets the latch RELEASE on its own once the fly is clear
        TauAdapt = MemAdaptTau,
    };

    public BraitenbergFlyMemoryView()
    {
        _net.Add(_sensorL);
        _net.Add(_sensorR);
        _net.Add(_motorL);
        _net.Add(_motorR);
        _net.Add(_wallL);
        _net.Add(_wallR);
        _net.Add(_memory);
        _net.Add(_noci);

        // CROSSED, excitatory: each smell sensor drives the OPPOSITE motor. weight 45 ≈ the
        // value that reliably turns sensor spikes into motor spikes. Crossing → SEEKING.
        _net.Connect(_sensorL, _motorR, 45.0);
        _net.Connect(_sensorR, _motorL, 45.0);

        // UNCROSSED, excitatory: each wall sensor drives the SAME-side motor. A near wall on
        // the left speeds the left wheel → fly yaws right, away from the wall. Same parts as
        // seeking, opposite crossing → AVOIDANCE. Both drives sum at the shared motors.
        _net.Connect(_wallL, _motorL, 45.0);
        _net.Connect(_wallR, _motorR, 45.0);

        // THE memory: wire the neuron to ITSELF. Source and target are the same neuron, so every
        // spike it fires loads its OWN synapse, which (after the network's built-in one-tick
        // delay) pushes it back up toward threshold. If MemorySelfWeight is strong enough, that
        // feedback re-fires it before the leak pulls it back to rest → self-sustaining activity =
        // the held state. Too strong and it never stops (saturates); too weak and it fades at
        // once. The bistable sweet spot in between is what we'll tune by poking it.
        _net.Connect(_memory, _memory, MemorySelfWeight);

        // WALL → MEMORY: both wall sensors also charge the memory neuron. While the fly is jammed
        // against a wall the wall sensors fire continuously, and that sustained drive INTEGRATES
        // on the memory neuron until it tips into its self-sustaining latch. A brief brush isn't
        // enough — only being stuck for a moment is — which is exactly "remember that I'm stuck".
        // Head-on (both sensors firing) charges it fastest, i.e. the most-stuck case latches soonest.
        _net.Connect(_wallL, _memory, WallToMemoryWeight);
        _net.Connect(_wallR, _memory, WallToMemoryWeight);

        // MEMORY → MOTOR (asymmetric, INHIBITORY): a LATCHED memory pulls ONE wheel DOWN. Why
        // inhibit rather than excite? A head-on jam SATURATES both motors (wall drive pins both
        // wheels at full), so pushing a wheel harder changes nothing — the difference stays zero
        // and the fly drives straight into the wall. Pulling the RIGHT wheel down instead drops it
        // out of saturation, so wheelL > wheelR → the fly yaws right and peels off. Because the
        // memory holds itself, that turn persists past the wall pressure, then releases on fatigue.
        // Negative weight = inhibition. The escape direction is a wiring choice, not a script.
        _net.Connect(_memory, _motorR, -MemoryToMotorWeight);
    }

    // ---- Body (world units = pixels) ----------------------------------------------
    private Vector2 _pos;
    private float _heading; // radians; 0 = +x. (Godot y grows DOWN.)
    private Vector2 _banana;

    private double _actL,
        _actR; // motor "muscle" activations: leaky integrals of motor spikes
    private double _turnSignal; // the brain's live steering command, read (not recomputed) from the

    // motor activation gap: wheelL - wheelR, in [-1, +1]. 0 = flying
    // straight; magnitude = how hard the fly is compensating right now.
    private double _smellL,
        _smellR; // latest antenna readings (kept for the mini brain panel)
    private double _wallL01,
        _wallR01; // latest wall-proximity readings at each antenna (0 = far, 1 = touching)

    private double _actMem; // leaky integral of the memory neuron's spikes — for the panel bar
    private double _actNoci; // leaky integral of the nociceptor's spikes — for its panel bar
    private double _pokeMsLeft; // ms of manual "poke" input still owed to the memory neuron (SPACE)
    private bool _colliding; // did the body actually ram a wall last frame? this is what drives the nociceptor

    // ---- Tunables -----------------------------------------------------------------
    private const float AntennaSpread = 0.7f; // radians each antenna sits off-center (wider = bigger L/R contrast)
    private const float AntennaDist = 38f; // how far antennae reach ahead of the body

    private const double SmellStrength = 1.0; // concentration at the banana itself
    private const double SmellFalloff = 420.0; // px at which concentration halves — big, so the gradient reaches across the screen
    private const double SensorTonic = 0.0; // no baseline: with nothing to smell, the sensors are silent and the fly truly rests
    private const double SensorGain = 40.0; // smell concentration → sensor input current. Tuned so a FAR banana sits right at the firing line (a hesitant creep) and a NEAR banana drives hard — speed now RISES as the fly closes in, instead of being pinned at max everywhere
    private const double MotorTonic = 0.0; // no baked-in cruise: the fly starts at REST, and motion must EMERGE from what it senses (smell drives the motors via the cross-wiring; membrane noise gives an occasional resting twitch)

    private const double WallGain = 40.0; // wall proximity → wall-sensor input current. Lower = the fly's wall organs are LESS sensitive, so it's less "scared" — it lets walls get closer before reacting (and charges the memory a touch less eagerly too).
    private const double WallFalloff = 45.0; // px at which proximity halves — small, so walls are felt only when near
    private const double NociGain = 60.0; // input the nociceptor gets WHILE the body is colliding — a sharp, strong "ouch", well above threshold (it's an event, not a graded nearness)

    private const double MotorTauMs = 60.0; // activation smoothing: turns spiky firing into smooth muscle
    private const double MotorKick = 1.0; // activation added per motor spike
    private const double ActMax = 3.0; // activation that counts as "full speed"

    private const float CruiseSpeed = 130f; // px/sec at full combined activation
    private const float TurnSpeed = 3.2f; // rad/sec at full activation difference
    private const float EatRadius = 42f; // get this close and the banana is "eaten" (respawns)

    // ---- Memory neuron (the new piece) --------------------------------------------
    private const double MemorySelfWeight = 60.0; // self-excitation per spike — the LATCH. Carried over from Play 5b, where it gives a solid hold.
    private const double MemAdaptKick = 0.6; // fatigue added per spike — the RELEASE. Small, so the brake builds slowly and the hold lasts.
    private const double MemAdaptTau = 400.0; // ms; how long fatigue lingers (sets hold + recovery time). Tune up for a longer committed turn.
    private const double WallToMemoryWeight = 25.0; // how hard each wall-sensor spike charges the memory. Tuned so SUSTAINED contact latches it, a brief brush does not.
    private const double MemoryToMotorWeight = 60.0; // a latched memory INHIBITS one wheel → a committed turn. Must be INHIBITORY, not excitatory: a head-on jam SATURATES both motors, so pushing a wheel harder does nothing — only pulling the OTHER wheel DOWN creates the left/right difference that turns the fly. Magnitude must beat the wall+smell drive on that wheel.
    private const double MemoryPokeCurrent = 30.0; // input current a SPACE-poke injects — comfortably above threshold so the neuron starts firing
    private const double MemoryPokeMs = 40.0; // how long one poke lasts (ms). A brief stimulus: the question is whether activity OUTLASTS it.
    private const double MemoryTauMs = 120.0; // smoothing for the panel bar only (slower than the motors, so a brief hold is easy to see)
    private const double MemoryKick = 1.0; // bar activation added per memory spike

    // Neural integration: many small stable Euler steps per rendered frame (dt << Tau=10ms).
    private const double NeuralStepMs = 1.0;
    private const int SubstepsPerFrame = 4;

    private Label _readout = null!;

    public override void _Ready()
    {
        Vector2 size = GetViewportRect().Size;
        _pos = size * 0.5f; // fly starts in the middle
        _heading = 0f;
        _banana = new Vector2(size.X - 90f, 90f); // banana in a corner (top-right)

        AddChild(
            new Label
            {
                Text =
                    "Play 6b — Fruit fly + memory.  Drive head-on into a wall: the memory LATCHES and drives a committed turn that breaks the deadlock, then releases.  (SPACE = manual poke.)     (Esc = menu)",
                Position = new Vector2(20, 20),
            }
        );
        _readout = new Label { Position = new Vector2(20, 44) };
        AddChild(_readout);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape })
        {
            GetTree().ChangeSceneToFile("res://PlayMenu.tscn");
        }

        // SPACE = a manual poke: owe the memory neuron a brief, strong input. _Process spends
        // this down over the next MemoryPokeMs and then stops — so anything we see AFTER that is
        // the neuron holding ITSELF, not us still pushing it.
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Space })
        {
            _pokeMsLeft = MemoryPokeMs;
        }
    }

    public override void _Process(double delta)
    {
        Vector2 size = GetViewportRect().Size;

        // 1. Sense at each antenna: how strong the banana smell is, and how near the nearest wall is.
        Vector2 antL = _pos + AntennaDist * Heading(_heading - AntennaSpread);
        Vector2 antR = _pos + AntennaDist * Heading(_heading + AntennaSpread);
        _smellL = Smell(antL);
        _smellR = Smell(antR);
        _wallL01 = WallProximity(antL, size);
        _wallR01 = WallProximity(antR, size);

        // 2. Feed the brain: smell sensors get baseline + smell drive; wall sensors get pure
        //    proximity drive (silent away from walls); motors get a tonic cruise drive.
        _net.SetInput(_sensorL, SensorTonic + SensorGain * _smellL);
        _net.SetInput(_sensorR, SensorTonic + SensorGain * _smellR);
        _net.SetInput(_wallL, WallGain * _wallL01);
        _net.SetInput(_wallR, WallGain * _wallR01);
        _net.SetInput(_motorL, MotorTonic);
        _net.SetInput(_motorR, MotorTonic);
        // Nociceptor: a strong jolt while the body is actually colliding (set last frame), else
        // silent. Proximity does NOT drive it — only a real ram does.
        _net.SetInput(_noci, _colliding ? NociGain : 0.0);

        // 3. Run the brain a few stable substeps; integrate motor spikes into smooth activations.
        for (int k = 0; k < SubstepsPerFrame; k++)
        {
            // Memory neuron input THIS substep: the poke current while we still owe poke time,
            // otherwise nothing. Decrement the owed time by one substep so the kick is brief —
            // after it runs out, the only thing that can keep the neuron firing is its own
            // self-synapse. (Not wired to the body yet, so this is the isolation test.)
            _net.SetInput(_memory, _pokeMsLeft > 0.0 ? MemoryPokeCurrent : 0.0);
            if (_pokeMsLeft > 0.0)
            {
                _pokeMsLeft -= NeuralStepMs;
            }

            _net.Step(NeuralStepMs);
            _actL += (NeuralStepMs / MotorTauMs) * (-_actL);
            _actR += (NeuralStepMs / MotorTauMs) * (-_actR);
            _actMem += (NeuralStepMs / MemoryTauMs) * (-_actMem); // memory bar leaks slower than the motors, so a brief hold is easy to read
            _actNoci += (NeuralStepMs / MemoryTauMs) * (-_actNoci); // nociceptor bar, same slow leak so a brief "ouch" stays visible
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
                _actMem += MemoryKick; // each memory spike bumps its display bar; the self-synapse keeps the spikes coming
            }
            if (_net.Fired(_noci))
            {
                _actNoci += MemoryKick; // each nociceptor spike bumps its red "ouch" bar
            }
        }

        // 4. Activations → wheels → motion. Differential drive: average = forward, difference = turn.
        float wheelL = (float)System.Math.Clamp(_actL / ActMax, 0.0, 1.0);
        float wheelR = (float)System.Math.Clamp(_actR / ActMax, 0.0, 1.0);
        float forward = (wheelL + wheelR) * 0.5f * CruiseSpeed;
        float turn = (wheelL - wheelR) * TurnSpeed; // left wheel faster → yaw right, and vice versa
        _turnSignal = wheelL - wheelR; // stash the raw steering command for the plot

        _heading += turn * (float)delta;
        _pos += Heading(_heading) * forward * (float)delta;

        // Physical backstop only: the fly can't leave the screen. Steering AWAY from walls is
        // the brain's job now (the wall sensors above) — there is no scripted bounce anymore.
        Vector2 beforeClamp = _pos;
        _pos = new Vector2(
            Mathf.Clamp(_pos.X, 20f, size.X - 20f),
            Mathf.Clamp(_pos.Y, 70f, size.Y - 20f)
        );
        // Did the wall physically STOP the body this frame? Then the fly rammed it — a real
        // collision, which is exactly what the nociceptor reports (next frame: a tiny reflex
        // latency, since the brain already stepped above). Proximity alone never trips this.
        _colliding = !_pos.IsEqualApprox(beforeClamp);

        // 5. Reached the banana? Move it to a fresh random corner so the fly keeps seeking.
        if (_pos.DistanceTo(_banana) < EatRadius)
        {
            _banana = RandomCorner(size);
        }

        _readout.Text =
            $"smell L/R: {_smellL:0.00}/{_smellR:0.00}    wall L/R: {_wallL01:0.00}/{_wallR01:0.00}    wheels L/R: {wheelL:0.00}/{wheelR:0.00}    compensating: {_turnSignal:+0.00;-0.00; 0.00}";
        QueueRedraw();
    }

    // ---- Smell field --------------------------------------------------------------
    private double Smell(Vector2 at)
    {
        double d = at.DistanceTo(_banana);
        double r = d / SmellFalloff;
        return SmellStrength / (1.0 + r * r); // smooth falloff: 1 at the banana, halves at SmellFalloff
    }

    // ---- Wall field ---------------------------------------------------------------
    // How near is the nearest of the four play-field walls at point `at`? Mirrors Smell(): a
    // smooth 0..1, 1 right at a wall and fading to ~0 a few falloffs away. The four bounds are
    // the SAME ones the position clamp uses, so the sensor feels exactly the wall it can't cross.
    private double WallProximity(Vector2 at, Vector2 size)
    {
        float d = Mathf.Min(
            Mathf.Min(at.X - 20f, (size.X - 20f) - at.X),
            Mathf.Min(at.Y - 70f, (size.Y - 20f) - at.Y)
        );
        if (d < 0f) // antenna poked past the bound → treat as touching
        {
            d = 0f;
        }
        double r = d / WallFalloff;
        return 1.0 / (1.0 + r * r); // 1 at the wall, halves at WallFalloff, ~0 far away
    }

    private static Vector2 Heading(float angle) => new(Mathf.Cos(angle), Mathf.Sin(angle));

    private Vector2 RandomCorner(Vector2 size)
    {
        float m = 90f;
        float x = GD.Randf() < 0.5f ? m : size.X - m;
        float y = GD.Randf() < 0.5f ? m : size.Y - m;
        return new Vector2(x, y);
    }

    // ---- Drawing ------------------------------------------------------------------
    public override void _Draw()
    {
        // Banana: a soft glow + a yellow blob, so its "smell" is visible as a gradient.
        for (int i = 4; i >= 1; i--)
        {
            DrawCircle(_banana, i * 26f, new Color(1f, 0.9f, 0.2f, 0.05f));
        }
        DrawCircle(_banana, 13f, new Color(1f, 0.85f, 0.1f));

        // Fly: a triangle pointing along its heading, with two antenna dots.
        Vector2 nose = _pos + 16f * Heading(_heading);
        Vector2 tailL = _pos + 12f * Heading(_heading + 2.5f);
        Vector2 tailR = _pos + 12f * Heading(_heading - 2.5f);
        DrawColoredPolygon(new[] { nose, tailL, tailR }, Colors.WhiteSmoke);
        DrawCircle(_pos + AntennaDist * Heading(_heading - AntennaSpread), 3f, Colors.SkyBlue);
        DrawCircle(_pos + AntennaDist * Heading(_heading + AntennaSpread), 3f, Colors.SkyBlue);

        // Tiny brain panel (top-left): sensor drive and motor activation, so you can watch the
        // brain steer the body — the live dual-view in miniature.
        DrawMiniBar(20, 70, (float)_smellL, "sL", Colors.SkyBlue);
        DrawMiniBar(54, 70, (float)_smellR, "sR", Colors.SkyBlue);
        DrawMiniBar(100, 70, (float)(_actL / ActMax), "mL", Colors.LimeGreen);
        DrawMiniBar(134, 70, (float)(_actR / ActMax), "mR", Colors.LimeGreen);
        DrawMiniBar(180, 70, (float)_wallL01, "wL", Colors.OrangeRed); // wall-proximity sensors
        DrawMiniBar(214, 70, (float)_wallR01, "wR", Colors.OrangeRed);
        DrawMiniBar(260, 70, (float)(_actMem / ActMax), "mem", Colors.Magenta); // the memory neuron: held activity AFTER a SPACE poke is the memory itself
        DrawMiniBar(300, 70, (float)(_actNoci / ActMax), "noci", new Color(1f, 0.15f, 0.15f)); // the nociceptor: red "ouch", lights ONLY on a real wall ram

        // Compensation needle: the net steering the brain is producing this instant. Centered =
        // flying straight; the gold bar grows out toward whichever side the fly is turning, and
        // how far it grows is how hard it's correcting for scent + walls combined.
        DrawCompensationBar(20, 140, 218, 16, (float)_turnSignal);
    }

    // A centered "needle" bar for a signed value in [-1, +1]: nothing in the middle means the
    // two inputs are balanced; fill grows right for positive, left for negative.
    private void DrawCompensationBar(float x, float y, float w, float h, float signed)
    {
        float s = Mathf.Clamp(signed, -1f, 1f);
        float cx = x + w * 0.5f; // the zero line: dead-straight flight
        float half = w * 0.5f; // full deflection (|s| = 1) fills exactly one half
        DrawRect(new Rect2(x, y, w, h), new Color(1, 1, 1, 0.08f)); // track
        if (s >= 0f)
        {
            DrawRect(new Rect2(cx, y, half * s, h), Colors.Gold); // turning one way
        }
        else
        {
            DrawRect(new Rect2(cx + half * s, y, -half * s, h), Colors.Gold); // ...the other
        }
        DrawLine(new Vector2(cx, y), new Vector2(cx, y + h), new Color(1, 1, 1, 0.35f)); // center tick
    }

    private void DrawMiniBar(float x, float top, float value01, string _, Color color)
    {
        const float h = 50f,
            w = 24f;
        float v = Mathf.Clamp(value01, 0f, 1f);
        DrawRect(new Rect2(x, top, w, h), new Color(1, 1, 1, 0.08f)); // track
        DrawRect(new Rect2(x, top + h * (1f - v), w, h * v), color); // fill from bottom
    }
}
