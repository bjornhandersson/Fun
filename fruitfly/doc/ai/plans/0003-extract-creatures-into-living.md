# Plan 0003 — Extract creatures into `FruitFly.Living` (Godot becomes a pure viewer)

**Status:** Fly extracted ✓ (ADR 0005). The memory fly now lives in `FruitFly.Living` (`Fly` +
`World`), the Godot view is a pure viewer, and a headless console check asserts the wall-escape
(`[fly headless] … PASS`). Remaining: migrate the gallery circuits. Order chosen with the user:
**stabilise the current fly first**, *then* extract that baseline — done.
**Goal:** Move the assembled fly — brain wiring + body + world — out of the Godot view into a
Godot-free `FruitFly.Living` library, so Godot only *visualises*. Prove the fly runs headless,
then migrate the rest of the gallery the same way.

## Why now

The substrate (`FruitFly.Core`) is clean, but the **creatures** drifted into the Godot views
(see ADR 0005): `BraitenbergFlyMemoryView.cs` now builds the `Network`, wires synapses, and
simulates the body/world. That breaks ADR 0004's "Godot is a viewer", couples the sim to Godot
types, and leaves behaviour untestable except by eye. The user caught it.

## Approach — incremental, one known-good creature first

- [x] **Stabilise the baseline.** Built + ran the nociceptor fly in Godot; user confirmed it
      seeks, avoids, latches the memory when stuck, escapes, and the nociceptor fires only on a
      real ram. This is the behaviour the extraction preserved.
- [x] **Create `FruitFly.Living`** — class library, `net10.0`, references `FruitFly.Core`,
      BCL-only. `ProjectReference` added from Godot **and** the console app.
- [x] **Extract the fly** out of `BraitenbergFlyMemoryView` into `FruitFly.Living`:
      - `Fly` owns the `Network` + all wiring + body transducers + memory + nociceptor;
      - `World` holds banana + walls + smell field + collision, using `System.Numerics.Vector2`
        / `System.Math` / `System.Random` — no Godot types;
      - `Step(world, dtSeconds)` senses → sets inputs → steps the net (substeps) → integrates
        activations → moves → clamps/collides → eats;
      - read-only state exposed for viewers (position, heading, antennae, activity, flags).
- [x] **Thin the Godot view** to a pure viewer: builds a `Fly` + `World`, `Step`s each frame,
      reads state, draws, forwards SPACE/Esc. No `Network`/`Synapse`/body math left.
- [x] **Prove it headless:** `FlyHeadlessCheck` (console) drives the fly into the corner and
      asserts it moves and never stays stuck > ~4 s. Result: `path≈5500px, longestStuck≈125
      steps → PASS`.
- [ ] **Migrate the rest** (single neuron, chain, summation, inhibition, loop, the plain fly,
      two-flies, memory Play 5b) into `FruitFly.Living`, opportunistically.
- [ ] Promote the headless check into a real test project (xUnit) once we want a CI gate.

## Boundary test (keeps us honest)

For each line we move, ask: *tiny brain* (neurons deciding) or *bigger brain providing*
(body / world / transduction)? Both belong in Living, but in **distinct** types (brain vs
body/world) so the line stays visible. Nothing that *decides* may remain in the viewer.

## Done when

The Godot fly view holds no `Network`/`Synapse`/body math — only constructing a
`FruitFly.Living` creature, stepping it, and drawing — and a **headless test** demonstrates the
fly's wall-escape without opening Godot.
