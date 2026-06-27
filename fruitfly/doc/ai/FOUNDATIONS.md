# Foundations — Fruit Fly Brain Simulation

> **New here?** Start with the [project README](../../README.md) for the human-facing
> overview of *what* this is (simulating a tiny brain — a fruit fly's — from spiking
> neurons up) and *why*. This file is the deeper harness/working-style reference.

The stable entry point for this project's AI harness. Keep this lean: **goals**,
**working style**, a **brief overview**, and **indexes** of plans and decisions.
Task-level detail lives in `plans/`; architecture decisions live in `../adr/`.

---

## Goals

- **The goal: understand a fruit fly.** Everything else serves this. The way we get
  there is by building **a simulated tiny brain** — a single fruit fly's — from spiking
  neurons up, so that understanding the brain *is* understanding the fly. The thing we are
  making is a *brain*, not a program that produces fly-like output.
- **Not a binary computer (the anti-goal):** the whole reason for this project is that a
  brain is **not** a digital computer, and we refuse to build one. Spikes are
  all-or-nothing, but the *computation is not* — information lives in continuous,
  analog quantities: firing **rates**, spike **timing**, coincidence **windows**, and
  **population** activity. We never model a circuit as logic gates, Boolean truth tables,
  or `if`-style decisions; behavior must **emerge** from graded, temporal neural dynamics.
  If an explanation or design reduces a circuit to digital logic, that is a red flag to
  stop and reframe.
- **Tiny brain vs. bigger brain (where computation is allowed to live):** there are two
  brains here. The **tiny brain** is the fly — the thing we simulate — and its *cognition*
  (turning sensation into a decision, choosing, steering) must live in **neurons**. The
  moment a hand-written algorithm decides what the fly *should do*, the designer's "bigger
  understanding" has been smuggled in to do the tiny brain's job; that gets **cleaned off**
  and pushed back into neurons. The **bigger brain** is us — the simulation around the fly —
  and it *obviously* understands the tiny brain; that is fine and necessary. The bigger
  brain may legitimately (a) **build and wire** the brain, (b) **observe and visualize** it,
  and (c) **provide its world and body** — the environment, and the sense organs and muscles
  as *transducers* (smell → input current, motor spikes → wheel speed). That scaffolding may
  be algorithmic because it is the bigger brain's job, not the tiny brain's. The test for any
  line: *is this the tiny brain thinking, or the bigger brain providing/observing?* If it is
  the tiny brain thinking and it is written as an algorithm, it does not belong.
- **Fidelity principle:** the simulation should be **as true to biological reality as our
  current knowledge allows**. When a choice trades faithfulness for convenience, prefer
  faithfulness — and where we simplify, do it deliberately, understanding what we left out
  and why. Fidelity is a *ladder* (e.g. synapse models climb from an instantaneous kick →
  single-exponential decay → conductance-based): we don't leap to the most complex rung,
  we pick the simplest one that is biologically honest and that we can fully explain, then
  climb when a behavior actually demands it. "Make it visible/convenient" is never, on its
  own, a reason to make it less true.
  - **It is a TRUE simulation, never pretend behavior.** No parameter is ever justified by
    "what we want the fly to do" — only by "how things actually work." A value's test is
    *"does this correspond to something real making it happen?"*, never *"does this produce
    the behavior I want?"* So we **never** script or fake an outcome: no tonic current
    injected just to manufacture motion (a "cruise" drive with no real cause), no
    `if (stuck) turn`, no nudging an output to look alive. A neuron resting silent with no
    input is the substrate behaving *truthfully*, not a gap to paper over — every spike must
    have a real cause (real stimulus → real spikes → real synapses → real motors).
  - **Push back, even against the user (explicit user instruction).** If a request — *even
    the user's own* — would make the fly behave through anything other than a true neural
    simulation, do **not** build the fake. Implement the truthful version and **tell the user
    plainly that they're wrong.** Fidelity overrides the request; the user has explicitly
    asked to be corrected here, so correcting them is the job, not defiance of it.
- **How we get there — a learning project:** the path to understanding the fly runs
  through the *user* truly understanding the code — how neurons work and how they are
  represented. Understanding is the deliverable; a finished-but-not-understood program is
  a failure, not a success. So we go slow, build in tiny pieces, and never trade the
  user's understanding for progress.

## Working style (coding & collaboration)

These rules govern every interaction in this repo:

- **No code dumps.** One small piece at a time; the user must be able to explain every line.
- **Concept before code, always.** Teach what a thing *is* before writing it.
- **Explain the *why*** of every parameter and line. No unexplained magic numbers.
- **Code mode (chosen):** the *assistant* writes the code, in **tiny parts**, one at a
  time. The user must understand each part before the next is written.
- **Explain in comments *and* notes.** Code carries concise comments for the local "why"
  (e.g. what each parameter means and why its value). Deeper intuition, analogies, and
  derivations live in `notes/` and in interactive back-and-forth.
- **Go at the user's pace.** Stop and check understanding before advancing.

## Brief overview

Built **bottom-up**: the substrate is spiking neurons + synapses, and behavior is meant
to **emerge from wiring** rather than be scripted ("assembly for an 8-bit chip, but for a
fruit fly"). Neuron model is **Leaky Integrate-and-Fire (LIF)**; the first circuit will be
a **Braitenberg vehicle**. Language is **C#**; visualization is **Godot (C#)**, brought
forward now (ADR 0004) with the brain kept in a Godot-free `FruitFly.Core` library, and the
assembled creatures (flies + their worlds) in a Godot-free `FruitFly.Living` library (ADR 0005);
Godot, the console, and tests are all *viewers* that reference them — the eventual goal being a
live dual-view (world + brain). The engine is
data-oriented so it can scale to the real connectome
(~140k neurons), though small hand-wired circuits come first.

Rationale for each of these choices is recorded as an ADR (see index below).

## Plans index

Task-level plans live in `plans/`. One plan per task/milestone.

| Plan | Status | Summary |
|------|--------|---------|
| [0001 — Milestone 0: LIF neuron](plans/0001-milestone-0-lif-neuron.md) | In progress | Single LIF neuron + synapse + 3-neuron chain; voltage trace. Get the math right before any engine. |
| [0002 — Persistent internal state (memory)](plans/0002-persistent-state-memory.md) | Working | First slice of "brain": a self-sustaining interneuron + spike-frequency adaptation gives the fly memory of being stuck, so it breaks free on its own. Built in isolation (5b) and in the fly (6). |
| [0003 — Extract creatures into `FruitFly.Living`](plans/0003-extract-creatures-into-living.md) | Fly done | The memory fly is extracted into Godot-free `FruitFly.Living` (Fly + World); the Godot view is a pure viewer; a headless console check asserts the wall-escape. Remaining: migrate the gallery circuits. |
| [0004 — Honest wall sensing, part 1: touch](plans/0004-honest-wall-sensing-touch.md) | Built (Living fly) | Ripped out the fake distance-to-wall field (a god's-eye number no fly could sense); `World.Touching` now reports real contact and the fly's wall neurons are touch receptors. The fly skims walls and the memory latch frees each jam — headless gate passes. Vision/looming deferred to 0005. |
| _0005 — Honest wall sensing, part 2: vision/looming_ | Earmarked | Give the fly a real eye: a wall read as an *expanding* dark region (optic flow / looming), so it avoids *before* contact — the dominant mechanism in a real fly. (Plan file not written yet; earmarked by 0004.) |
| [0006 — Toward neural flight: wings, aerodynamics, 3D](plans/0006-toward-neural-flight.md) | Roadmap | The arc toward flight: neurons that beat real wings, steer by asymmetric wingbeat, and hold the fly up against gravity in **3D**, with stabilisation emerging from real senses. A ladder of rungs (CPG → asymmetry → 3D body → quasi-steady aerodynamics → flight senses); each rung becomes its own plan. Next concrete step: 0007, the wingbeat CPG. |

_Several circuits (Braitenberg seeking, summation, inhibition, self-sustaining loop) were built
**without** their own plans — a documentation gap to backfill. (The memoryless Braitenberg fly
and the two ~300k-neuron flies view were removed once Play 6 superseded them.) Remaining future
milestones (decision circuit, ring-attractor compass, spontaneous search, connectome subgraphs)
will each get a plan when we reach them._

## Decision index (ADRs)

Architecture Decision Records live in `../adr/`. See [`../adr/README.md`](../adr/README.md).

| ADR | Decision |
|-----|----------|
| [0001](../adr/0001-use-csharp.md) | Use C# |
| [0002](../adr/0002-bottom-up-spiking-neurons.md) | Bottom-up spiking neurons (emergent), not a top-down state machine |
| [0003](../adr/0003-leaky-integrate-and-fire.md) | Use the Leaky Integrate-and-Fire neuron model |
| [0004](../adr/0004-godot-for-visualization.md) | Use Godot (C#) for visualization, brought forward now; brain stays in `FruitFly.Core` |
| [0005](../adr/0005-creatures-in-fruitfly-living.md) | Assembled creatures live in a Godot-free `FruitFly.Living` library; Godot is strictly a viewer |
