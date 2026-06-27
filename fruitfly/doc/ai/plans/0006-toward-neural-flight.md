# Plan 0006 — Toward neural flight: wings, aerodynamics, and 3D (roadmap)

**Status:** Roadmap — *not* a single buildable milestone, but the arc several milestones hang
from. Each **rung** below becomes its own numbered plan when we start it. The concrete **next
step** is rung 1 (the wingbeat CPG). Nothing here is built yet.
**Goal:** Get to a fly whose *neurons drive real wing kinematics that produce flight forces*, in
**3D**, with the fly's cognition (sense → decide → steer) keeping it aloft and on course. In one
line: **a tiny brain that flies its own body through the air.**

## The honest framing (so it doesn't drown the project)

This *sounds* like "first build a fluid-dynamics engine." It is not — that would be
backwards and would bury the learning project. Apply this repo's own two rules:

- **Tiny brain vs. bigger brain.** What we care about is *neurons controlling wings* — the **tiny
  brain**. How a beating wing pushes air to make force is the **body**, which the bigger brain
  provides as a *transducer* ("wing motion in → force out"), exactly the role smell and touch
  already play. So the aerodynamics is scaffolding we *provide*, never the fly's cognition.
- **Fidelity is a ladder; don't leap to the top rung.** The force model climbs from "amplitude →
  a force constant" → **quasi-steady blade-element** (what real insect-flight research uses) →
  and only at the very top, full unsteady CFD (Navier–Stokes, leading-edge vortices — the "X,Y,Z
  swell"). We pick the simplest rung that is biologically honest and we can fully explain, then
  climb only when a behaviour demands it. **Full CFD is deferred indefinitely** — it's a separate,
  research-grade body, not a prerequisite for real neural wing control.

The test for every line stays the same: *tiny brain thinking, or bigger brain providing/observing?*

## Where we are now (rung 0)

Two motor neurons → an L/R **wing-effort** scalar → 2D differential kinematics (`Fly.Step`):
`forward = (wingL+wingR)/2`, `turn = wingL−wingR`. The wings are **abstract** — thrust is
instantaneous, there is no wingbeat, no body mass, no gravity, no third dimension. Honest for what
it is (a Braitenberg vehicle that happens to seek a banana), but the wings don't *beat* and the
fly can't *fall*. Everything below grows out of fixing exactly those two gaps.

## The ladder (each rung = a future plan)

- **Rung 1 — The wingbeat (a neural oscillator / CPG).** A tiny loop of neurons that produces
  **rhythm on its own** — a central pattern generator. This is a genuinely *new brain capability*:
  **self-generated activity**, the third thing a brain has beyond the reflex (done) and memory
  (done), and already foreshadowed as "spontaneous activity" in Plan 0002's deferred list. The
  wingbeat **amplitude/frequency** becomes the controllable variable; thrust still comes from a
  trivial constant (no aerodynamics yet). *This is the next concrete step and gets its own plan.*
- **Rung 2 — Steering by bilateral asymmetry.** Neural modulation of **left vs. right stroke
  amplitude** → a yaw torque. This *replaces* today's differential-drive fake with the real thing:
  a fly turns by beating one wing harder than the other. Forces still planar.
- **Rung 3 — Make it 3D (the body becomes a body in space).** Position gains **X, Y, Z**;
  orientation gains **pitch/roll/yaw**; the body gains **mass** and feels **gravity**. The fly must
  now generate **lift** or it falls. The Godot viewer goes **3D** (cool in itself). Crucially this
  turns flight into a real **control problem**: staying aloft and upright can't be scripted, so it
  becomes a rich source of new circuits (equilibrium reflexes, and later the haltere sense). Big,
  self-contained, exciting milestone.
- **Rung 4 — Honest aerodynamics (climb the force ladder).** Swap the force constant for a
  **quasi-steady blade-element** model: a wing's force from its instantaneous velocity and angle of
  attack (translational + rotational + added-mass terms). This is the rung that makes wing control
  *physically real* without a fluid simulator. Leading-edge vortex / clap-and-fling and full CFD
  sit above it, deferred.
- **Rung 5 — Senses that close the flight loop.** Real flies stabilise flight with **halteres**
  (vibrating gyroscopes sensing rotation), **airflow** on the antennae, and **vision** (optic flow
  — ties back to the eye, Plan 0005). Wire these back to the wingbeat controller and **closed-loop,
  stabilised flight emerges** rather than being commanded.

Each rung is small on its own; the "lot to take in" is really just rung 1.

## "Can this brain *evolve* into it?" — two meanings, kept honest

- **Grow it rung by rung (our method).** Hand-wire each circuit so the user understands every
  neuron. The brain "evolves" through *our* development; every line stays explainable. This fits
  the prime directive.
- **Evolve it with a genetic algorithm (deferred).** Mutate wiring/weights, select for flight,
  let a controller emerge. Genuinely exciting and very "alive" — **but** it tends to produce a
  **black-box** brain nobody can explain, which fights this project's whole point (the user
  understanding the code). A legitimate someday-experiment, eyes open; **not** the path we take to
  reach flight. Recorded here so the trade-off is a deliberate choice, not a default.

## Boundary test (keeps us honest at every rung)

The wingbeat rhythm, the bilateral steering, the equilibrium reflexes — all must live in
**neurons**. The aerodynamic force model, gravity, body mass, and the 3D world are the **bigger
brain providing the body/world**, and may be algorithmic because that is the body, not the
cognition. No `if (falling) flap harder` ever — if that temptation appears, the wiring is wrong,
not the rule.

## Open questions to resolve with the user (per rung, when we reach it)

- **Rung 1:** build the CPG **in isolation first** (its own gallery play, like the memory neuron
  5b) or grow it straight into the fly? (Isolation worked beautifully for memory.)
- **Rung 3:** how far into 3D — full 6-DOF rigid body, or start with "2.5D" (height + gravity, yaw
  only) and add roll/pitch later? And: Godot 3D viewer now, or keep a 2D top-down view of a 3D sim
  at first?
- **Rung 4:** is quasi-steady blade-element the right honest rung, or is a simpler lift/drag-vs-angle
  curve enough until a behaviour demands more?
- **Scope:** do we keep the banana-seeking task through all of this, or does flight get its own
  minimal task (hover, hold altitude, fly to a point)?

## Done when

A fly whose **neurons beat its wings**, **steer by asymmetric wingbeat**, and **hold itself up
against gravity in 3D** — with stabilisation emerging from real senses, not script — and the user
can explain every neuron in the loop. We will have climbed from a 6-neuron reflex to a brain that
flies a body. Each rung ships as its own plan; this document is the map they hang from.

## Immediate next

Write **Plan 0007 — The wingbeat oscillator (CPG)** and build rung 1: the fly's first
self-generated rhythm. No aerodynamics, no 3D yet — just wings that *beat* because a little ring of
neurons won't sit still.
