# Plan 0002 — Persistent internal state (the first slice of "brain")

**Status:** Working — the memory is built in isolation (Play 5b: poke → integrate → hold →
release) **and** wired into the fly (Play 6b): a stuck fly latches "I'm stuck" and breaks free
on its own via a committed, self-held turn. Remaining: clean up the banana-in-corner overlap and
backfill `notes/` + FOUNDATIONS.
**Goal:** Give the fly its first piece of **working memory** — an interneuron whose activity
*persists on its own* after the stimulus that triggered it is gone. This turns the fly from a
pure **reflex** (behavior = this instant's sensation) into something whose behavior depends on
its recent **history**. Demonstrated by the wall deadlock: a fly that *remembers* it is stuck
and keeps escaping even after the wall stops pushing.

## Why this, why now

Everything so far is a **reflex arc**: sensors wired straight to motors (6 neurons, 4
synapses), behavior fully determined by the current instant. That is real and not fake —
it is genuine chemotaxis — but it is not yet a *brain*. The three things a brain has that a
reflex lacks are **memory** (state over time), **decision** (committing between competing
drives), and **spontaneity** (self-generated activity). We start with **memory**, because
state-over-time is the most fundamental departure from a reflex: it is the first time the
fly's behavior can depend on something other than *right now*.

This is an application of **ADR 0002** (behavior emerges from neurons), not a new architecture
decision — the memory lives in neural dynamics, never in a C# variable.

## The concept

**Neural memory = self-sustaining activity, not a stored bit.** A neuron (or small loop)
that excites *itself* can keep firing after its input stops: the spikes it produces feed back
in and re-trigger it. That persistent firing *is* the memory — the trace of a past event held
as ongoing activity.

Crucial framing (see memory: *not a binary computer*): this is **not** a flip-flop or a 1-bit
latch. It is a **bistable, leaky, temporal** thing — an *attractor*. The "held" state is
graded, it can be pushed harder or softer, and it **decays** if the self-excitation is not
quite enough. We are modelling persistence as analog dynamics, not a digital register. The
gallery's existing **self-sustaining loop** play already shows the raw phenomenon; here we
make it do useful work as an **interneuron** sitting *between* sense and motor.

### The demonstrating behavior: remembering "I'm stuck"

Today the fly pins head-on against a wall because avoidance is purely *differential* (it needs
a left/right difference to turn) and a head-on wall gives none — and, fatally, the fly has
**no memory that it has been stuck at all**; every frame is fresh. The memory neuron fixes the
*root cause*, not the symptom:

- A "blocked" interneuron **integrates** sustained wall drive over time.
- When blockage persists past a tipping point, it **latches** into a self-sustaining state.
- While latched, it drives an **asymmetric** motor push (a committed turn) — even after the
  wall pressure momentarily eases — then **decays** and releases once the fly is clear.

The escape is *not* scripted: there is no `if (stuck) turn`. The "decision" lives in the
neuron's bistable threshold; the persistence lives in its self-excitation; the release lives
in its leak/decay. History-dependence emerges.

## Steps (tiny pieces — assistant writes, one at a time)

- [x] Lesson: persistent firing as memory (attractor intuition; contrast with a digital
      latch). Re-watch the self-sustaining loop play with this lens.
- [x] Add **one interneuron** with a **self-synapse** (recurrent excitation). Confirmed
      **bistable** in standalone **Play 5b**: silent at rest, latches when pushed hard enough,
      holds on its own. Critical self-weight ≈ 43. Key finding: the bare self-loop is a
      *permanent* latch — it holds but cannot release itself.
- [x] Wire **wall sensors → memory neuron** so sustained blockage charges it toward its
      tipping point (integration over time). Both wall sensors → memory (`WallToMemoryWeight`).
- [x] Wire **memory neuron → motors asymmetrically** so a latched state produces a committed
      turn (the escape). **Key finding:** it had to be **INHIBITORY**, not excitatory — a head-on
      jam SATURATES both motors, so pushing a wheel harder does nothing (difference stays 0);
      only pulling the *other* wheel DOWN out of saturation creates the turn. The stuck fly now
      breaks free on its own.
- [x] Add **decay/release** — done via **spike-frequency adaptation** added to `LifNeuron` (a
      fatigue current; `AdaptKick`/`TauAdapt`, off by default). Since the bare latch can't
      release itself, accumulating fatigue is what tips it back to rest. Release is a fast
      *snap* (bistable tip-over), not a gentle fade — expected. Hold-time tuned by feel in 5b.
- [ ] Update `notes/` with the intuition, and update the FOUNDATIONS plan index.

## Deferred (add only when a behavior demands it)

- **Spontaneous search** (a fly that smells nothing currently freezes forever) — a separate
  milestone; needs self-generated activity, not memory.
- **Decision / arbitration** (mutual inhibition so drives *compete* instead of *sum*) — the
  natural *next* brain-slice after memory.
- ~~**Spike-frequency adaptation** in `LifNeuron`~~ — **ADOPTED** as the release mechanism
  (see steps above). The bare self-loop is a permanent latch, so adaptation is what lets it
  let go. Now part of `LifNeuron`, off by default (`AdaptKick = 0`) so older circuits are
  unchanged.

## Open questions to resolve with the user

- Build the memory neuron **in isolation** first (its own little play), or wire it straight
  into the fly and tune in place?
- Is "remembering I'm stuck" the most compelling first demo of memory, or would a simpler,
  more visible demonstration teach the concept better before we apply it?

## Done when

The fly, pinned head-on against a wall, **breaks free on its own** through a persistent
internal state — the user can explain how the self-excitation holds the state, how the leak
releases it, and why this is memory and not a scripted escape or a digital latch.
