using FruitFly.Living; // the assembled creature lives here now, not in this view
using Godot;
using Vec = System.Numerics.Vector2; // the creature speaks System.Numerics; we convert at the edges

// Play 6b — Fruit fly + memory + nociception.  PURE VIEWER (ADR 0005).
//
// All the brain and body now live in FruitFly.Living (Fly + World). This file does only what a
// viewer should: build the creature, Step it each frame, read its state, and draw it. There is
// no Network, no synapse, no body math here — if you want to change how the fly THINKS, edit
// FruitFly.Living, not this file.
//
// What you're looking at: a fly that seeks a banana (smell → motors, crossed), avoids walls
// (wall sensors → motors, uncrossed), and — when it grinds head-on into a wall — LATCHES a
// memory of being stuck and drives a committed inhibitory turn to break free, then releases via
// fatigue. The red "noci" bar fires only on a real ram. SPACE pokes the memory by hand.
public partial class BraitenbergFlyMemoryView : Node2D
{
    private World _world = null!;
    private Fly _fly = null!;
    private Label _readout = null!;

    public override void _Ready()
    {
        Vector2 size = GetViewportRect().Size;
        _world = new World(new Vec(size.X, size.Y));
        _fly = new Fly(new Vec(size.X * 0.5f, size.Y * 0.5f)); // start in the middle, facing +x

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
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Space })
        {
            _fly.Poke();
        }
    }

    public override void _Process(double delta)
    {
        _fly.Step(_world, delta); // the creature thinks and moves itself; we just advance it

        _readout.Text =
            $"smell L/R: {_fly.SmellL:0.00}/{_fly.SmellR:0.00}    wall L/R: {_fly.WallL:0.00}/{_fly.WallR:0.00}    wheels L/R: {_fly.WheelL:0.00}/{_fly.WheelR:0.00}    compensating: {_fly.TurnSignal:+0.00;-0.00; 0.00}";
        QueueRedraw();
    }

    // ---- Drawing: read the creature's state, render it ------------------------------
    private static Vector2 G(Vec v) => new(v.X, v.Y); // System.Numerics → Godot

    public override void _Draw()
    {
        Vector2 banana = G(_world.Banana);
        Vector2 pos = G(_fly.Position);
        float h = _fly.HeadingRadians;

        // Banana: a soft glow + a yellow blob, so its "smell" reads as a gradient.
        for (int i = 4; i >= 1; i--)
        {
            DrawCircle(banana, i * 26f, new Color(1f, 0.9f, 0.2f, 0.05f));
        }
        DrawCircle(banana, 13f, new Color(1f, 0.85f, 0.1f));

        // Fly: a triangle along its heading, with its two antenna dots.
        Vector2 nose = pos + 16f * Heading(h);
        Vector2 tailL = pos + 12f * Heading(h + 2.5f);
        Vector2 tailR = pos + 12f * Heading(h - 2.5f);
        DrawColoredPolygon(new[] { nose, tailL, tailR }, Colors.WhiteSmoke);
        DrawCircle(G(_fly.AntennaLeft), 3f, Colors.SkyBlue);
        DrawCircle(G(_fly.AntennaRight), 3f, Colors.SkyBlue);

        // Tiny brain panel: sensor drive, motor activation, memory and nociceptor — the live
        // dual-view in miniature. (Values come straight off the creature; no recompute here.)
        DrawMiniBar(20, 70, (float)_fly.SmellL, Colors.SkyBlue);
        DrawMiniBar(54, 70, (float)_fly.SmellR, Colors.SkyBlue);
        DrawMiniBar(100, 70, (float)_fly.WheelL, Colors.LimeGreen);
        DrawMiniBar(134, 70, (float)_fly.WheelR, Colors.LimeGreen);
        DrawMiniBar(180, 70, (float)_fly.WallL, Colors.OrangeRed);
        DrawMiniBar(214, 70, (float)_fly.WallR, Colors.OrangeRed);
        DrawMiniBar(260, 70, (float)_fly.MemoryActivity, Colors.Magenta); // held activity = the memory
        DrawMiniBar(300, 70, (float)_fly.Nociception, new Color(1f, 0.15f, 0.15f)); // red "ouch" on a real ram

        // Compensation needle: the net steering the brain is producing this instant.
        DrawCompensationBar(20, 140, 218, 16, (float)_fly.TurnSignal);
    }

    private static Vector2 Heading(float angle) => new(Mathf.Cos(angle), Mathf.Sin(angle));

    private void DrawCompensationBar(float x, float y, float w, float h, float signed)
    {
        float s = Mathf.Clamp(signed, -1f, 1f);
        float cx = x + w * 0.5f; // the zero line: dead-straight flight
        float half = w * 0.5f;
        DrawRect(new Rect2(x, y, w, h), new Color(1, 1, 1, 0.08f)); // track
        if (s >= 0f)
        {
            DrawRect(new Rect2(cx, y, half * s, h), Colors.Gold);
        }
        else
        {
            DrawRect(new Rect2(cx + half * s, y, -half * s, h), Colors.Gold);
        }
        DrawLine(new Vector2(cx, y), new Vector2(cx, y + h), new Color(1, 1, 1, 0.35f)); // center tick
    }

    private void DrawMiniBar(float x, float top, float value01, Color color)
    {
        const float h = 50f,
            w = 24f;
        float v = Mathf.Clamp(value01, 0f, 1f);
        DrawRect(new Rect2(x, top, w, h), new Color(1, 1, 1, 0.08f)); // track
        DrawRect(new Rect2(x, top + h * (1f - v), w, h * v), color); // fill from bottom
    }
}
