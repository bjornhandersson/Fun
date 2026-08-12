using FruitFly.Living; // the united Fly lives here; this file only DRAWS it (ADR 0005)
using Godot;
using NVec = System.Numerics.Vector2; // the brain/world speak System.Numerics; Godot speaks its own Vector2/3

// Play 9 — the UNITED fly, in 3D (Plan 0009). PURE VIEWER of a FruitFly.Living.Fly built in 3D mode.
// The same creature you've watched seek a banana on a plane now also CLIMBS to the banana's height on
// its own wingbeat — so it flies through space and eats in 3D. To make it feel ALIVE (like Play 6) we
// show its sensing: antennae that flare on wall contact, a glowing smell aura on the banana, a motion
// trail, a shadow + drop-line so you read its altitude, a gently following camera, and a live brain +
// flight overlay. Honest split: LIFT/altitude is the real wingbeat; FORWARD thrust is still the 2D
// abstraction (pitch is the next rung). (Esc = menu.)
public partial class Fly3DView : Node3D
{
    private World _world = null!;
    private Fly _fly = null!;

    private MeshInstance3D _banana = null!,
        _bananaGlow = null!;
    private Node3D _flyBody = null!; // we MOVE and TURN this; wings + antennae hang off it
    private Node3D _wingHingeL = null!,
        _wingHingeR = null!; // pivots at the shoulders: swept back, and they beat about this hinge
    private MeshInstance3D _antL = null!,
        _antR = null!;

    private const float WingSweep = 0.7f; // radians the wing is swept BACK over the abdomen (vs straight out)
    private const float WingRaise = 0.25f; // resting upward tilt
    private const float WingAmp = 0.95f; // beat amplitude — big, so it sweeps up over the back like a fly
    private MeshInstance3D _shadow = null!,
        _dropline = null!,
        _trail = null!;
    private Camera3D _cam = null!;
    private Vector3 _camTarget; // smoothed point the camera looks at (follows the fly)
    private double _t; // time accumulator, for the banana's gentle smell pulse

    private const int TrailLen = 64;
    private readonly Vector3[] _trailPts = new Vector3[TrailLen];
    private int _trailHead,
        _trailCount;

    // px → scene units. The world is hundreds of px wide; scale down so the camera frames it nicely.
    private const float PxToUnit = 0.02f;
    private float _cx,
        _cz; // world centre, so the scene sits on the origin

    // World plane (wx, wy) + altitude → Godot space: X stays X, the plane's second axis becomes Z,
    // and ALTITUDE becomes Y (up). This one mapping is the whole bridge between brain-space and view.
    private Vector3 Map(float wx, float wy, float alt) =>
        new((wx - _cx) * PxToUnit, alt * PxToUnit, (wy - _cz) * PxToUnit);

    public override void _Ready()
    {
        _world = new World(new NVec(800f, 600f));
        _fly = new Fly(new NVec(400f, 300f), fly3D: true); // released in the middle, in 3D mode
        _cx = _world.Size.X * 0.5f;
        _cz = _world.Size.Y * 0.5f;

        BuildSky();
        BuildGroundAndWalls();
        BuildBanana();
        BuildFly();
        BuildAltitudeCues();
        BuildTrail();
        BuildCameraAndLight();
        BuildHud();
        PlaceBanana();
    }

    private void BuildSky()
    {
        var env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.05f, 0.06f, 0.09f),
            AmbientLightColor = new Color(0.5f, 0.5f, 0.6f),
            AmbientLightEnergy = 0.6f,
        };
        AddChild(new WorldEnvironment { Environment = env });
    }

    private void BuildGroundAndWalls()
    {
        AddChild(new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(_world.Size.X, _world.Size.Y) * PxToUnit },
            MaterialOverride = Flat(new Color(0.15f, 0.17f, 0.20f)),
        });

        float w = _world.Size.X * PxToUnit,
            d = _world.Size.Y * PxToUnit;
        const float h = 6f,
            t = 0.15f;
        Color wall = new(0.30f, 0.34f, 0.42f, 0.25f);
        AddWall(new Vector3(0, h * 0.5f, -d * 0.5f), new Vector3(w, h, t), wall);
        AddWall(new Vector3(0, h * 0.5f, d * 0.5f), new Vector3(w, h, t), wall);
        AddWall(new Vector3(-w * 0.5f, h * 0.5f, 0), new Vector3(t, h, d), wall);
        AddWall(new Vector3(w * 0.5f, h * 0.5f, 0), new Vector3(t, h, d), wall);
    }

    private void AddWall(Vector3 pos, Vector3 size, Color color) =>
        AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = size },
            Position = pos,
            MaterialOverride = Flat(color),
        });

    private void BuildBanana()
    {
        // A translucent aura = the SMELL the fly climbs toward (like Play 6's glow, now a sphere).
        _bananaGlow = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 2.2f, Height = 4.4f },
            MaterialOverride = Flat(new Color(1f, 0.85f, 0.2f, 0.06f)),
        };
        AddChild(_bananaGlow);

        _banana = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.5f, Height = 1.0f },
            MaterialOverride = Glow(new Color(1f, 0.82f, 0.15f)),
        };
        AddChild(_banana);
    }

    private void BuildFly()
    {
        _flyBody = new Node3D();
        AddChild(_flyBody);

        // A dumb little brown fruit fly. Forward is -Z (LookAt points -Z at the target), so the head
        // and big red eyes sit toward -Z and the fat abdomen trails at +Z. Lumpy squashed spheres.
        Color brown = new(0.32f, 0.21f, 0.10f);
        Color darkBrown = new(0.19f, 0.12f, 0.05f);
        AddPart(new Vector3(0f, 0.02f, 0.18f), new Vector3(0.17f, 0.15f, 0.30f), darkBrown); // abdomen
        AddPart(new Vector3(0f, 0.04f, -0.06f), new Vector3(0.16f, 0.16f, 0.18f), brown); // thorax hump
        AddPart(new Vector3(0f, 0.04f, -0.22f), new Vector3(0.11f, 0.11f, 0.11f), brown); // little head
        AddPart(new Vector3(-0.08f, 0.06f, -0.24f), new Vector3(0.08f, 0.09f, 0.08f), default, eye: true);
        AddPart(new Vector3(0.08f, 0.06f, -0.24f), new Vector3(0.08f, 0.09f, 0.08f), default, eye: true);

        // Wings: hinged at the shoulders, swept BACK over the abdomen. They beat about the hinge.
        _wingHingeL = WingHinge(-1);
        _wingHingeR = WingHinge(+1);
        _flyBody.AddChild(_wingHingeL);
        _flyBody.AddChild(_wingHingeR);

        // Antennae out front: tiny stubs that FLARE RED while an antenna touches a wall (the fly
        // is sensing the world). They sit in world space, so they're children of the scene, not the
        // body — we place them each frame from _fly.AntennaLeft/Right.
        _antL = new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.07f, Height = 0.14f } };
        _antR = new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.07f, Height = 0.14f } };
        AddChild(_antL);
        AddChild(_antR);
    }

    // One squashed-sphere body part (scale = its three semi-axes). eye:true = a glossy red bug eye.
    private void AddPart(Vector3 pos, Vector3 semi, Color color, bool eye = false) =>
        _flyBody.AddChild(new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 1f, Height = 2f },
            Position = pos,
            Scale = semi,
            MaterialOverride = eye ? Glow(new Color(0.65f, 0.06f, 0.05f)) : Flat(color),
        });

    // A wing hinge: a pivot at the shoulder, swept back, with a long narrow membrane extending out
    // from it. The beat (set each frame) rotates the WHOLE hinge up/down — so the wing sweeps from
    // the hinge like a real fly's, not like a fixed airplane wing pinned at its middle.
    private Node3D WingHinge(int side)
    {
        var hinge = new Node3D { Position = new Vector3(side * 0.07f, 0.13f, -0.02f) };
        hinge.AddChild(new MeshInstance3D
        {
            // Long + narrow, tapering back; offset OUT and slightly BACK along the hinge's local axis.
            Mesh = new BoxMesh { Size = new Vector3(0.6f, 0.012f, 0.16f) },
            Position = new Vector3(side * 0.34f, 0f, 0.12f),
            MaterialOverride = Flat(new Color(0.82f, 0.84f, 0.9f, 0.30f)),
        });
        return hinge;
    }

    private void BuildAltitudeCues()
    {
        // A soft shadow on the ground straight below the fly, and a thin drop-line up to it, so the
        // altitude reads at a glance (otherwise height is hard to judge from one camera angle).
        _shadow = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.45f, BottomRadius = 0.45f, Height = 0.02f },
            MaterialOverride = Flat(new Color(0f, 0f, 0f, 0.35f)),
        };
        AddChild(_shadow);

        _dropline = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(0.03f, 1f, 0.03f) }, // unit-tall; we scale Y to alt
            MaterialOverride = Flat(new Color(1f, 1f, 1f, 0.18f)),
        };
        AddChild(_dropline);
    }

    private void BuildTrail()
    {
        _trail = new MeshInstance3D
        {
            MaterialOverride = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                VertexColorUseAsAlbedo = true,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            },
        };
        AddChild(_trail);
    }

    private void BuildCameraAndLight()
    {
        _camTarget = Vector3.Zero;
        _cam = new Camera3D { Position = new Vector3(0, 9, 13) };
        AddChild(_cam);
        _cam.LookAt(Vector3.Zero, Vector3.Up); // LookAt needs the node IN the tree

        AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-55, -35, 0) });
    }

    private void BuildHud()
    {
        var layer = new CanvasLayer();
        AddChild(layer);

        var title = new Label
        {
            Text =
                "Play 9 — the united fly, in 3D.  Seeks the banana in X/Y AND climbs to its height on its "
                + "own wingbeat.  Lift is the real beat; forward thrust is still the 2D abstraction.   (Esc = menu)",
            Position = new Vector2(24, 18),
            Size = new Vector2(1180, 30),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        title.AddThemeFontSizeOverride("font_size", 16);
        layer.AddChild(title);

        layer.AddChild(new FlyBrainOverlay { Fly = _fly }); // the living brain + flight meters
    }

    private void PlaceBanana()
    {
        Vector3 p = Map(_world.Banana.X, _world.Banana.Y, _world.BananaHeight);
        _banana.Position = p;
        _bananaGlow.Position = p;
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
        _t += delta;
        double dt = Mathf.Min(delta, 1.0 / 30.0); // clamp a stutter so the body integrator stays sane

        NVec wasBanana = _world.Banana;
        _fly.Step(_world, dt); // the creature thinks and moves; we only advance and read it
        if (_world.Banana != wasBanana)
        {
            PlaceBanana(); // it ate — the banana jumped to a fresh corner + height
        }

        var p = _fly.Position;
        float alt = (float)_fly.Altitude;
        Vector3 flyPos = Map(p.X, p.Y, alt);

        // Place + face the fly along its heading.
        _flyBody.Position = flyPos;
        float hdg = _fly.HeadingRadians;
        var forward = new Vector3(Mathf.Cos(hdg), 0f, Mathf.Sin(hdg));
        _flyBody.LookAt(flyPos + forward, Vector3.Up);

        // Flap the wings with the real beat phase.
        // Beat: each hinge keeps its swept-back Y, and rolls up/down (Z) by the wingbeat phase.
        // Mirrored sign so both wings beat together, sweeping up over the back.
        float beat = WingRaise + Mathf.Clamp((float)_fly.WingbeatPhase, -1f, 1f) * WingAmp;
        _wingHingeL.Rotation = new Vector3(0f, WingSweep, beat);
        _wingHingeR.Rotation = new Vector3(0f, -WingSweep, -beat);

        // Antennae at their world spots, flaring red on contact (the fly sensing the wall).
        PlaceAntenna(_antL, _fly.AntennaLeft, alt, _fly.TouchL > 0.5);
        PlaceAntenna(_antR, _fly.AntennaRight, alt, _fly.TouchR > 0.5);

        UpdateAltitudeCues(flyPos, alt);
        UpdateTrail(flyPos);
        UpdateBanana();
        FollowCamera(flyPos);
    }

    private void PlaceAntenna(MeshInstance3D ant, NVec at, float alt, bool touching)
    {
        ant.Position = Map(at.X, at.Y, alt);
        ant.MaterialOverride = touching ? Glow(new Color(1f, 0.25f, 0.1f)) : Flat(new Color(0.4f, 0.3f, 0.15f));
    }

    private void UpdateAltitudeCues(Vector3 flyPos, float alt)
    {
        float gy = alt * PxToUnit;
        _shadow.Position = new Vector3(flyPos.X, 0.02f, flyPos.Z);
        // Higher up → a wider, fainter shadow, like a real one.
        float spread = 1f + gy * 0.12f;
        _shadow.Scale = new Vector3(spread, 1f, spread);
        ((StandardMaterial3D)_shadow.MaterialOverride).AlbedoColor =
            new Color(0f, 0f, 0f, Mathf.Lerp(0.35f, 0.08f, Mathf.Clamp(gy / 10f, 0f, 1f)));

        _dropline.Position = new Vector3(flyPos.X, gy * 0.5f, flyPos.Z);
        _dropline.Scale = new Vector3(1f, Mathf.Max(gy, 0.001f), 1f);
    }

    private void UpdateTrail(Vector3 flyPos)
    {
        _trailPts[_trailHead] = flyPos;
        _trailHead = (_trailHead + 1) % TrailLen;
        if (_trailCount < TrailLen)
        {
            _trailCount++;
        }

        var mesh = new ImmediateMesh();
        if (_trailCount >= 2)
        {
            mesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);
            for (int i = 0; i < _trailCount; i++)
            {
                int idx = (_trailHead - _trailCount + i + TrailLen * 2) % TrailLen; // oldest → newest
                float a = (float)i / (_trailCount - 1); // fades in toward the fly
                mesh.SurfaceSetColor(new Color(0.6f, 0.9f, 1f, a * 0.7f));
                mesh.SurfaceAddVertex(_trailPts[idx]);
            }
            mesh.SurfaceEnd();
        }
        _trail.Mesh = mesh;
    }

    private void UpdateBanana()
    {
        // A gentle breathing of the smell aura — purely cosmetic, signals "this is the live goal".
        float pulse = 1f + 0.08f * Mathf.Sin((float)_t * 2.0f);
        _bananaGlow.Scale = new Vector3(pulse, pulse, pulse);
    }

    private void FollowCamera(Vector3 flyPos)
    {
        // Ease the look-target toward the fly so the camera tracks it without jerking.
        var goal = new Vector3(flyPos.X * 0.6f, flyPos.Y * 0.5f, flyPos.Z * 0.6f);
        _camTarget = _camTarget.Lerp(goal, 0.05f);
        _cam.Position = _camTarget + new Vector3(0, 9, 13);
        _cam.LookAt(_camTarget, Vector3.Up);
    }

    private static StandardMaterial3D Flat(Color color)
    {
        var m = new StandardMaterial3D
        {
            AlbedoColor = color,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
        if (color.A < 1f)
        {
            m.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        }
        return m;
    }

    // Like Flat, but emissive so the banana / hot antenna glow against the dark scene.
    private static StandardMaterial3D Glow(Color color)
    {
        var m = Flat(color);
        m.EmissionEnabled = true;
        m.Emission = color;
        m.EmissionEnergyMultiplier = 1.4f;
        return m;
    }
}
