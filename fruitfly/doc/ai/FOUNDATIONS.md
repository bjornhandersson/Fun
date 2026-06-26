# Foundations — Fruit Fly Brain Simulation

> **New here?** Start with the [project README](../../README.md) for the human-facing
> overview of *what* this is (simulating a tiny brain — a fruit fly's — from spiking
> neurons up) and *why*. This file is the deeper harness/working-style reference.

The stable entry point for this project's AI harness. Keep this lean: **goals**,
**working style**, a **brief overview**, and **indexes** of plans and decisions.
Task-level detail lives in `plans/`; architecture decisions live in `../adr/`.

---

## Goals

- **Nominal goal:** simulate a single fruit fly to understand its behavior.
- **Fidelity principle:** the simulation should be **as true to biological reality as our
  current knowledge allows**. When a choice trades faithfulness for convenience, prefer
  faithfulness — and where we simplify, do it deliberately, understanding what we left out
  and why. Fidelity is a *ladder* (e.g. synapse models climb from an instantaneous kick →
  single-exponential decay → conductance-based): we don't leap to the most complex rung,
  we pick the simplest one that is biologically honest and that we can fully explain, then
  climb when a behavior actually demands it. "Make it visible/convenient" is never, on its
  own, a reason to make it less true.
- **Real goal (more important):** the user *truly understands the code* — how neurons
  work and how they are represented in code. This is a **learning project**, explicitly
  not vibe-coding a deliverable. A finished-but-not-understood program is a failure.

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
forward now (ADR 0004) with the brain kept in a Godot-free `FruitFly.Core` library that
Godot references — the eventual goal being a live dual-view (world + brain). The engine is
data-oriented so it can scale to the real connectome
(~140k neurons), though small hand-wired circuits come first.

Rationale for each of these choices is recorded as an ADR (see index below).

## Plans index

Task-level plans live in `plans/`. One plan per task/milestone.

| Plan | Status | Summary |
|------|--------|---------|
| [0001 — Milestone 0: LIF neuron](plans/0001-milestone-0-lif-neuron.md) | In progress | Single LIF neuron + synapse + 3-neuron chain; voltage trace. Get the math right before any engine. |

_Future milestones (Braitenberg seeking, decision circuit, ring-attractor compass,
internal-state/zombie behavior, connectome subgraphs) will each get their own plan when
we reach them._

## Decision index (ADRs)

Architecture Decision Records live in `../adr/`. See [`../adr/README.md`](../adr/README.md).

| ADR | Decision |
|-----|----------|
| [0001](../adr/0001-use-csharp.md) | Use C# |
| [0002](../adr/0002-bottom-up-spiking-neurons.md) | Bottom-up spiking neurons (emergent), not a top-down state machine |
| [0003](../adr/0003-leaky-integrate-and-fire.md) | Use the Leaky Integrate-and-Fire neuron model |
| [0004](../adr/0004-godot-for-visualization.md) | Use Godot (C#) for visualization, brought forward now; brain stays in `FruitFly.Core` |
