using Godot;
using FruitFly;   // the Godot-free brain (LifNeuron lives in FruitFly.Core)

// Draws one neuron's membrane voltage as a vertical bar.
//
// First version: STATIC — fixed voltages, no simulation yet. Its only job is to prove the
// scene -> script -> _Draw pipeline works and to set up the voltage->pixel mapping we'll
// reuse once the neuron is animated.
public partial class NeuronView : Node2D
{
	// The voltage window we visualize (mV) — same range as the console trace.
	private const float ScaleLow = -75f;   // maps to the bottom of the plot
	private const float ScaleHigh = -45f;  // maps to the top of the plot

	// Reference voltages — hard-coded for now; these match LifNeuron's defaults.
	private const float VRest = -65f;       // where a resting neuron sits
	private const float VThreshold = -50f;  // the firing line

	// The on-screen rectangle the bar lives in, in pixels. Remember: y grows DOWN,
	// so PlotTop (smaller y) is the HIGH-voltage end and PlotBottom is the LOW end.
	private const float PlotX = 80f;        // left edge of the bar
	private const float PlotWidth = 60f;
	private const float PlotTop = 60f;      // y for ScaleHigh
	private const float PlotBottom = 360f;  // y for ScaleLow

	// The live neuron this view animates. One real LifNeuron, stepped every frame.
	private readonly LifNeuron _neuron = new();

	// How hard we drive it each step (input current I). 20 is above rheobase, so it
	// charges and fires periodically — same constant the console used for neuron A.
	private const double Drive = 20.0;

	// Playback speed relative to real life. 1.0 = true fruit-fly speed: with these
	// parameters the neuron fires at ~62 Hz (a realistic driven-neuron rate), so at 1.0
	// it blinks ~62×/sec — a blur. Drop this (e.g. 0.1) to watch the cycle in slow motion.
	private const double SlowMotion = 0.1; //1.0

	// Did the neuron fire on the most recent step? Drives the spike flash in _Draw.
	private bool _fired;

	// Godot calls _Ready once when the scene loads. We add a title + hint label so the
	// Play identifies itself and tells you how to get back to the gallery.
	public override void _Ready()
	{
		AddChild(new Label
		{
			Text = "Play 1 — Single neuron     (Esc = menu)",
			Position = new Vector2(20, 20),
		});
	}

	// Esc returns to the gallery. Every Play shares this convention so you can always
	// step back out to pick another one.
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape })
			GetTree().ChangeSceneToFile("res://PlayMenu.tscn");
	}

	// Godot calls _Process once per frame. We use it to advance the simulation, then
	// ask for a repaint. We ignore `delta` (real seconds) for now and step a fixed Dt.
	public override void _Process(double delta)
	{
		// Advance the neuron by this frame's real elapsed time (delta is in seconds →
		// ×1000 for ms), scaled by SlowMotion. Now sim-time tracks wall-clock, so the
		// on-screen firing rate IS the neuron's true rate (× SlowMotion).
		double dtMs = delta * 1000.0 * SlowMotion;
		_fired = _neuron.Step(Drive, dtMs);   // advance; remember if it spiked
		QueueRedraw();                        // state changed → ask Godot to call _Draw this frame
	}

	// Godot calls _Draw whenever the node needs repainting.
	public override void _Draw()
	{
		// Threshold marker: a horizontal red line at VThreshold, poking out past the bar.
		float thrY = VoltageToY(VThreshold);
		DrawLine(new Vector2(PlotX - 10, thrY), new Vector2(PlotX + PlotWidth + 10, thrY),
				 Colors.Red, width: 2f);

		// The voltage bar: a filled rectangle from the bottom of the plot up to V.
		// V is the neuron's live membrane voltage now, not a constant — this is what
		// turns the static bar into an animation.
		float vY = VoltageToY((float)_neuron.V);
		Color barColor = _fired ? Colors.White : Colors.SkyBlue;  // flash white the frame it fires
		DrawRect(new Rect2(PlotX, vY, PlotWidth, PlotBottom - vY), barColor);
	}

	// Map a voltage (mV) to a y pixel within [PlotTop, PlotBottom].
	private static float VoltageToY(float v)
	{
		float f = (v - ScaleLow) / (ScaleHigh - ScaleLow); // 0 at the bottom, 1 at the top
		return PlotBottom - f * (PlotBottom - PlotTop);     // invert: higher V -> smaller y
	}
}
