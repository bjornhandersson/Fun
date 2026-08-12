# CLAUDE.md

Operational guide for working in this repository. For full context and the reasoning
behind every decision, read **[`doc/ai/FOUNDATIONS.md`](doc/ai/FOUNDATIONS.md)**.

## What this project is

A simulation of a single fruit fly, built **bottom-up from spiking neurons** so that
behavior *emerges* from neural wiring rather than being scripted. Built in **C#**.

## Non-negotiable: this is a TRUE simulation, not pretend behavior

Every behavior must be **real** — caused through the neurons, not faked to *look* right.
No parameter is ever about "what we want the fly to do"; it is always about **how things
actually work**. The test for any value or line is *"does this correspond to something real
making it happen?"* — never *"does this produce the behavior I want?"*

- **Never script or fake behavior.** No tonic injected just to manufacture motion (e.g. a
  "cruise" current with no real cause), no `if (stuck) turn`, no nudging outputs to look
  alive. A neuron at rest staying silent is the substrate behaving *truthfully*, not a
  problem to paper over. Movement must EMERGE from genuine causes (real stimulus → real
  spikes → real synapses → real motors).
- **Push back, even against the user — this is an explicit instruction from the user.** If a
  request (even the user's own) would make the fly act through anything other than a true
  neural simulation, **do not build the fake.** Implement the truthful version *and tell the
  user, plainly, that they're wrong.* Fidelity overrides the request. The user has asked to
  be corrected here; correcting them is doing the job, not defying it.

See FOUNDATIONS "Goals → Fidelity principle" and "Tiny brain vs. bigger brain".

## The most important thing: this is a LEARNING project

The real goal is that the **user truly understands the code** (how neurons work and how
they are represented in code) — *not* shipping a finished program. Treat this as the
prime directive. Follow these rules in every interaction:

- **No code dumps.** One small piece at a time; the user must be able to explain every line.
- **Concept before code, always.** Teach what a thing *is* before writing it.
- **Explain the *why*** of every parameter and line. No unexplained magic numbers.
- **Code mode (chosen): the assistant writes the code**, in tiny parts, teaching each
  part first; the user must understand it before the next part. (Can flex; confirm per
  session. See FOUNDATIONS "Working style".)
- **Go at the user's pace.** Stop and check understanding before advancing.

## Technical approach

- **Bottom-up, emergent.** Substrate = spiking neurons + synapses. Behavior emerges from
  wiring. "Assembly for an 8-bit chip, but for a fruit fly."
- **Tiny brain vs. bigger brain.** The fly's *cognition* (sense → decide → steer) lives in
  neurons only; an algorithm that decides what the fly *should do* is the designer's "bigger
  understanding" smuggled in — clean it off, push it into neurons. The *bigger brain* (us)
  may still wire, observe, and provide the world/body (sense organs + muscles as
  transducers). Test each line: tiny brain thinking, or bigger brain providing/observing?
  See FOUNDATIONS "Goals". 
- **Neuron model:** Leaky Integrate-and-Fire (LIF).
- **First circuit:** Braitenberg vehicle (cross-wired sensors → motors → seeking emerges).
- **Engine:** data-oriented (struct-of-arrays, CSR sparse synapses, event-driven spikes).
- **Visualization:** Godot (.NET/C#) — brought forward now (see ADR 0004). The brain lives
  in a Godot-free `FruitFly.Core` library; Godot and the console app are *viewers* that
  reference it. Eventual goal: live dual-view (world + brain).

## Current status

Stack decided; AI harness established. The neural substrate is built and verified:
`LifNeuron` and `Synapse` for small hand-wired circuits, plus `SpikingNet` (the *same* LIF
Euler step in a data-oriented SoA + CSR layout) for large populations. A Godot gallery
visualises the circuits built so far: single neuron, two-neuron chain, summation, inhibition,
a self-sustaining loop, a memory neuron in isolation, the assembled Braitenberg **fruit fly**
(Play 6) — it seeks a banana by smell, avoids walls by **touch**, and latches a memory of being
stuck to break free — a **wingbeat CPG** (Play 7): two neurons that generate their own rhythm
(mutual inhibition + fatigue); and a **hover** (Play 8, 2.5D): a body with mass + gravity whose
wingbeat makes lift, held aloft by a neural reflex (sense dropping → beat harder) — shove it and it
recovers; and the **united fly in 3D** (Play 9, Plan 0009): the Play-6 horizontal brain composed with
the wingbeat altitude layer into ONE creature that flies through 3D space — it nails the banana's
height every trial, though reliable *eating* still waits on a horizontal-orbit fix (documented RED in
`Fly3DHeadlessCheck`). The last few are the first steps toward neural flight. All behaviour **emerges
from wiring** — no view lets the engine make a decision. (The 3D wings are a faithful *readout* of the
real neural wingbeat, not an aerodynamic sim; lift is vigour×gain, not wing forces — see Plan 0009.)
(Earlier redundant fly demos — a memoryless fly and a two-flies population view — were removed once 6
superseded them.)
See [`doc/ai/plans/0001-milestone-0-lif-neuron.md`](doc/ai/plans/0001-milestone-0-lif-neuron.md).

## How to run things

- **Godot app** (the gallery / plays) — there is no `godot` on PATH; launch the Mono build
  directly, in the background so the session isn't blocked:
  ```
  /Applications/Godot_mono.app/Contents/MacOS/Godot --path src/FruitFly.Godot
  ```
- **Headless checks** (console app, `src/FruitFly/Program.cs`):
  ```
  dotnet run --project src/FruitFly
  ```

## AI structure

- `CLAUDE.md` (this file) — short operational entry point.
- `doc/ai/FOUNDATIONS.md` — goals, working style, brief overview, and indexes of plans + ADRs.
- `doc/ai/plans/` — one plan per task/milestone (where we plan things before building).
- `doc/adr/` — Architecture Decision Records (significant decisions + their *why*).
