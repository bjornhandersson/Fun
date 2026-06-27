using FruitFly.Living; // the assembled creature lives here now, not in this view
using Godot;
using Vec = System.Numerics.Vector2; // the creature speaks System.Numerics; we convert at the edges

// Play 6 — Fruit fly + memory + nociception.  PURE VIEWER (ADR 0005).
//
// All the brain and body now live in FruitFly.Living (Fly + World). This file does only what a
// viewer should: build the creature, Step it each frame, read its state, and draw it. There is
// no Network, no synapse, no body math here — if you want to change how the fly THINKS, edit
// FruitFly.Living, not this file.
//
// What you're looking at: a fly that seeks a banana (smell → motors, crossed), avoids walls by
// TOUCH (contact receptors → motors, uncrossed) — the orange bars snap full only when an antenna
// actually touches a wall — and, when it grinds head-on, LATCHES a memory of being stuck and
// drives a committed inhibitory turn to break free, then releases via fatigue. The red "noci" bar
// fires only on a real ram. SPACE pokes the memory by hand.
public partial class BraitenbergFlyMemoryView : Node2D
{
    private World _world = null!;
    private Fly _fly = null!;
    private Font _font = null!; // for on-canvas labels under the meters

    public override void _Ready()
    {
        Vector2 size = GetViewportRect().Size;
        _world = new World(new Vec(size.X, size.Y));
        _fly = new Fly(new Vec(size.X * 0.5f, size.Y * 0.5f)); // start in the middle, facing +x
        _font = ThemeDB.FallbackFont;

        var title = new Label
        {
            Text =
                "Play 6 — Fruit fly + memory.  Drive head-on into a wall: the memory LATCHES, drives a committed turn that breaks the deadlock, then releases.   SPACE = poke memory · Esc = menu",
            Position = new Vector2(24, 18),
            Size = new Vector2(size.X - 48, 30),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        AddChild(title);
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
        QueueRedraw();
    }

    // ---- Drawing: read the creature's state, render it ------------------------------
    private static Vector2 G(Vec v) => new(v.X, v.Y); // System.Numerics → Godot

    // ---- Brain-panel layout (one place to retune sizes) -----------------------------
    private const float PanelX = 24f; // left edge of the HUD panel
    private const float PanelTop = 64f; // below the title
    private const float BarW = 46f; // each meter bar
    private const float BarH = 120f; // tall enough to read at a glance
    private const float BarTop = PanelTop + 52f; // bars start below the group headers
    private const float Pair = 56f; // L/R spacing inside a group
    private const float Gap = 38f; // spacing between groups

    public override void _Draw()
    {
        Vector2 banana = G(_world.Banana);
        Vector2 pos = G(_fly.Position);
        float h = _fly.HeadingRadians;

        // Banana: a soft glow + a yellow blob, so its "smell" reads as a gradient.
        for (int i = 5; i >= 1; i--)
        {
            DrawCircle(banana, i * 34f, new Color(1f, 0.9f, 0.2f, 0.05f));
        }
        DrawCircle(banana, 18f, new Color(1f, 0.85f, 0.1f));

        DrawFly(pos, h);
        DrawBrainPanel();
    }

    // The creature: a bigger arrow along its heading, antennae that light up RED on contact.
    private void DrawFly(Vector2 pos, float h)
    {
        Vector2 nose = pos + 26f * Heading(h);
        Vector2 tailL = pos + 20f * Heading(h + 2.5f);
        Vector2 tailR = pos + 20f * Heading(h - 2.5f);
        DrawColoredPolygon(new[] { nose, tailL, tailR }, Colors.WhiteSmoke);
        DrawPolyline(new[] { tailL, nose, tailR }, new Color(0, 0, 0, 0.35f), 2f); // crisp outline

        // Antennae: small blue dots normally; they flare RED + larger while actually touching a wall.
        DrawAntenna(G(_fly.AntennaLeft), _fly.TouchL > 0.5);
        DrawAntenna(G(_fly.AntennaRight), _fly.TouchR > 0.5);
    }

    private void DrawAntenna(Vector2 at, bool touching)
    {
        if (touching)
        {
            DrawCircle(at, 11f, new Color(1f, 0.3f, 0.1f, 0.30f)); // contact halo
            DrawCircle(at, 6f, Colors.OrangeRed);
        }
        else
        {
            DrawCircle(at, 5f, Colors.SkyBlue);
        }
    }

    // The live "tiny brain": grouped, labelled meters so each signal is identifiable at a glance.
    private void DrawBrainPanel()
    {
        // A dim backing card so text/bars read over the world behind them. Walk the SAME layout
        // the groups use below to find the true right edge, so the card always wraps every bar.
        const int pairs = 3; // SMELL, TOUCH, WINGS
        const int singles = 2; // MEMORY, NOCI
        float contentRight =
            PanelX + pairs * (Pair + BarW + Gap) + singles * (BarW + Gap) - Gap; // no trailing gap
        const float padX = 16f;
        float cardLeft = PanelX - padX;
        DrawRect(
            new Rect2(cardLeft, PanelTop - 12f, contentRight + padX - cardLeft, BarH + 170f),
            new Color(0.05f, 0.06f, 0.09f, 0.55f)
        );

        float x = PanelX;
        x = DrawGroup(x, "SMELL", "L", "R",
            (float)_fly.SmellL, (float)_fly.SmellR, Colors.SkyBlue, percent: false);
        x = DrawGroup(x, "TOUCH", "L", "R",
            (float)_fly.TouchL, (float)_fly.TouchR, Colors.OrangeRed, percent: false, onOff: true);
        x = DrawGroup(x, "WINGS", "L", "R",
            (float)_fly.WingL, (float)_fly.WingR, Colors.LimeGreen, percent: true);
        x = DrawGroup(x, "MEMORY", "hold", null,
            (float)_fly.MemoryActivity, 0f, Colors.Magenta, percent: true);
        x = DrawGroup(x, "NOCI", "ouch", null,
            (float)_fly.Nociception, 0f, new Color(1f, 0.25f, 0.25f), percent: true);

        // Steering bar: where the brain is pushing the fly THIS instant.
        float steerY = BarTop + BarH + 40f;
        DrawSteering(PanelX, steerY, x - PanelX - Gap - 12f, 22f, (float)_fly.TurnSignal);
    }

    // Draw one group of 1–2 bars with a header and per-bar caption+value. Returns the next x.
    private float DrawGroup(
        float x, string header, string capA, string? capB,
        float a, float b, Color color, bool percent, bool onOff = false)
    {
        bool pair = capB != null;
        float span = pair ? Pair + BarW : BarW;
        float centerX = x + span * 0.5f;

        DrawCentered(header, centerX, PanelTop + 14f, 16, new Color(1, 1, 1, 0.85f));
        DrawBar(x, a, color, capA, percent, onOff);
        if (pair)
        {
            DrawBar(x + Pair, b, color, capB!, percent, onOff);
        }
        return x + span + Gap;
    }

    private void DrawBar(float x, float value01, Color color, string caption, bool percent, bool onOff)
    {
        float v = Mathf.Clamp(value01, 0f, 1f);
        DrawRect(new Rect2(x, BarTop, BarW, BarH), new Color(1, 1, 1, 0.08f)); // track
        DrawRect(new Rect2(x, BarTop + BarH * (1f - v), BarW, BarH * v), color); // fill from bottom
        DrawRect(new Rect2(x, BarTop, BarW, BarH), new Color(1, 1, 1, 0.12f), filled: false, width: 1f);

        string val = onOff ? (v > 0.5f ? "ON" : "·") : percent ? $"{v * 100f:0}%" : $"{v:0.00}";
        DrawCentered(val, x + BarW * 0.5f, BarTop - 8f, 15, Colors.White);
        DrawCentered(caption, x + BarW * 0.5f, BarTop + BarH + 20f, 14, new Color(1, 1, 1, 0.7f));
    }

    // A center-zero steering meter: fills LEFT or RIGHT from the middle, with end labels.
    private void DrawSteering(float x, float y, float w, float h, float signed)
    {
        float s = Mathf.Clamp(signed, -1f, 1f);
        float cx = x + w * 0.5f;
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
        DrawLine(new Vector2(cx, y - 3f), new Vector2(cx, y + h + 3f), new Color(1, 1, 1, 0.45f), 2f);
        DrawString(_font, new Vector2(x, y - 8f), "◄ turn left", HorizontalAlignment.Left, -1, 14,
            new Color(1, 1, 1, 0.7f));
        var rt = "turn right ►";
        float rtW = _font.GetStringSize(rt, HorizontalAlignment.Left, -1, 14).X;
        DrawString(_font, new Vector2(x + w - rtW, y - 8f), rt, HorizontalAlignment.Left, -1, 14,
            new Color(1, 1, 1, 0.7f));
    }

    private void DrawCentered(string text, float centerX, float baselineY, int fontSize, Color color)
    {
        float w = _font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize).X;
        DrawString(_font, new Vector2(centerX - w * 0.5f, baselineY), text,
            HorizontalAlignment.Left, -1, fontSize, color);
    }

    private static Vector2 Heading(float angle) => new(Mathf.Cos(angle), Mathf.Sin(angle));
}
