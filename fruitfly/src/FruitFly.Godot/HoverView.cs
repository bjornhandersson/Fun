using FruitFly.Living; // the FlyingBody lives here; this file only DRAWS it
using Godot;

// Play 8 — Hover: the wingbeat holds the body up (Plan 0008). PURE VIEWER of a FruitFly.Living.
// FlyingBody. Side view: gravity pulls the fly down, its wingbeat makes lift, and a reflex — sense
// dropping → beat harder — holds it aloft. SHOVE it with ↓/Space (down) or ↑ (up) and watch it
// recover on its own; the hold is the neural loop, not a script. (Esc = menu.)
public partial class HoverView : Node2D
{
    private readonly FlyingBody _body = new(startAltitude: StartAlt, reflex: true);

    private const float FloorY = 560f; // screen y of the floor (altitude 0)
    private const float FlyX = 300f; // where the fly sits horizontally in the side view
    private const float AltScale = 0.70f; // screen px per px of altitude
    private const float StartAlt = 320f;
    private const float MaxAlt = 700f; // top of the altitude trace's scale
    private const double ShoveDown = -160.0; // velocity impulse from a downward shove (px/s)
    private const float FoodRate = 240f; // px/s the food moves while ↑/↓ is held
    private const float FoodMin = 60f,
        FoodMax = 660f;
    private float _foodAlt = StartAlt; // the food's height — the fly smells it and flies to it

    private const int HistLen = 360;
    private readonly float[] _altHist = new float[HistLen];
    private int _head;

    private Label _readout = null!;

    public override void _Ready()
    {
        for (int i = 0; i < HistLen; i++)
        {
            _altHist[i] = StartAlt; // prefill so the trace starts flat at the hover line
        }

        var title = new Label
        {
            Text =
                "Play 8 — Hover & seek.  The wingbeat lifts against gravity; a reflex holds altitude; and the fly "
                + "SMELLS the food and climbs to it.   ↑ / ↓ = move the food,   Space = shove the fly.     (Esc = menu)",
            Position = new Vector2(24, 22),
            Size = new Vector2(1100, 30),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        AddChild(title);

        _body.SeekAltitude(_foodAlt); // the fly seeks the food's height from the start

        _readout = new Label { Position = new Vector2(24, 56) };
        _readout.AddThemeFontSizeOverride("font_size", 16);
        AddChild(_readout);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return;
        }
        if (key.Keycode == Key.Escape)
        {
            GetTree().ChangeSceneToFile("res://PlayMenu.tscn");
        }
        else if (key.Keycode == Key.Space)
        {
            _body.Nudge(ShoveDown); // an external shove — the reflex still has to recover
        }
    }

    public override void _Process(double delta)
    {
        // ↑/↓ move the food (held keys → smooth). The fly smells it and climbs the gradient to it.
        if (Input.IsPhysicalKeyPressed(Key.Up))
        {
            _foodAlt = Mathf.Min(FoodMax, _foodAlt + FoodRate * (float)delta);
        }
        if (Input.IsPhysicalKeyPressed(Key.Down))
        {
            _foodAlt = Mathf.Max(FoodMin, _foodAlt - FoodRate * (float)delta);
        }
        _body.SeekAltitude(_foodAlt);

        _body.Step(delta); // the body thinks (beats, senses, corrects) and moves; we just advance it
        _altHist[_head] = (float)_body.Altitude;
        _head = (_head + 1) % HistLen;
        _readout.Text =
            $"fly {_body.Altitude:0}px   food {_foodAlt:0}px   speed {_body.VerticalVelocity:+0;-0}px/s   "
            + $"beat vigour {_body.BeatVigor:0.00}"; // no "command" readout: there is no command
        // variable any more — the correction is synaptic current, visible in the sense gauges
        QueueRedraw();
    }

    public override void _Draw()
    {
        Font font = ThemeDB.FallbackFont;

        // --- side-view world (left): floor, the hover reference, and the fly at its altitude ---
        DrawLine(new Vector2(40, FloorY), new Vector2(560, FloorY), new Color(1, 1, 1, 0.4f), 2f);
        DrawString(font, new Vector2(46, FloorY + 22), "floor", HorizontalAlignment.Left, -1, 14,
            new Color(1, 1, 1, 0.5f));

        // The food: a soft yellow glow at its height, so its "smell" reads as a gradient.
        float foodY = FloorY - _foodAlt * AltScale;
        var foodPos = new Vector2(FlyX, foodY);
        for (int i = 4; i >= 1; i--)
        {
            DrawCircle(foodPos, i * 16f, new Color(1f, 0.9f, 0.2f, 0.05f));
        }
        DrawCircle(foodPos, 9f, new Color(1f, 0.85f, 0.1f));

        float flyY = FloorY - (float)_body.Altitude * AltScale;
        DrawFly(new Vector2(FlyX, flyY), (float)_body.WingPhase);

        // --- gauges (right): what the reflex is doing right now ---
        DrawGauge(font, 620f, "LIFT", (float)(_body.BeatVigor / 0.45), Colors.LimeGreen);
        DrawGauge(font, 720f, "drop sense", (float)_body.DescentSense, Colors.OrangeRed);
        DrawGauge(font, 820f, "rise sense", (float)_body.AscentSense, new Color(0.4f, 0.9f, 1f));

        // --- altitude over time (right, lower) ---
        DrawTrace(font, 620f, 360f, 470f, 170f);
    }

    private void DrawFly(Vector2 p, float phase)
    {
        float tilt = Mathf.Clamp(phase, -1f, 1f) * 0.7f; // wings flap with the beat
        const float len = 28f;
        Vector2 lt = p + new Vector2(-Mathf.Cos(tilt), -Mathf.Sin(tilt)) * len;
        Vector2 rt = p + new Vector2(Mathf.Cos(tilt), -Mathf.Sin(tilt)) * len;
        Color wing = new Color(1f, 0.55f, 1f).Lerp(new Color(0.4f, 0.9f, 1f), (phase + 1f) * 0.5f);
        DrawLine(p, lt, wing, 5f);
        DrawLine(p, rt, wing, 5f);
        DrawCircle(p, 9f, Colors.WhiteSmoke);
    }

    private void DrawGauge(Font font, float x, string label, float value01, Color color)
    {
        const float top = 150f,
            w = 56f,
            h = 150f;
        float v = Mathf.Clamp(value01, 0f, 1f);
        DrawRect(new Rect2(x, top, w, h), new Color(1, 1, 1, 0.08f));
        DrawRect(new Rect2(x, top + h * (1f - v), w, h * v), color);
        float lw = font.GetStringSize(label, HorizontalAlignment.Left, -1, 14).X;
        DrawString(font, new Vector2(x + w * 0.5f - lw * 0.5f, top + h + 20f), label,
            HorizontalAlignment.Left, -1, 14, new Color(1, 1, 1, 0.7f));
    }

    private void DrawTrace(Font font, float x, float y, float w, float h)
    {
        DrawRect(new Rect2(x, y, w, h), new Color(1, 1, 1, 0.05f));
        float colW = w / HistLen;
        Vector2 prev = Vector2.Zero;
        bool have = false;
        for (int i = 0; i < HistLen; i++)
        {
            int idx = (_head + i) % HistLen; // oldest left, newest right
            float a = Mathf.Clamp(_altHist[idx], 0f, MaxAlt);
            var cur = new Vector2(x + i * colW, y + h - a / MaxAlt * h);
            if (have)
            {
                DrawLine(prev, cur, Colors.Gold, 2f);
            }
            prev = cur;
            have = true;
        }
        float ry = y + h - Mathf.Clamp(_foodAlt, 0f, MaxAlt) / MaxAlt * h;
        DrawDashedLine(new Vector2(x, ry), new Vector2(x + w, ry), new Color(1f, 0.85f, 0.2f, 0.4f), 1f);
        DrawString(font, new Vector2(x + 8f, y + 20f), "altitude over time (dashed = food)",
            HorizontalAlignment.Left, -1, 14, new Color(1, 1, 1, 0.6f));
    }
}
