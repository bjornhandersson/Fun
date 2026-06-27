using FruitFly; // LifNeuron + Synapse from FruitFly.Core
using Godot;

// Play 3 — Summation (integration).
//
// Two input neurons converge on ONE output neuron. Each input is driven by a current you
// control live from the keyboard, so it fires at a rate you set. Their two synaptic
// currents are ADDED and the sum is fed to the output's membrane — which is all
// "summation" is. The neuron code is untouched: LifNeuron.Step already integrates whatever
// single current it's given; we just hand it the sum.
//
// Watch it as an ANALOG MIXER, not a gate: turn either input up and the output's firing
// RATE rises; turn both down and it slows and stops. Nothing flips on/off — the output
// rate moves continuously with the combined drive.
public partial class SummationView : Node2D
{
    // ---- The circuit (all FruitFly.Core) ------------------------------------------
    private readonly LifNeuron _in1 = new();
    private readonly LifNeuron _in2 = new();
    private readonly LifNeuron _out = new();
    private readonly Synapse _syn1;
    private readonly Synapse _syn2;

    public SummationView()
    {
        // weight 35: one input spike alone bumps the output by ~0.25*35 ≈ 9 mV — NOT enough
        // to fire it from rest on its own. So the output only fires when spikes pile up
        // (from a fast input, or from both inputs together): its rate reflects the summed
        // drive rather than copying one input one-for-one.
        _syn1 = new Synapse(_in1, _out, weight: 35.0);
        _syn2 = new Synapse(_in2, _out, weight: 35.0);
    }

    // ---- Live input you control ---------------------------------------------------
    // Drive = the constant input current into each input neuron (sets its firing rate).
    // Start both at 20 (the same "above rheobase" drive as earlier Plays).
    private double _drive1 = 20.0;
    private double _drive2 = 20.0;

    private const double DriveMin = 0.0; // 0 = that input goes silent
    private const double DriveMax = 40.0; // plenty to fire fast
    private const double DriveRate = 25.0; // units/sec change while a key is held — analog, not a toggle

    private const double SlowMotion = 0.1; // watchable: individual spikes visible, rate changes still felt

    private bool _f1,
        _f2,
        _fout; // did each neuron fire this frame? (for the flash)
    private Label _readout = null!; // assigned in _Ready, before any frame runs

    public override void _Ready()
    {
        AddChild(
            new Label
            {
                Text = "Play 3 — Summation:  two inputs pooled onto one output     (Esc = menu)",
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
    }

    public override void _Process(double delta)
    {
        // --- live, analog drive control (held keys nudge the drive smoothly) ---
        double d = DriveRate * delta;
        // Physical-key checks: read the key's POSITION, not the layout-dependent letter, so
        // W/S work regardless of keyboard layout (the previous IsKeyPressed was flaky here).
        if (Input.IsPhysicalKeyPressed(Key.Up))
        {
            _drive1 = System.Math.Min(DriveMax, _drive1 + d);
        }
        if (Input.IsPhysicalKeyPressed(Key.Down))
        {
            _drive1 = System.Math.Max(DriveMin, _drive1 - d);
        }
        if (Input.IsPhysicalKeyPressed(Key.W))
        {
            _drive2 = System.Math.Min(DriveMax, _drive2 + d);
        }
        if (Input.IsPhysicalKeyPressed(Key.S))
        {
            _drive2 = System.Math.Max(DriveMin, _drive2 - d);
        }

        // --- advance the circuit ---
        double dtMs = delta * 1000.0 * SlowMotion;
        _f1 = _in1.Step(_drive1, dtMs);
        _f2 = _in2.Step(_drive2, dtMs);

        // THE WHOLE IDEA: both synaptic currents pool into one number before the membrane
        // ever sees them. That '+' is summation.
        double total = _syn1.Step(_f1, dtMs) + _syn2.Step(_f2, dtMs);
        _fout = _out.Step(total, dtMs);

        _readout.Text =
            $"Input 1 drive: {_drive1, 4:0.0}  (↑/↓)        Input 2 drive: {_drive2, 4:0.0}  (W/S)";
        QueueRedraw();
    }

    // ---- Drawing ------------------------------------------------------------------
    private const float VLow = -75f,
        VHigh = -45f,
        VThreshold = -50f;
    private const float CurrentLow = 0f,
        CurrentHigh = 80f; // summed current; ~2*weight peak headroom
    private const float PlotTop = 90f,
        PlotBottom = 380f,
        BarWidth = 60f;

    // Four bars: the two inputs, the pooled current, the output.
    private const float In1X = 80f,
        In2X = 220f,
        SumX = 380f,
        OutX = 540f;

    public override void _Draw()
    {
        DrawThreshold(In1X);
        DrawBar(In1X, ValueToY((float)_in1.V, VLow, VHigh), _f1 ? Colors.White : Colors.SkyBlue);

        DrawThreshold(In2X);
        DrawBar(In2X, ValueToY((float)_in2.V, VLow, VHigh), _f2 ? Colors.White : Colors.SkyBlue);

        // The pooled current = syn1.Current + syn2.Current, the thing the output integrates.
        float sum = (float)(_syn1.Current + _syn2.Current);
        DrawBar(SumX, ValueToY(sum, CurrentLow, CurrentHigh), Colors.Orange);

        DrawThreshold(OutX);
        DrawBar(
            OutX,
            ValueToY((float)_out.V, VLow, VHigh),
            _fout ? Colors.White : Colors.LimeGreen
        );
    }

    private void DrawBar(float x, float y, Color color) =>
        DrawRect(new Rect2(x, y, BarWidth, PlotBottom - y), color);

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
