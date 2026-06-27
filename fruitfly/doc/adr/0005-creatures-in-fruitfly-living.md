# ADR 0005 — Assembled creatures live in a Godot-free `FruitFly.Living` library

**Status:** Accepted

## Context

ADR 0004 set the boundary: the brain lives in Godot-free `FruitFly.Core`; Godot and the
console are *viewers*. That held for the **substrate** (`LifNeuron`, `Synapse`, `Network`,
`SpikingNet`). But the **assembled creatures** drifted: the Braitenberg fly's wiring, its body
transducers (smell/wall sensing → input current, motor spikes → wheel activation), and its
world (smell field, walls, collision) grew *inside* the Godot view files
(`BraitenbergFlyMemoryView.cs` and siblings). The viewer is now constructing brains and
simulating bodies. The user surfaced this as drift from ADR 0004's intent.

Two compounding forces:

- The bodies/worlds use Godot types (`Vector2`, `Mathf`, `GD.Randf`), hard-coupling the
  simulation to the engine.
- The only way to check a fly's behavior is to *watch it* in Godot — no headless run, no test.
  Correctness rests on eyeballing.

ADR 0004 loosely named "future circuits" as belonging in Core. In practice Core should stay the
pure **substrate**; assembled creatures (brain + body + world) are a distinct layer that belongs
in neither the substrate nor the viewer.

## Decision

1. Add a **Godot-free class library `FruitFly.Living`** for assembled creatures and their
   worlds. It references `FruitFly.Core` and uses only the BCL (`System.Numerics.Vector2`,
   `System.Math`, `System.Random`) — **no Godot types**.
2. A creature (e.g. `Fly`) owns: its `Network` wiring (the *tiny brain*) and its body/world
   transducers (the *bigger brain's* provided sense organs, muscles, and world). It exposes
   read-only state and a `Step(dt)` method. Viewers never make decisions.
3. **Godot is strictly a viewer**: it constructs a `FruitFly.Living` creature, calls `Step`,
   reads state, draws, forwards input. No wiring, no body math in the view.
4. The console app and unit tests drive the same `FruitFly.Living` creatures **headless**.

This refines ADR 0004: "circuits in Core" becomes "substrate in Core, assembled creatures in
Living."

## Consequences

- The architecture mirrors the philosophy: Core = substrate, Living = creature (tiny brain +
  provided body/world), Godot = the bigger brain *observing*. The "tiny vs bigger brain" line
  becomes a project boundary you can see.
- Creatures become **headless-runnable and testable** — behaviour can be *asserted* ("a fly
  rammed into a wall escapes within N steps") instead of eyeballed. A real fidelity win.
- Cost: body/world code must drop Godot types for BCL equivalents (mechanical, but it touches
  sensing/collision/random-number use).
- Godot regenerates its `.csproj` and may drop the new `ProjectReference`; we re-add and verify
  (the same caveat already noted in the Godot csproj for the Core reference).
- Migration is incremental (see plan 0003): extract one known-good creature (the fly) first,
  prove it headless, then migrate the gallery circuits.
