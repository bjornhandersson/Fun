# Plan 0007 — The wingbeat oscillator (CPG): the motor's first self-generated rhythm

**Status:** Not started — chosen as rung 1 of the flight roadmap ([[0006]]). Build the oscillator
**in isolation first** (its own gallery play, the way the memory neuron was proven in Play 5b),
then wire it into the fly's wings.
**Goal:** Give the fly's *motor system* its first **self-generated rhythm** — a small circuit of
neurons that produces a steady **beat with no rhythmic input**. This is the substrate a real
wingbeat needs: wings that flap because neurons won't sit still, not because the body tells them to.

## Why now / why this

The motor neurons (`_motorL/_motorR`) already spike, but their output is a *steady* push (a leaky
integral of summed sensory drive). A real fly's wings **beat**. That rhythm is the missing neural
capability — and it is the **third** thing a brain has beyond the two we've built:

- **Reflex** (Play 1–6 wiring) — behaviour = this instant's sensation.
- **Memory** (Plan 0002) — behaviour depends on recent history (self-sustaining activity).
- **Spontaneity / rhythm (this plan)** — behaviour the brain generates *itself*, with no driving
  input. A central pattern generator (CPG) is the cleanest example.

This is also exactly the "spontaneous activity" milestone deferred back in Plan 0002.

## The concept

**A CPG = neurons that oscillate on their own.** No rhythmic input goes in; rhythm comes out. The
classic biological motif (and the one we'll use because it reuses what we already built) is the
**half-centre oscillator**: two neurons (or pools) that **inhibit each other**, plus a **fatigue**
that makes whichever side is winning gradually tire. Whoever fires suppresses the other; as the
leader fatigues, the follower escapes inhibition and takes over; then *it* fatigues — and the two
sides **alternate** forever. That alternation *is* the beat.

Two pieces we already have make this almost free:

- **Inhibition** — proven in the gallery (Play 4) and already used as a negative synapse weight.
- **Spike-frequency adaptation** — the fatigue current added to `LifNeuron` for the memory neuron
  (`AdaptKick`/`TauAdapt`). The same mechanism that lets the memory latch *release* is what makes a
  half-centre *hand over*. (See [[saturation-hides-emergence]] — keep both cells in the graded band
  so the alternation is real dynamics, not a stuck flip-flop. This is **not** a digital clock; it's
  an analog, leaky oscillation — see [[not-a-binary-computer]].)

### Boundary test (keeps us honest)

The rhythm must come from the **neurons** (mutual inhibition + fatigue), never from a C# timer,
`sin(t)`, or a frame counter — that would be the bigger brain handing the fly a beat it didn't
generate. The wing→motion physics (`forward`/`turn`) stays in the body; we are neuralizing the
motor *pattern*, not the kinematics.

## Steps (tiny pieces — assistant writes, one at a time)

- [x] **Lesson:** what a CPG is; why two mutually-inhibiting cells + fatigue alternate; contrast
      with a digital clock. Re-used the inhibition (Play 4) and adaptation (Plan 0002) intuitions.
- [x] **The circuit lives in ONE place** — `FruitFly.Living.HalfCentreOscillator`: two LIF cells on
      a real `Network`, cross-inhibited, adaptation on, a steady drive set once; `Step(dtMs)` ticks
      the brain and folds spikes into `LeftActivity`/`RightActivity` (0..1, the same muscle-activation
      trick the Fly uses — those two signals will BE the wing amplitudes at rung 2). Godot-free, per
      ADR 0005. Working params: Drive 25, Inhib 40, AdaptKick 1.0, AdaptTau 120, Noise 1.0 (no tuning
      needed). Tempo (~4–5 Hz) is a knob in AdaptKick/AdaptTau for later.
- [x] **Prove it headless** — `HalfCentreOscillatorCheck` (console) is a thin VIEWER of that class:
      it Steps the oscillator and prints a swinging L/R trace. Alternates from a quiet start, no
      timer: `switches=7 → PASS`. (Same role `FlyHeadlessCheck` plays for the Fly.)
- [x] **Visualise it** — Godot **Play 7** (`WingbeatCpgView`), also a thin viewer of the same class:
      a scrolling timeline — L fills up from a centre line (magenta), R fills down (cyan), so the
      alternation swings above/below the line. A flat "steady drive" bar on the left makes the point
      visible: constant input in, rhythmic output out. No neurons or params duplicated in the viewers.
- [x] **Tried wiring it into the fly's wings (2D) — and learned it doesn't belong here yet.** Gated
      the fly's thrust by the beat two ways: (a) by the instantaneous power stroke → steering turned
      on/off with each stroke, the fly couldn't turn through the troughs and pinned to walls
      (`longestStuck 371`, FAIL); (b) by a smooth "beating" envelope → to keep steering smooth the
      envelope has to be near-*constant*, making the beat a behaviourally **inert** multiplier.
      **Conclusion (the substrate talking):** a wingbeat has **no honest job in a 2D top-down world**
      — up here thrust is just "go forward"; the beat's real purpose is generating **lift against
      gravity**, which only exists in 3D. So rung 2 (beat drives the body) effectively **merges into
      rung 3 (3D)**. Reverted the fly integration so nothing fake/inert is left; the fly passes at its
      proven baseline (`path 4339, longestStuck 58`). Wingbeat circuit + Play 7 stay as-is.
- [ ] **Wire the beat → thrust → LIFT in 3D** (now part of [[0006]] rung 3): the wings beat, the
      power stroke makes lift to hold the fly up, and left/right amplitude steers. This is where the
      beat finally earns its keep.

## Open questions to resolve with the user

- **One oscillator or two?** A single half-centre whose two sides *are* left/right wing (alternation
  = the beat), or one oscillator per wing with steering setting each one's amplitude? Start simplest.
- **Build in isolation first** (recommended — it worked for memory in 5b) or grow it straight into
  the fly and tune in place?
- **What demonstrates it?** A visible, self-starting beat from a silent substrate is the proof —
  same shape of demo as the memory latch.

## Done when

A small neural circuit produces a **steady, self-generated beat** from a quiet start — no timer, no
`sin(t)` — and the user can explain how mutual inhibition plus fatigue make the two sides alternate,
and why that is rhythm emerging from dynamics rather than a clock. Then the wings beat because the
brain beats them.
