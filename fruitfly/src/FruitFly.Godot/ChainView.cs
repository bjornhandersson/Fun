using Godot;
using FruitFly;   // the Godot-free brain: LifNeuron + Synapse live in FruitFly.Core

// Play 2 — Two-neuron chain.
//
// A is driven by a constant input and fires periodically. Each A-spike pumps the synapse,
// whose current decays over several steps and charges B. We draw THREE bars side by side:
//
//     A (voltage)        synapse (current)        B (voltage)
//
// so you can watch the causal chain: A crosses its line -> the middle bar jumps and fades
// -> B ramps up and crosses ITS line. With the decaying synapse, B charges over several
// frames instead of teleporting (compare Play 1's instantaneous behaviour).
public partial class ChainView : Node2D
{
	// ---- The circuit (all from FruitFly.Core) -------------------------------------
	private readonly LifNeuron _a = new();
	private readonly LifNeuron _b = new();
	private readonly Synapse _synapse;

	// Constant input to A. 20 is above rheobase, so A charges and fires periodically —
	// same drive as Play 1's neuron.
	private const double Drive = 20.0;

	public ChainView()
	{
		// weight 70: with TauSyn=5 and the neuron's Tau=10, one A-spike peaks B's voltage
		// by ~0.25*70 ≈ 17 mV — just past the ~15 mV B needs to fire — reached by ramping
		// over several frames, not in one jump.
		_synapse = new Synapse(_a, _b, weight: 70.0);
	}

	// ---- Playback -----------------------------------------------------------------
	// 0.05 = heavy slow motion. A fires ~every 16 ms; this spreads each cycle over enough
	// frames to watch the synapse current rise and B charge up.
	private const double SlowMotion = 0.05;

	private bool _aFired;
	private bool _bFired;

	public override void _Ready()
	{
		AddChild(new Label
		{
			Text = "Play 2 — Two-neuron chain:  A  →  synapse  →  B     (Esc = menu)",
			Position = new Vector2(20, 20),
		});
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape })
			GetTree().ChangeSceneToFile("res://PlayMenu.tscn");
	}

	public override void _Process(double delta)
	{
		double dtMs = delta * 1000.0 * SlowMotion;

		// The exact same three-line update the console driver runs, in order:
		_aFired = _a.Step(Drive, dtMs);          // 1. A on its constant drive
		double toB = _synapse.Step(_aFired, dtMs); // 2. synapse advances (decays + maybe kick) → current for B
		_bFired = _b.Step(toB, dtMs);            // 3. B on whatever the synapse delivers

		QueueRedraw();
	}

	// ---- Drawing ------------------------------------------------------------------
	// Voltage window shared by both neuron bars (mV) — same range as Play 1.
	private const float VLow = -75f;
	private const float VHigh = -45f;
	private const float VThreshold = -50f;   // matches LifNeuron's default threshold

	// Current window for the synapse bar. 0 = silent; ~weight is the peak right after a
	// spike, so a little headroom above 70.
	private const float CurrentLow = 0f;
	private const float CurrentHigh = 80f;

	// Plot geometry shared by every bar.
	private const float PlotTop = 70f;       // y for the HIGH end (remember: y grows DOWN)
	private const float PlotBottom = 360f;   // y for the LOW end
	private const float BarWidth = 60f;

	// Left edge of each of the three bars.
	private const float Ax = 80f;
	private const float SynX = 280f;
	private const float Bx = 480f;

	public override void _Draw()
	{
		// A: voltage bar with its threshold line; flashes white the frame it fires.
		DrawThreshold(Ax, VThreshold);
		DrawValueBar(Ax, ValueToY((float)_a.V, VLow, VHigh), _aFired ? Colors.White : Colors.SkyBlue);

		// Synapse: current bar. No threshold line (it's a current, not a voltage). It jumps
		// the frame A fires, then decays — the visible "memory" of the synapse.
		DrawValueBar(SynX, ValueToY((float)_synapse.Current, CurrentLow, CurrentHigh), Colors.Orange);

		// B: voltage bar with its threshold line; flashes white the frame it fires.
		DrawThreshold(Bx, VThreshold);
		DrawValueBar(Bx, ValueToY((float)_b.V, VLow, VHigh), _bFired ? Colors.White : Colors.SkyBlue);
	}

	// A filled bar from the plot bottom up to y.
	private void DrawValueBar(float x, float y, Color color)
		=> DrawRect(new Rect2(x, y, BarWidth, PlotBottom - y), color);

	// A horizontal red threshold marker poking past a bar.
	private void DrawThreshold(float x, float voltage)
	{
		float y = ValueToY(voltage, VLow, VHigh);
		DrawLine(new Vector2(x - 10, y), new Vector2(x + BarWidth + 10, y), Colors.Red, width: 2f);
	}

	// Map a value to a y pixel within [PlotTop, PlotBottom] given that value's [low, high].
	private static float ValueToY(float value, float low, float high)
	{
		float f = (value - low) / (high - low);          // 0 at the bottom, 1 at the top
		return PlotBottom - f * (PlotBottom - PlotTop);  // invert: higher value -> smaller y
	}
}
