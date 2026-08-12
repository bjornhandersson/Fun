# Plan 0010 — Neuralize the flight command (kill the magic baseline)

**Status:** Done (steps 1–3). The uncaused flight baseline is gone — the beat is driven by a real
pacemaker command neuron. Behaviour matches the old suite (hover ~209px, alt-seek up+down, CPG
oscillates); only the pre-existing 3D orbit RED remains, untouched. Docs step (4) pending.

## Why

Fidelity audit (input → output, no smuggled decisions) found the one real leak in the flight
layer: the **baseline "command to fly" is a magic constant** injected with no cause —
`HalfCentreOscillator.Drive = 25.0` and `FlyingBody.BaselineCommand = 25.0`. It goes into the
Network's `_external[]` channel, which is *documented as the world's sensory-input channel*. So
the world is impersonating a flight drive: the beat exists because of a designer number, not a
firing cause. That is exactly the "cruise current with no real cause" CLAUDE.md forbids.

Real flies have persistent descending **command neurons** that hold flight on. The honest fix is
to make the baseline a real neuron — a **pacemaker** whose *intrinsic* biophysics keep it firing —
whose spikes drive the wingbeat through a synapse.

## The mechanism

A LIF neuron with `VRest` **above** `VThreshold` can never settle: it drifts past the firing line,
fires, resets, and repeats — forever, with **zero external input**. That is a pacemaker, and the
drive is now a *property of the cell* (`init`-only, like `Tau`), not a current the world pushes in.
`-40 mV` rest = the old `-65 rest + 25 drive`, reproducing the baseline as the cell's own nature.

Honesty note: this is NOT relocating the number. (a) It moves from `_external[]` (world's channel)
to the cell's personality; (b) it becomes a *separate upstream node* whose SPIKES drive the wings,
so flight gains a locus that can be gated/fatigued/modulated neurally. That structural change is
the real gain.

## Steps (tiny, assistant writes one at a time)

- [x] **1. Isolation proof** — `PacemakerCheck`: a lone neuron with `VRest = -40` fires on its own
      with `I = 0`. Proven: ~90 Hz, first spike at 8 ms, zero input.
- [x] **2. CPG drive** — command neuron added to `HalfCentreOscillator`, synapsed → both L/R
      (`CommandWeight = 52`, calibrated so baseline vigour ≈ 0.204), constant `Drive` deleted.
      Play 7 still oscillates. `SetCommand` → `SetModulation` (extra current on top, default 0).
- [x] **3. Flight modulation** — `FlyingBody.BaselineCommand` deleted; the hover/climb reflexes
      (sensory-caused) now feed `SetModulation` only. Hover holds ~209px; alt-seek up+down PASS.
- [ ] **4. Docs** — update FOUNDATIONS index, [[0007]], [[0008]], [[0009]] to point here.

## Out of scope (named, not fixed here)

- Lift gain (`LiftGain = 1470`) tuned so hover is the equilibrium — a separate audit item.
- Forward thrust as the "wheels" abstraction and lift = vigour×gain — the deferred pitch rung.
- Membrane noise as an accepted (real biophysics) exterior influence.
