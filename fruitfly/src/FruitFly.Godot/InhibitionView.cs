using Godot;
using FruitFly;   // LifNeuron + Synapse from FruitFly.Core

// Play 4 — Inhibition (push and pull).
//
// Same shape as Play 3 (two inputs → one output), with ONE change: Input 2's synapse has a
// NEGATIVE weight. Nothing in LifNeuron or Synapse changed — a negative weight simply makes
// `Current += Weight` subtract, so Input 2's spikes push the output's membrane DOWN, away
// from firing. Excitation and inhibition are the same machine with opposite signs.
//
// Watch the brake: turn Input 1 up and the green output fires faster; turn Input 2 up and it
// fires LESS, even to silence. The pooled-current bar now swings below zero when inhibition
// wins. It's all continuous push and pull of current — never a switch.
public partial class InhibitionView : Node2D
{
	private readonly LifNeuron _in1 = new();
	private readonly LifNeuron _in2 = new();
	private readonly LifNeuron _out = new();
	private readonly Synapse _syn1;   // excitatory
	private readonly Synapse _syn2;   // inhibitory

	public InhibitionView()
	{
		// +45 excitatory: strong enough that Input 1 alone drives the output to fire steadily,
		// so there's something for inhibition to suppress.
		_syn1 = new Synapse(_in1, _out, weight: 45.0);
		// -45 inhibitory: equal magnitude, opposite sign — a symmetric brake. Input 2's spikes
		// subtract current, pulling the output back below threshold.
		_syn2 = new Synapse(_in2, _out, weight: -45.0);
	}

	private double _drive1 = 20.0;
	private double _drive2 = 0.0;    // start the brake OFF so you first see the output firing freely

	private const double DriveMin = 0.0;
	private const double DriveMax = 40.0;
	private const double DriveRate = 25.0;
	private const double SlowMotion = 0.1;

	private bool _f1, _f2, _fout;
	private Label _readout = null!;   // assigned in _Ready, before any frame runs

	public override void _Ready()
	{
		AddChild(new Label
		{
			Text = "Play 4 — Inhibition:  Input 1 (+) excites, Input 2 (−) suppresses     (Esc = menu)",
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
		double d = DriveRate * delta;
		if (Input.IsPhysicalKeyPressed(Key.Up))   _drive1 = Mathf.Min(DriveMax, _drive1 + d);
		if (Input.IsPhysicalKeyPressed(Key.Down)) _drive1 = Mathf.Max(DriveMin, _drive1 - d);
		if (Input.IsPhysicalKeyPressed(Key.W))    _drive2 = Mathf.Min(DriveMax, _drive2 + d);
		if (Input.IsPhysicalKeyPressed(Key.S))    _drive2 = Mathf.Max(DriveMin, _drive2 - d);

		double dtMs = delta * 1000.0 * SlowMotion;
		_f1 = _in1.Step(_drive1, dtMs);
		_f2 = _in2.Step(_drive2, dtMs);

		// Excitatory current + inhibitory (negative) current pool onto one membrane. The same
		// '+' as summation — but syn2's contribution is negative, so it subtracts.
		double total = _syn1.Step(_f1, dtMs) + _syn2.Step(_f2, dtMs);
		_fout = _out.Step(total, dtMs);

		_readout.Text = $"Input 1 (+) drive: {_drive1,4:0.0}  (↑/↓)        Input 2 (−) drive: {_drive2,4:0.0}  (W/S)";
		QueueRedraw();
	}

	// Wider voltage window than earlier Plays: VLow reaches -85 so we can see the output
	// HYPERPOLARIZE (dip below rest/reset) when inhibition pushes it down.
	private const float VLow = -85f, VHigh = -45f, VThreshold = -50f;
	// Current window is now SIGNED: it swings negative when inhibition wins.
	private const float CurrentLow = -80f, CurrentHigh = 80f;
	private const float PlotTop = 90f, PlotBottom = 400f, BarWidth = 60f;

	private const float In1X = 80f, In2X = 220f, SumX = 380f, OutX = 540f;

	public override void _Draw()
	{
		DrawThreshold(In1X);
		DrawBar(In1X, ValueToY((float)_in1.V, VLow, VHigh), _f1 ? Colors.White : Colors.SkyBlue);

		DrawThreshold(In2X);
		// Inhibitory input tinted differently so its role reads at a glance.
		DrawBar(In2X, ValueToY((float)_in2.V, VLow, VHigh), _f2 ? Colors.White : Colors.MediumPurple);

		// Pooled current — can be positive (net excitation, orange) or negative (net
		// inhibition, purple). Drawn from a zero baseline so the sign is visible.
		float sum = (float)(_syn1.Current + _syn2.Current);
		DrawSignedBar(SumX, sum, CurrentLow, CurrentHigh, Colors.Orange, Colors.MediumPurple);

		DrawThreshold(OutX);
		DrawBar(OutX, ValueToY((float)_out.V, VLow, VHigh), _fout ? Colors.White : Colors.LimeGreen);
	}

	// Filled bar from the plot bottom up to y (for voltages).
	private void DrawBar(float x, float y, Color color)
		=> DrawRect(new Rect2(x, y, BarWidth, PlotBottom - y), color);

	// Filled bar growing UP or DOWN from the value's zero line (for the signed current).
	private void DrawSignedBar(float x, float value, float low, float high, Color pos, Color neg)
	{
		float zeroY = ValueToY(0f, low, high);
		float valY = ValueToY(value, low, high);
		DrawLine(new Vector2(x - 10, zeroY), new Vector2(x + BarWidth + 10, zeroY), Colors.Gray, 1f);
		DrawRect(new Rect2(x, Mathf.Min(zeroY, valY), BarWidth, Mathf.Abs(zeroY - valY)),
				 value >= 0 ? pos : neg);
	}

	private void DrawThreshold(float x)
	{
		float y = ValueToY(VThreshold, VLow, VHigh);
		DrawLine(new Vector2(x - 10, y), new Vector2(x + BarWidth + 10, y), Colors.Red, width: 2f);
	}

	private static float ValueToY(float value, float low, float high)
	{
		float f = (value - low) / (high - low);
		return PlotBottom - f * (PlotBottom - PlotTop);
	}
}
