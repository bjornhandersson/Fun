using System;
using FruitFly; // LifNeuron + Synapse + Network from FruitFly.Core
using Godot;

// Play 5b — Memory: one self-exciting neuron (Plan 0002).
//
// The cleanest possible memory: ONE neuron wired to ITSELF. Its own spikes loop back and
// re-trigger it, so it keeps firing after the poke that started it is gone — and a slow
// fatigue current (adaptation) eventually tires it out, so it stops on its own. Poke → hold
// → release, with nothing scripted.
//
// Memory is about TIME ("it kept firing AFTER I let go"), so we draw it as a scrolling
// TIMELINE, not instantaneous bars. Newest is at the right. Two things are plotted:
//   • a BLUE band along the top  = when YOU are poking it (SPACE held)
//   • a MAGENTA fill             = how hard the neuron is firing
// The memory is literally VISIBLE as a gap: the magenta keeps going to the RIGHT of where the
// blue band ends. That overhang — firing with no poke — IS the held memory.
public partial class MemoryNeuronView : Node2D
{
    private readonly Network _net = new();

    // The one cell. An ordinary LifNeuron; the memory comes from the self-wire below, not a
    // special neuron. We switch on its adaptation (fatigue) via AdaptKick/TauAdapt so the latch
    // can release itself. Otherwise default LIF: rest -65, fire -50, leak Tau = 10 ms.
    private readonly LifNeuron _mem = new() { AdaptKick = MemAdaptKick, TauAdapt = MemAdaptTau };
    private readonly Synapse _self; // the one wire: _mem -> _mem

    public MemoryNeuronView()
    {
        _net.Add(_mem);
        _self = _net.Connect(_mem, _mem, SelfWeight); // THE wire: source and target are the same neuron
    }

    // ---- The three knobs that shape the memory --------------------------------------------
    // SelfWeight = the LATCH: how hard each spike re-excites the neuron. High enough to hold.
    private const double SelfWeight = 60.0;

    // AdaptKick + TauAdapt = the RELEASE (fatigue). SMALL kick so the brake builds up SLOWLY over
    // many spikes → a long, watchable hold before it finally wins. TauAdapt = how long it stays
    // tired afterwards. (Bigger kick = shorter hold; smaller kick = longer hold or none at all.)
    private const double MemAdaptKick = 0.6;
    private const double MemAdaptTau = 400.0;

    private const double KickDrive = 25.0; // external current injected while SPACE is held
    private const double SlowMotion = 0.1; // run the brain at 1/10 speed so the hold is watchable

    // ---- Smoothed "is it firing" activity, 0..1 (for the magenta trace) --------------------
    private const double ActTauMs = 30.0; // how fast the activity trace follows the spikes
    private const double ActKick = 0.1; // per-spike bump; tuned so steady firing sits near 1.0
    private double _act; // the smoothed activity itself

    // ---- Scrolling history (a ring buffer of the last few seconds) -------------------------
    private const int HistLen = 300; // frames kept ≈ 5 s at 60 fps
    private readonly float[] _actHist = new float[HistLen]; // firing intensity per frame
    private readonly bool[] _inputHist = new bool[HistLen]; // was SPACE held that frame
    private int _head; // next slot to write (oldest data sits just after it)

    private bool _kicking;
    private double _sinceSpikeMs = 1e9; // ms since the last spike; starts "long ago" = silent
    private Label _readout = null!;

    public override void _Ready()
    {
        AddChild(
            new Label
            {
                Text =
                    "Play 5b — Memory neuron.  TAP SPACE then let go.  Blue = your poke, magenta = firing.  "
                    + "MEMORY = magenta keeps going AFTER the blue stops.     (Esc = menu)",
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
        // Poke the neuron while SPACE is held; release removes ALL external input, so anything
        // still firing afterwards can ONLY be the neuron sustaining itself.
        _kicking = Input.IsPhysicalKeyPressed(Key.Space);
        _net.SetInput(_mem, _kicking ? KickDrive : 0.0);

        double dtMs = delta * 1000.0 * SlowMotion;
        _net.Step(dtMs);
        bool fired = _net.Fired(_mem);

        // Smooth the spikes into a 0..1 "how hard is it firing" trace.
        _act += (dtMs / ActTauMs) * (-_act);
        if (fired)
        {
            _act += ActKick;
        }
        float act01 = (float)Math.Clamp(_act, 0.0, 1.0);

        // Record this frame into the scrolling history.
        _inputHist[_head] = _kicking;
        _actHist[_head] = act01;
        _head = (_head + 1) % HistLen;

        // Plain-words status: is it firing, and is anyone poking it?
        _sinceSpikeMs = fired ? 0.0 : _sinceSpikeMs + dtMs;
        bool firing = _sinceSpikeMs < 40.0;
        _readout.Text = _kicking
            ? "driving  —  you are poking it"
            : firing
                ? "MEMORY HOLDING  —  firing with NO input"
                : "at rest";
        QueueRedraw();
    }

    // ---- Drawing: the scrolling timeline ---------------------------------------------------
    private const float StripX = 60f,
        StripY = 100f,
        StripW = 640f,
        StripH = 240f,
        InputLaneH = 16f; // the blue "poke" band along the top

    public override void _Draw()
    {
        // Backdrop for the plot.
        DrawRect(new Rect2(StripX, StripY, StripW, StripH), new Color(1, 1, 1, 0.05f));

        float colW = StripW / HistLen;
        for (int i = 0; i < HistLen; i++)
        {
            int idx = (_head + i) % HistLen; // oldest on the left, newest on the right
            float x = StripX + i * colW;

            // Magenta firing fill, growing UP from the bottom by how hard it's firing.
            float a = _actHist[idx];
            if (a > 0f)
            {
                float fh = (StripH - InputLaneH) * a;
                DrawRect(new Rect2(x, StripY + StripH - fh, colW + 1f, fh), Colors.Magenta);
            }

            // Blue poke band along the very top, only where YOU were holding SPACE.
            if (_inputHist[idx])
            {
                DrawRect(new Rect2(x, StripY, colW + 1f, InputLaneH), Colors.SkyBlue);
            }
        }

        // "now" line at the right edge — time flows left → right into this edge.
        float nowX = StripX + StripW;
        DrawLine(new Vector2(nowX, StripY), new Vector2(nowX, StripY + StripH), new Color(1, 1, 1, 0.4f));
    }
}
