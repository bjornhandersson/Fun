# Plan 0008 — Altitude: the wingbeat holds the fly up (2.5D)

**Status:** Not started — the start of [[0006]] rungs 2+3 (which merged: see [[0007]], the beat has no
honest job until there's gravity). Scope chosen with the user: **add ONE vertical axis** (gravity +
lift), keep horizontal seeking/steering exactly as it is, and **isolate it first** in a side-view
play before touching the seeking fly — the same isolate-then-integrate path that worked for memory
(5b) and the CPG (Play 7).
**Goal:** Make the wingbeat from [[0007]] finally do real work: the body has **mass** and feels
**gravity**, and the beat makes **lift**. Crucially, **holding altitude must EMERGE from a neural
reflex** (sense vertical motion → adjust the beat), never from a hand-set hover thrust.

## Why this, why now

[[0007]] showed the wingbeat has nothing honest to do in a flat, top-down world — thrust there is
just "go forward". Gravity is what gives a wingbeat its purpose: **stay up or fall.** Adding a single
vertical axis is the smallest change that makes the beat matter, and it turns flight into a real
**control problem** (the third thing on the roadmap: behaviour the fly must actively regulate).

## The concept

**Lift vs. gravity.** Gravity pulls the body down with a constant force (mass × g). The wingbeat
pushes up with **lift** — and over a wingbeat cycle, lift grows with how hard the wings beat. Net
vertical force = lift − weight; that drives vertical acceleration.

**Why a fixed beat cannot hover (the control problem).** If the beat is constant, lift is constant.
Constant lift ≠ weight (except by exact luck), so the body accelerates **up forever or down forever**.
Even if we tuned lift = weight precisely, the tiniest disturbance would send it drifting — there is
nothing holding it. **Hovering is not a setting; it is a regulated balance.** We will *show* this
first (a body that just falls or climbs), because the failure is what motivates the reflex.

**The equilibrium reflex (where hovering comes from).** Give the body a sense of its own **vertical
motion**, wired so that *dropping makes it beat harder* and *rising eases it off* — negative
feedback. Now the beat is continuously corrected toward the lift that cancels gravity, and a steady
hover **emerges from the loop**. The "hold" lives in the feedback, not in a number we typed.

- The **vertical-motion sense** is the *body* providing a sense organ (bigger brain, ADR 0005) — a
  stand-in for how a real fly knows it's descending (optic flow as the ground rushes up; airflow on
  the antennae). It is a transducer, like smell and touch.
- The **correction** — sense → adjust beat — lives in **neurons**. That is the tiny brain.

### Boundary test (keeps us honest)

The altitude-hold must come from **sensor → neuron → wing drive**. Forbidden: `if (falling) addLift`,
a PID controller in C#, or a hand-set "hover thrust" tuned to exactly cancel gravity. A *rough*
baseline beat is allowed (a fly's wings are sized to its weight — that's morphology, the body), but
the **correction that actually holds it must be neural**, and we must be able to point at the loop.

## Steps (tiny pieces — assistant writes, one at a time)

- [ ] **Lesson:** lift vs. gravity; why a constant beat can only fall or climb; negative feedback as
      the source of a hover. (Concept before code.)
- [x] **A body that falls/climbs** — `FruitFly.Living.FlyingBody`: mass, gravity, and lift from the
      [[0007]] wingbeat at a **fixed** command. `FlyingBodyCheck` (console) drops it from 300px and it
      sinks, *accelerating*, straight through the floor (`end −593px → DIVERGED`). Fixed beat → no
      hover, exactly as the concept predicts. This motivates the reflex.
- [ ] **Add the vertical-motion sense + reflex loop** — a transducer for descent/ascent feeding the
      wingbeat's drive through neurons (negative feedback). Tune until **altitude-holding emerges**:
      drop it and it beats harder and recovers.
- [ ] **Side-view play** — a Godot view (height on the vertical axis, time scrolling) so we watch it
      sag, beat, recover, and settle into a hover. The beat from Play 7 now visibly *does something*.
- [ ] **Docs:** update this plan, [[0006]], [[0007]], and the FOUNDATIONS index as altitude lands.

## Open questions to resolve with the user (when we hit them)

- **Velocity feedback vs. position feedback.** Sensing vertical *velocity* (damping) stops dives but
  drifts in altitude; sensing *position* holds a height but needs an honest reference. A real fly's
  reference is **visual** (hold this view of the world) — so position-hold is honest *if* framed as
  visual station-keeping, not a magic altitude number. Likely resolve empirically while building.
- **How rough can the baseline beat be?** Keep it clearly under/over weight so the reflex is doing
  the real work, not a pre-balanced trim.
- **Oscillator drive needs to be settable.** Today `HalfCentreOscillator` has a fixed internal drive;
  the reflex has to modulate it. Small extension: let the command drive be set per step.

## Done when

A body **holds itself up against gravity by beating its wings** — knock it down and it recovers to a
hover — and the hold demonstrably comes from a **neural feedback loop** (vertical sense → beat), not
a scripted setpoint. The user can explain why a fixed beat can only fall or climb, and how the reflex
makes a stable hover emerge. The wingbeat from [[0007]] has earned its keep.
