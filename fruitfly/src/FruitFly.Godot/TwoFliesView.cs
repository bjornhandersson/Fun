using Godot;
using FruitFly;   // SpikingNet from FruitFly.Core

// Play 7 — Two big-brain fruit flies (~300,000 neurons EACH) that seek honey and AVOID WALLS
// WITH THEIR BRAINS, in a field of 5 obstacles.
//
// Avoidance EMERGES from neurons, the Play 6 way, scaled to populations — never from a scripted
// bounce. The engine never touches the fly's heading; the only non-neural collision code is a
// position backstop (a body can't pass through a solid wall), which constrains where the body IS
// but never what it DECIDES. Turning away is entirely the brain's job. Each fly has two extra
// 50k-neuron WALL populations that feel how near the nearest wall is at each antenna, wired
// UNCROSSED to the motors:
//
//     smellL pop ──cross──►  motorR pop      (seeking: turn TOWARD honey)
//     smellR pop ──cross──►  motorL pop
//     wallL  pop ─uncross─►  motorL pop      (avoiding: turn AWAY from wall)
//     wallR  pop ─uncross─►  motorR pop
//
// A wall close on the left fires the left wall pop → speeds the left wheel → the fly yaws RIGHT,
// away. Seeking and avoiding feed the SAME motors, so they just SUM in the firing rates — no
// arbitration, no bounce. Two flies share the world but have separate brains and ignore each
// other. The only non-neural thing left is a hard backstop so a body can't pass through a wall.
public partial class TwoFliesView : Node2D
{
	// ---- Brain shape (one fly's SpikingNet) ----------------------------------------
	private const int Pop = 50_000;                 // neurons per population
	private const int N = 6 * Pop;                  // 300,000 neurons per fly (4 seeking pops + 2 wall pops)
	private const int FanIn = 2;                    // synapses each motor neuron receives, PER source population

	// Where each population lives in one fly's flat neuron index space. Wall pops are structurally
	// identical to smell pops — they only BECOME wall sensors via wiring (uncrossed) and drive.
	private const int SmellL = 0;
	private const int SmellR = Pop;
	private const int MotorL = 2 * Pop;
	private const int MotorR = 3 * Pop;
	private const int WallL = 4 * Pop;
	private const int WallR = 5 * Pop;

	// ---- Brain tunables -------------------------------------------------------------
	private const double BiasSpread = 6.0;   // per-neuron excitability spread → smooth population rate (see SpikingNet.Bias)
	private const double Weight = 12.0;      // synapse strength: one source spike nudges its target motor neuron
	private const double SensorTonic = 13.0; // smell-pop baseline drive (just under threshold, so faint smell still registers)
	private const double SensorGain = 145.0; // smell concentration → sensor input current (nudged up: stronger pull near honey)
	private const double MotorTonic = 12.0;  // motor-pop baseline drive → the fly always cruises forward

	private const double WallGain = 150.0;   // wall proximity → wall-pop input current (strong, so avoidance overrides seeking up close)
	private const double WallFalloff = 60.0; // px at which felt proximity halves — small, so walls register only when near

	// ---- One fly: its own brain + its own body -------------------------------------
	// A second fly is just a second instance of this. Each owns a private 300k-neuron SpikingNet.
	private sealed class Fly
	{
		public readonly SpikingNet Net;
		public Vector2 Pos;
		public float Heading;

		public double RateL, RateR;                 // smoothed motor-pop firing fractions = wheel speeds
		public double SmellLeft, SmellRight;        // latest antenna smell readings
		public double WallLeft, WallRight;          // latest wall-proximity readings (0 far, 1 touching)
		public readonly Color Tint;

		public Fly(int seed, Vector2 pos, float heading, Color tint)
		{
			Pos = pos; Heading = heading; Tint = tint;
			Net = new SpikingNet(N);
			var rng = new System.Random(seed);      // distinct seed → distinct brain, so the two flies differ

			for (int i = 0; i < N; i++)
				Net.Bias[i] = rng.NextDouble() * BiasSpread;

			Wire(Net, SmellL, MotorR, rng);         // CROSSED:   left smell  → right motor → seeking
			Wire(Net, SmellR, MotorL, rng);         // CROSSED:   right smell → left  motor
			Wire(Net, WallL, MotorL, rng);          // UNCROSSED: left wall   → left  motor → avoiding
			Wire(Net, WallR, MotorR, rng);          // UNCROSSED: right wall  → right motor
			Net.Build();

			Fill(Net, MotorL, MotorTonic);          // constant cruise drive, set once
			Fill(Net, MotorR, MotorTonic);
		}
	}

	// Connect every neuron in the destination pop to FanIn random sources in the source pop.
	// Direction-agnostic: "crossed" vs "uncrossed" is purely which two pops you pass in.
	private static void Wire(SpikingNet net, int sourceStart, int destStart, System.Random rng)
	{
		for (int m = 0; m < Pop; m++)
			for (int f = 0; f < FanIn; f++)
				net.Connect(sourceStart + rng.Next(Pop), destStart + m, Weight);
	}

	private static void Fill(SpikingNet net, int start, double current)
	{
		for (int i = 0; i < Pop; i++) net.External[start + i] = current;
	}

	// ---- Shared world (world units = pixels) ----------------------------------------
	private Fly[] _flies = System.Array.Empty<Fly>();
	private Vector2 _banana;
	private Rect2[] _walls = System.Array.Empty<Rect2>();   // 5 interior obstacles, built in _Ready

	private const float AntennaSpread = 0.7f;
	private const float AntennaDist = 38f;

	private const double SmellStrength = 1.0;
	private const double SmellFalloff = 360.0;   // shortened from Play 7's 420 → steeper gradient = stronger pull near the honey

	private const double MotorTauMs = 60.0;      // smoothing: turns spiky population firing into a smooth wheel speed
	private const double CruiseRate = 0.12;      // population firing fraction that counts as "full speed"

	private const float CruiseSpeed = 130f;
	private const float TurnSpeed = 3.2f;
	private const float EatRadius = 42f;

	private const float EdgeL = 20f, EdgeT = 70f, EdgeR = 20f, EdgeB = 20f;   // play-field margins

	private const double NeuralStepMs = 1.0;
	private const int SubstepsPerFrame = 4;

	private Label _readout = null!;

	public override void _Ready()
	{
		Vector2 size = GetViewportRect().Size;
		_banana = new Vector2(size.X - 90f, 90f);

		// Five interior walls as fractions of the field, scattered so they don't box in the two
		// start spots or the corners (where the banana respawns). World geometry, not brain — the
		// brain only ever feels them through the proximity sensors.
		_walls = new[]
		{
			Bar(size, 0.20f, 0.28f, 0.025f, 0.30f),   // vertical bar, left-of-center
			Bar(size, 0.42f, 0.16f, 0.24f,  0.025f),  // horizontal bar, upper-middle
			Bar(size, 0.62f, 0.52f, 0.025f, 0.30f),   // vertical bar, lower-right
			Bar(size, 0.30f, 0.70f, 0.24f,  0.025f),  // horizontal bar, lower-middle
			Bar(size, 0.80f, 0.34f, 0.025f, 0.26f),   // vertical bar, right side
		};

		// Two flies, distinct seeds / starts / tints. Separate brains; they ignore each other.
		_flies = new[]
		{
			new Fly(12345, new Vector2(size.X * 0.35f, size.Y * 0.5f), 0f, Colors.WhiteSmoke),
			new Fly(67890, new Vector2(size.X * 0.55f, size.Y * 0.5f), Mathf.Pi, Colors.Khaki),
		};

		AddChild(new Label
		{
			Text = $"Play 7 — two {N:N0}-neuron flies seek honey AND avoid 5 walls, all from population firing rates. No bounce.   (Esc = menu)",
			Position = new Vector2(20, 20),
		});
		_readout = new Label { Position = new Vector2(20, 44) };
		AddChild(_readout);
	}

	// Build one wall rect from fractions of the viewport (x, y = top-left; w, h = size).
	private static Rect2 Bar(Vector2 size, float fx, float fy, float fw, float fh) =>
		new(new Vector2(fx * size.X, fy * size.Y), new Vector2(fw * size.X, fh * size.Y));

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape })
			GetTree().ChangeSceneToFile("res://PlayMenu.tscn");
	}

	public override void _Process(double delta)
	{
		Vector2 size = GetViewportRect().Size;

		foreach (var fly in _flies)
			StepFly(fly, size, (float)delta);

		var f0 = _flies[0];
		_readout.Text =
			$"fly1  smell L/R: {f0.SmellLeft:0.00}/{f0.SmellRight:0.00}    wall L/R: {f0.WallLeft:0.00}/{f0.WallRight:0.00}    " +
			$"motor rate L/R: {f0.RateL:0.000}/{f0.RateR:0.000}    neurons: {2 * N:N0} across 2 brains";
		QueueRedraw();
	}

	// Advance one fly by one frame: sense → drive its brain → run substeps → move → backstop.
	private void StepFly(Fly fly, Vector2 size, float delta)
	{
		// 1. Sense at each antenna: honey smell, and how near the nearest wall is (edges + interior).
		Vector2 antL = fly.Pos + AntennaDist * Heading(fly.Heading - AntennaSpread);
		Vector2 antR = fly.Pos + AntennaDist * Heading(fly.Heading + AntennaSpread);
		fly.SmellLeft = Smell(antL);
		fly.SmellRight = Smell(antR);
		fly.WallLeft = WallProximity(antL, size);
		fly.WallRight = WallProximity(antR, size);

		// 2. Drive the pops: smell = baseline + smell; wall = pure proximity (silent away from walls);
		//    motors keep their constant cruise drive (set once in the Fly ctor).
		Fill(fly.Net, SmellL, SensorTonic + SensorGain * fly.SmellLeft);
		Fill(fly.Net, SmellR, SensorTonic + SensorGain * fly.SmellRight);
		Fill(fly.Net, WallL, WallGain * fly.WallLeft);
		Fill(fly.Net, WallR, WallGain * fly.WallRight);

		// 3. Run the brain a few substeps; smooth each motor pop's firing FRACTION into a wheel speed.
		for (int k = 0; k < SubstepsPerFrame; k++)
		{
			fly.Net.Step(NeuralStepMs);
			double rateL = (double)fly.Net.CountFired(MotorL, Pop) / Pop;
			double rateR = (double)fly.Net.CountFired(MotorR, Pop) / Pop;
			fly.RateL += (NeuralStepMs / MotorTauMs) * (rateL - fly.RateL);
			fly.RateR += (NeuralStepMs / MotorTauMs) * (rateR - fly.RateR);
		}

		// 4. Population rates → wheels → motion. Average = forward, difference = turn.
		float wheelL = (float)System.Math.Clamp(fly.RateL / CruiseRate, 0.0, 1.0);
		float wheelR = (float)System.Math.Clamp(fly.RateR / CruiseRate, 0.0, 1.0);
		float forward = (wheelL + wheelR) * 0.5f * CruiseSpeed;
		float turn = (wheelL - wheelR) * TurnSpeed;

		fly.Heading += turn * delta;
		fly.Pos += Heading(fly.Heading) * forward * delta;

		// Physical backstop ONLY: keep the body inside the edges and out of the solid walls. Turning
		// away is the brain's job (the wall pops above); this just stops tunneling if it reacts late.
		fly.Pos = new Vector2(Mathf.Clamp(fly.Pos.X, EdgeL, size.X - EdgeR),
		                      Mathf.Clamp(fly.Pos.Y, EdgeT, size.Y - EdgeB));
		foreach (var w in _walls) fly.Pos = PushOut(fly.Pos, w);

		// 5. Reached the honey? Respawn it so the flies keep seeking.
		if (fly.Pos.DistanceTo(_banana) < EatRadius)
			_banana = RandomCorner(size);
	}

	// ---- Smell field ----------------------------------------------------------------
	private double Smell(Vector2 at)
	{
		double d = at.DistanceTo(_banana);
		double r = d / SmellFalloff;
		return SmellStrength / (1.0 + r * r);
	}

	// ---- Wall field -----------------------------------------------------------------
	// How near is the nearest wall at `at` — the four edges OR any interior wall? Smooth 0..1, 1 at
	// a wall, fading to ~0 a few falloffs away. Same walls the backstop enforces, so the brain feels
	// exactly the world the body collides with.
	private double WallProximity(Vector2 at, Vector2 size)
	{
		float d = Mathf.Min(Mathf.Min(at.X - EdgeL, (size.X - EdgeR) - at.X),
		                    Mathf.Min(at.Y - EdgeT, (size.Y - EdgeB) - at.Y));
		foreach (var w in _walls) d = Mathf.Min(d, DistanceToRect(at, w));
		if (d < 0f) d = 0f;                          // poked past/into a wall → treat as touching
		double r = d / WallFalloff;
		return 1.0 / (1.0 + r * r);
	}

	// Distance from a point to a rectangle (0 if inside it).
	private static float DistanceToRect(Vector2 p, Rect2 r)
	{
		float dx = Mathf.Max(Mathf.Max(r.Position.X - p.X, p.X - r.End.X), 0f);
		float dy = Mathf.Max(Mathf.Max(r.Position.Y - p.Y, p.Y - r.End.Y), 0f);
		return Mathf.Sqrt(dx * dx + dy * dy);
	}

	// If `p` is inside wall `r`, shove it out through the nearest side. (Backstop only.)
	private static Vector2 PushOut(Vector2 p, Rect2 r)
	{
		if (!r.HasPoint(p)) return p;
		float left = p.X - r.Position.X, right = r.End.X - p.X;
		float top = p.Y - r.Position.Y, bottom = r.End.Y - p.Y;
		float m = Mathf.Min(Mathf.Min(left, right), Mathf.Min(top, bottom));
		if (m == left)  return new Vector2(r.Position.X, p.Y);
		if (m == right) return new Vector2(r.End.X, p.Y);
		if (m == top)   return new Vector2(p.X, r.Position.Y);
		return new Vector2(p.X, r.End.Y);
	}

	private static Vector2 Heading(float angle) => new(Mathf.Cos(angle), Mathf.Sin(angle));

	private Vector2 RandomCorner(Vector2 size)
	{
		float m = 90f;
		float x = GD.Randf() < 0.5f ? m : size.X - m;
		float y = GD.Randf() < 0.5f ? m : size.Y - m;
		return new Vector2(x, y);
	}

	// ---- Drawing -------------------------------------------------------------------
	public override void _Draw()
	{
		foreach (var w in _walls)
			DrawRect(w, new Color(0.45f, 0.45f, 0.5f));   // solid slabs to route around

		for (int i = 4; i >= 1; i--)
			DrawCircle(_banana, i * 26f, new Color(1f, 0.9f, 0.2f, 0.05f));
		DrawCircle(_banana, 13f, new Color(1f, 0.85f, 0.1f));

		foreach (var fly in _flies)
			DrawFly(fly);

		// Mini brain panel for fly 1: smell-pop drive (blue), motor-pop rate (green), wall (orange).
		var f0 = _flies[0];
		DrawMiniBar(20, 70, (float)f0.SmellLeft, Colors.SkyBlue);
		DrawMiniBar(54, 70, (float)f0.SmellRight, Colors.SkyBlue);
		DrawMiniBar(100, 70, (float)(f0.RateL / CruiseRate), Colors.LimeGreen);
		DrawMiniBar(134, 70, (float)(f0.RateR / CruiseRate), Colors.LimeGreen);
		DrawMiniBar(180, 70, (float)f0.WallLeft, Colors.OrangeRed);
		DrawMiniBar(214, 70, (float)f0.WallRight, Colors.OrangeRed);
	}

	private void DrawFly(Fly fly)
	{
		Vector2 nose = fly.Pos + 16f * Heading(fly.Heading);
		Vector2 tailL = fly.Pos + 12f * Heading(fly.Heading + 2.5f);
		Vector2 tailR = fly.Pos + 12f * Heading(fly.Heading - 2.5f);
		DrawColoredPolygon(new[] { nose, tailL, tailR }, fly.Tint);
		DrawCircle(fly.Pos + AntennaDist * Heading(fly.Heading - AntennaSpread), 3f, Colors.SkyBlue);
		DrawCircle(fly.Pos + AntennaDist * Heading(fly.Heading + AntennaSpread), 3f, Colors.SkyBlue);
	}

	private void DrawMiniBar(float x, float top, float value01, Color color)
	{
		const float h = 50f, w = 24f;
		float v = Mathf.Clamp(value01, 0f, 1f);
		DrawRect(new Rect2(x, top, w, h), new Color(1, 1, 1, 0.08f));
		DrawRect(new Rect2(x, top + h * (1f - v), w, h * v), color);
	}
}
