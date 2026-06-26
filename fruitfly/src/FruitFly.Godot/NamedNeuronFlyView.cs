using Godot;
using FruitFly;   // LifNeuron + Synapse + Network from FruitFly.Core

// Play 6 — Fruit fly + banana (a Braitenberg vehicle).
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
public partial class NamedNeuronFlyView : Node2D
{
	// ---- Brain (FruitFly.Core) ----------------------------------------------------
	private readonly Network _net = new();
	private readonly LifNeuron _sensorL = new();
	private readonly LifNeuron _sensorR = new();
	private readonly LifNeuron _motorL = new();
	private readonly LifNeuron _motorR = new();

	// Wall-proximity sensors. Same kind of neuron as the antennae, but they smell WALLS
	// instead of bananas, and they're wired UNCROSSED — which makes the fly turn AWAY.
	private readonly LifNeuron _wallL = new();
	private readonly LifNeuron _wallR = new();

	public NamedNeuronFlyView()
	{
		_net.Add(_sensorL); _net.Add(_sensorR);
		_net.Add(_motorL);  _net.Add(_motorR);
		_net.Add(_wallL);   _net.Add(_wallR);

		// CROSSED, excitatory: each smell sensor drives the OPPOSITE motor. weight 45 ≈ the
		// value that reliably turns sensor spikes into motor spikes. Crossing → SEEKING.
		_net.Connect(_sensorL, _motorR, 45.0);
		_net.Connect(_sensorR, _motorL, 45.0);

		// UNCROSSED, excitatory: each wall sensor drives the SAME-side motor. A near wall on
		// the left speeds the left wheel → fly yaws right, away from the wall. Same parts as
		// seeking, opposite crossing → AVOIDANCE. Both drives sum at the shared motors.
		_net.Connect(_wallL, _motorL, 45.0);
		_net.Connect(_wallR, _motorR, 45.0);
	}

	// ---- Body (world units = pixels) ----------------------------------------------
	private Vector2 _pos;
	private float _heading;        // radians; 0 = +x. (Godot y grows DOWN.)
	private Vector2 _banana;

	private double _actL, _actR;   // motor "muscle" activations: leaky integrals of motor spikes
	private double _turnSignal;    // the brain's live steering command, read (not recomputed) from the
	                               // motor activation gap: wheelL - wheelR, in [-1, +1]. 0 = flying
	                               // straight; magnitude = how hard the fly is compensating right now.
	private double _smellL, _smellR;   // latest antenna readings (kept for the mini brain panel)
	private double _wallL01, _wallR01; // latest wall-proximity readings at each antenna (0 = far, 1 = touching)

	// ---- Tunables -----------------------------------------------------------------
	private const float AntennaSpread = 0.7f;   // radians each antenna sits off-center (wider = bigger L/R contrast)
	private const float AntennaDist = 38f;      // how far antennae reach ahead of the body

	private const double SmellStrength = 1.0;   // concentration at the banana itself
	private const double SmellFalloff = 420.0;  // px at which concentration halves — big, so the gradient reaches across the screen
	private const double SensorTonic = 13.0;    // spontaneous baseline (just under firing) so faint smells still register
	private const double SensorGain = 130.0;    // smell concentration → sensor input current (well above threshold even far away)
	private const double MotorTonic = 16.0;     // baseline drive so the fly always cruises forward

	private const double WallGain = 55.0;       // wall proximity → wall-sensor input current (strong, so avoidance overrides seeking up close)
	private const double WallFalloff = 45.0;    // px at which proximity halves — small, so walls are felt only when near

	private const double MotorTauMs = 60.0;     // activation smoothing: turns spiky firing into smooth muscle
	private const double MotorKick = 1.0;       // activation added per motor spike
	private const double ActMax = 3.0;          // activation that counts as "full speed"

	private const float CruiseSpeed = 130f;     // px/sec at full combined activation
	private const float TurnSpeed = 3.2f;       // rad/sec at full activation difference
	private const float EatRadius = 42f;        // get this close and the banana is "eaten" (respawns)

	// Neural integration: many small stable Euler steps per rendered frame (dt << Tau=10ms).
	private const double NeuralStepMs = 1.0;
	private const int SubstepsPerFrame = 4;

	private Label _readout = null!;

	public override void _Ready()
	{
		Vector2 size = GetViewportRect().Size;
		_pos = size * 0.5f;            // fly starts in the middle
		_heading = 0f;
		_banana = new Vector2(size.X - 90f, 90f);   // banana in a corner (top-right)

		AddChild(new Label
		{
			Text = "Play 6 — Fruit fly seeks banana.  Crossed smell→motor wiring; seeking emerges.     (Esc = menu)",
			Position = new Vector2(20, 20),
		});
		_readout = new Label { Position = new Vector2(20, 44) };
		AddChild(_readout);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape })
			GetTree().ChangeSceneToFile("res://PlayMenu.tscn");
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

		// 3. Run the brain a few stable substeps; integrate motor spikes into smooth activations.
		for (int k = 0; k < SubstepsPerFrame; k++)
		{
			_net.Step(NeuralStepMs);
			_actL += (NeuralStepMs / MotorTauMs) * (-_actL);
			_actR += (NeuralStepMs / MotorTauMs) * (-_actR);
			if (_net.Fired(_motorL)) _actL += MotorKick;
			if (_net.Fired(_motorR)) _actR += MotorKick;
		}

		// 4. Activations → wheels → motion. Differential drive: average = forward, difference = turn.
		float wheelL = (float)System.Math.Clamp(_actL / ActMax, 0.0, 1.0);
		float wheelR = (float)System.Math.Clamp(_actR / ActMax, 0.0, 1.0);
		float forward = (wheelL + wheelR) * 0.5f * CruiseSpeed;
		float turn = (wheelL - wheelR) * TurnSpeed;   // left wheel faster → yaw right, and vice versa
		_turnSignal = wheelL - wheelR;                // stash the raw steering command for the plot

		_heading += turn * (float)delta;
		_pos += Heading(_heading) * forward * (float)delta;

		// Physical backstop only: the fly can't leave the screen. Steering AWAY from walls is
		// the brain's job now (the wall sensors above) — there is no scripted bounce anymore.
		_pos = new Vector2(Mathf.Clamp(_pos.X, 20f, size.X - 20f),
		                   Mathf.Clamp(_pos.Y, 70f, size.Y - 20f));

		// 5. Reached the banana? Move it to a fresh random corner so the fly keeps seeking.
		if (_pos.DistanceTo(_banana) < EatRadius)
			_banana = RandomCorner(size);

		_readout.Text = $"smell L/R: {_smellL:0.00}/{_smellR:0.00}    wall L/R: {_wallL01:0.00}/{_wallR01:0.00}    wheels L/R: {wheelL:0.00}/{wheelR:0.00}    compensating: {_turnSignal:+0.00;-0.00; 0.00}";
		QueueRedraw();
	}

	// ---- Smell field --------------------------------------------------------------
	private double Smell(Vector2 at)
	{
		double d = at.DistanceTo(_banana);
		double r = d / SmellFalloff;
		return SmellStrength / (1.0 + r * r);   // smooth falloff: 1 at the banana, halves at SmellFalloff
	}

	// ---- Wall field ---------------------------------------------------------------
	// How near is the nearest of the four play-field walls at point `at`? Mirrors Smell(): a
	// smooth 0..1, 1 right at a wall and fading to ~0 a few falloffs away. The four bounds are
	// the SAME ones the position clamp uses, so the sensor feels exactly the wall it can't cross.
	private double WallProximity(Vector2 at, Vector2 size)
	{
		float d = Mathf.Min(Mathf.Min(at.X - 20f, (size.X - 20f) - at.X),
		                    Mathf.Min(at.Y - 70f, (size.Y - 20f) - at.Y));
		if (d < 0f) d = 0f;                          // antenna poked past the bound → treat as touching
		double r = d / WallFalloff;
		return 1.0 / (1.0 + r * r);                  // 1 at the wall, halves at WallFalloff, ~0 far away
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
			DrawCircle(_banana, i * 26f, new Color(1f, 0.9f, 0.2f, 0.05f));
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
		DrawMiniBar(180, 70, (float)_wallL01, "wL", Colors.OrangeRed);   // wall-proximity sensors
		DrawMiniBar(214, 70, (float)_wallR01, "wR", Colors.OrangeRed);

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
		float cx = x + w * 0.5f;          // the zero line: dead-straight flight
		float half = w * 0.5f;            // full deflection (|s| = 1) fills exactly one half
		DrawRect(new Rect2(x, y, w, h), new Color(1, 1, 1, 0.08f));            // track
		if (s >= 0f)
			DrawRect(new Rect2(cx, y, half * s, h), Colors.Gold);             // turning one way
		else
			DrawRect(new Rect2(cx + half * s, y, -half * s, h), Colors.Gold); // ...the other
		DrawLine(new Vector2(cx, y), new Vector2(cx, y + h), new Color(1, 1, 1, 0.35f)); // center tick
	}

	private void DrawMiniBar(float x, float top, float value01, string _, Color color)
	{
		const float h = 50f, w = 24f;
		float v = Mathf.Clamp(value01, 0f, 1f);
		DrawRect(new Rect2(x, top, w, h), new Color(1, 1, 1, 0.08f));         // track
		DrawRect(new Rect2(x, top + h * (1f - v), w, h * v), color);          // fill from bottom
	}
}
