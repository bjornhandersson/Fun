using FruitFly; // LifNeuron + Synapse + Network from FruitFly.Core
using Godot;

// Play 5 — Loop (the first flicker of memory).
//
// Two neurons wired into a LOOP: A excites B, and B excites A. There is no "source first"
// order here — they depend on each other — so this only works because the Network updates
// everyone simultaneously with a one-tick transmission delay.
//
// Hold SPACE to kick A with a little external drive. Then LET GO: because each partner's
// spike re-fires the other across the loop, the activity sustains itself with no input at
// all. That self-sustaining reverberation is the substrate of short-term memory — a circuit
// "holding" something after the cause is gone. Positive feedback (Play "self-accelerating")
// closing on itself.
public partial class LoopView : Node2D
{
    private readonly Network _net = new();
    private readonly LifNeuron _a = new();
    private readonly LifNeuron _b = new();
    private readonly Synapse _ab; // A -> B
    private readonly Synapse _ba; // B -> A

    public LoopView()
    {
        _net.Add(_a);
        _net.Add(_b);
        // weight 95: strong enough that one partner's spike reliably re-fires the other from
        // its reset level, so once lit the two keep triggering each other around the loop.
        _ab = _net.Connect(_a, _b, 95.0);
        _ba = _net.Connect(_b, _a, 95.0);
    }

    private const double KickDrive = 25.0; // external current into A while SPACE is held
    private const double SlowMotion = 0.1;

    private bool _kicking;
    private Label _readout = null!;

    public override void _Ready()
    {
        AddChild(
            new Label
            {
                Text =
                    "Play 5 — Loop:  A ⇄ B excite each other.  Hold SPACE to kick A, then release.     (Esc = menu)",
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
        // SPACE injects external drive into A; releasing it removes ALL external input, so
        // anything that keeps firing afterwards is the loop sustaining itself.
        _kicking = Input.IsPhysicalKeyPressed(Key.Space);
        _net.SetInput(_a, _kicking ? KickDrive : 0.0);

        _net.Step(delta * 1000.0 * SlowMotion);

        _readout.Text = _kicking
            ? "kicking A  (external drive ON)"
            : "free-running  (no external input — anything firing is the loop remembering)";
        QueueRedraw();
    }

    // ---- Drawing: A → (A→B current) → B → (B→A current), the signal going around the loop ----
    private const float VLow = -75f,
        VHigh = -45f,
        VThreshold = -50f;
    private const float CurrentLow = 0f,
        CurrentHigh = 110f; // excitatory only; ~95 peak + headroom
    private const float PlotTop = 90f,
        PlotBottom = 380f,
        BarWidth = 60f;

    private const float Ax = 80f,
        AbX = 240f,
        Bx = 400f,
        BaX = 560f;

    public override void _Draw()
    {
        DrawThreshold(Ax);
        DrawBar(
            Ax,
            ValueToY((float)_a.V, VLow, VHigh),
            _net.Fired(_a) ? Colors.White : Colors.SkyBlue
        );

        DrawBar(AbX, ValueToY((float)_ab.Current, CurrentLow, CurrentHigh), Colors.Orange); // A -> B current

        DrawThreshold(Bx);
        DrawBar(
            Bx,
            ValueToY((float)_b.V, VLow, VHigh),
            _net.Fired(_b) ? Colors.White : Colors.SkyBlue
        );

        DrawBar(BaX, ValueToY((float)_ba.Current, CurrentLow, CurrentHigh), Colors.Orange); // B -> A current
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
