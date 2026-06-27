using FruitFly.Living;
using Godot;

// A 2D overlay that draws the united fly's LIVING SIGNALS over the 3D scene (Plan 0009, step ④).
// Same idea as Play 6's brain panel — grouped, labelled meters so you can watch the fly THINK —
// but extended with the VERTICAL signals this fly also has: altitude, lift (beat vigour), and climb
// rate. It reads everything from the Fly's public API and owns no brain logic (ADR 0005).
public partial class FlyBrainOverlay : Node2D
{
    public Fly Fly = null!; // set by the view before this enters the tree

    private Font _font = null!;

    public override void _Ready() => _font = ThemeDB.FallbackFont;

    public override void _Process(double delta) => QueueRedraw(); // re-read the fly every frame

    // ---- layout (mirrors Play 6 so the panel feels familiar) ------------------------
    private const float PanelX = 24f;
    private const float PanelTop = 70f;
    private const float BarW = 42f;
    private const float BarH = 110f;
    private const float BarTop = PanelTop + 52f;
    private const float Pair = 52f; // L/R spacing inside a group
    private const float Gap = 34f; // spacing between groups

    public override void _Draw()
    {
        // Backing card sized to wrap every group (3 pairs + 2 singles for the brain, then the
        // vertical trio: LIFT single + CLIMB single + an ALT single).
        const int pairs = 3,
            singles = 5;
        float contentRight = PanelX + pairs * (Pair + BarW + Gap) + singles * (BarW + Gap) - Gap;
        const float padX = 16f;
        float cardLeft = PanelX - padX;
        DrawRect(new Rect2(cardLeft, PanelTop - 12f, contentRight + padX - cardLeft, BarH + 150f),
            new Color(0.05f, 0.06f, 0.09f, 0.6f));

        float x = PanelX;
        x = Group(x, "SMELL", "L", "R", (float)Fly.SmellL, (float)Fly.SmellR, Colors.SkyBlue, percent: false);
        x = Group(x, "TOUCH", "L", "R", (float)Fly.TouchL, (float)Fly.TouchR, Colors.OrangeRed,
            percent: false, onOff: true);
        x = Group(x, "WINGS", "L", "R", (float)Fly.WingL, (float)Fly.WingR, Colors.LimeGreen, percent: true);
        x = Group(x, "MEMORY", "hold", null, (float)Fly.MemoryActivity, 0f, Colors.Magenta, percent: true);
        x = Group(x, "NOCI", "ouch", null, (float)Fly.Nociception, 0f, new Color(1f, 0.25f, 0.25f),
            percent: true);

        // --- the vertical layer: this is the part that's NEW vs Play 6 -----------------
        // LIFT = how hard it's beating (BeatVigor, scaled to its hover band ~0.45).
        x = Group(x, "LIFT", "beat", null, (float)(Fly.BeatVigor / 0.45), 0f,
            new Color(0.5f, 1f, 0.6f), percent: true);
        // ALT = current altitude as a fraction of the world's height band (~520px).
        x = Group(x, "ALT", "height", null, (float)Fly.Altitude / 520f, 0f, Colors.Gold, percent: true);

        // CLIMB = signed vertical speed, a centre-zero meter (up vs down).
        float climbY = BarTop + BarH + 36f;
        Signed(PanelX, climbY, contentRight - PanelX, 20f, (float)Fly.VerticalVelocity / 150f,
            "▼ sinking", "climbing ▲");
    }

    private float Group(float x, string header, string capA, string? capB, float a, float b,
        Color color, bool percent, bool onOff = false)
    {
        bool pair = capB != null;
        float span = pair ? Pair + BarW : BarW;
        Centered(header, x + span * 0.5f, PanelTop + 14f, 15, new Color(1, 1, 1, 0.85f));
        Bar(x, a, color, capA, percent, onOff);
        if (pair)
        {
            Bar(x + Pair, b, color, capB!, percent, onOff);
        }
        return x + span + Gap;
    }

    private void Bar(float x, float value01, Color color, string caption, bool percent, bool onOff)
    {
        float v = Mathf.Clamp(value01, 0f, 1f);
        DrawRect(new Rect2(x, BarTop, BarW, BarH), new Color(1, 1, 1, 0.08f));
        DrawRect(new Rect2(x, BarTop + BarH * (1f - v), BarW, BarH * v), color);
        DrawRect(new Rect2(x, BarTop, BarW, BarH), new Color(1, 1, 1, 0.12f), filled: false, width: 1f);

        string val = onOff ? (v > 0.5f ? "ON" : "·") : percent ? $"{v * 100f:0}%" : $"{v:0.00}";
        Centered(val, x + BarW * 0.5f, BarTop - 8f, 14, Colors.White);
        Centered(caption, x + BarW * 0.5f, BarTop + BarH + 18f, 13, new Color(1, 1, 1, 0.7f));
    }

    private void Signed(float x, float y, float w, float h, float signed, string leftCap, string rightCap)
    {
        float s = Mathf.Clamp(signed, -1f, 1f);
        float cx = x + w * 0.5f,
            half = w * 0.5f;
        DrawRect(new Rect2(x, y, w, h), new Color(1, 1, 1, 0.08f));
        if (s >= 0f)
        {
            DrawRect(new Rect2(cx, y, half * s, h), Colors.Gold);
        }
        else
        {
            DrawRect(new Rect2(cx + half * s, y, -half * s, h), Colors.Gold);
        }
        DrawLine(new Vector2(cx, y - 3f), new Vector2(cx, y + h + 3f), new Color(1, 1, 1, 0.45f), 2f);
        DrawString(_font, new Vector2(x, y - 8f), leftCap, HorizontalAlignment.Left, -1, 13,
            new Color(1, 1, 1, 0.7f));
        float rw = _font.GetStringSize(rightCap, HorizontalAlignment.Left, -1, 13).X;
        DrawString(_font, new Vector2(x + w - rw, y - 8f), rightCap, HorizontalAlignment.Left, -1, 13,
            new Color(1, 1, 1, 0.7f));
    }

    private void Centered(string text, float centerX, float baselineY, int fontSize, Color color)
    {
        float w = _font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize).X;
        DrawString(_font, new Vector2(centerX - w * 0.5f, baselineY), text, HorizontalAlignment.Left,
            -1, fontSize, color);
    }
}
