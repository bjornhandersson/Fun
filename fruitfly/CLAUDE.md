# CLAUDE.md

Operational guide for working in this repository. For full context and the reasoning
behind every decision, read **[`doc/ai/FOUNDATIONS.md`](doc/ai/FOUNDATIONS.md)**.

## What this project is

A simulation of a single fruit fly, built **bottom-up from spiking neurons** so that
behavior *emerges* from neural wiring rather than being scripted. Built in **C#**.

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
- **Neuron model:** Leaky Integrate-and-Fire (LIF).
- **First circuit:** Braitenberg vehicle (cross-wired sensors → motors → seeking emerges).
- **Engine:** data-oriented (struct-of-arrays, CSR sparse synapses, event-driven spikes).
- **Visualization:** Godot (.NET/C#) — brought forward now (see ADR 0004). The brain lives
  in a Godot-free `FruitFly.Core` library; Godot and the console app are *viewers* that
  reference it. Eventual goal: live dual-view (world + brain).

## Current status

Stack decided; AI harness established. **Milestone 0 underway — LIF neuron and synapse
built, now wiring chains.** `LifNeuron` (state + parameters + an Euler `Step`) and
`Synapse` (turns a source spike event into target input current) are done and verified.
The `Program` driver wires a **2-neuron chain** (A driven by constant input → B through one
synapse) and renders dual ASCII voltage panels: A fires periodically and each A-spike
drives B over threshold. **Next:** extend to a 3-neuron chain (input → middle → output).
See the active plan: [`doc/ai/plans/0001-milestone-0-lif-neuron.md`](doc/ai/plans/0001-milestone-0-lif-neuron.md).

## AI structure

- `CLAUDE.md` (this file) — short operational entry point.
- `doc/ai/FOUNDATIONS.md` — goals, working style, brief overview, and indexes of plans + ADRs.
- `doc/ai/plans/` — one plan per task/milestone (where we plan things before building).
- `doc/adr/` — Architecture Decision Records (significant decisions + their *why*).
