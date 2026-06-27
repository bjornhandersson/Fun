using System.Numerics;

namespace FruitFly.Living;

// The WORLD the fly lives in: a banana to seek and four walls to avoid. Pure geometry and
// scalar fields — no brain here, and (per ADR 0005) no Godot types. The fly QUERIES this:
// "how strong is the smell here?", "am I touching a wall here?", "clamp me inside", "did I eat?".
public sealed class World
{
    // Play-field bounds (world units). The SAME numbers feed both the wall-proximity field and
    // the hard clamp, so the fly feels exactly the wall it cannot cross.
    private const float MinX = 20f,
        MinY = 70f,
        Margin = 20f;

    private const double SmellStrength = 1.0; // concentration at the banana itself
    private const double SmellFalloff = 420.0; // px at which smell halves — big, reaches across the field
    private const float EatRadius = 42f; // within this the banana is eaten and respawns
    private const float CornerInset = 90f; // how far in from a corner a (re)spawned banana sits

    private readonly Random _rng;

    public Vector2 Size { get; }
    public Vector2 Banana { get; private set; }

    public World(Vector2 size, Random? rng = null)
    {
        _rng = rng ?? new Random();
        Size = size;
        Banana = new Vector2(size.X - CornerInset, CornerInset); // start in a corner (top-right)
    }

    // Smooth odor field: 1 at the banana, halving every SmellFalloff, never quite 0.
    public double Smell(Vector2 at)
    {
        double r = Vector2.Distance(at, Banana) / SmellFalloff;
        return SmellStrength / (1.0 + r * r);
    }

    // Is the point `at` physically TOUCHING a wall? Zero-range CONTACT, not distance: a bristle
    // either is deflected by the surface or it isn't — there is no "how near". True when `at` has
    // reached or crossed any of the four bounds — the SAME bounds Clamp() enforces, so an antenna
    // touches exactly the wall the body cannot cross. (Antennae reach past the body, so they make
    // contact while the body centre is still inside.) This is the honest replacement for the old
    // god's-eye proximity field: nothing on a fly can sense distance-to-wall, but a bristle can
    // sense touch.
    public bool Touching(Vector2 at)
    {
        return at.X <= MinX
            || at.X >= Size.X - Margin
            || at.Y <= MinY
            || at.Y >= Size.Y - Margin;
    }

    // Keep a body inside the field. Returns the clamped position and reports whether the wall
    // actually had to STOP it — that "the world physically blocked me" event is a real collision,
    // which is exactly what the fly's nociceptor reports.
    public Vector2 Clamp(Vector2 pos, out bool collided)
    {
        var clamped = new Vector2(
            Math.Clamp(pos.X, MinX, Size.X - Margin),
            Math.Clamp(pos.Y, MinY, Size.Y - Margin)
        );
        collided = clamped != pos;
        return clamped;
    }

    // If `pos` is on the banana, eat it (respawn in a fresh random corner) and return true.
    public bool TryEat(Vector2 pos)
    {
        if (Vector2.Distance(pos, Banana) >= EatRadius)
        {
            return false;
        }
        float x = _rng.NextDouble() < 0.5 ? CornerInset : Size.X - CornerInset;
        float y = _rng.NextDouble() < 0.5 ? CornerInset : Size.Y - CornerInset;
        Banana = new Vector2(x, y);
        return true;
    }
}
