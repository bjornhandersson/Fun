# Plan 0001 — Milestone 0: the LIF neuron

**Status:** In progress
**Goal:** Build and run a single Leaky Integrate-and-Fire neuron, then a synapse, then a
3-neuron chain, producing a voltage trace we can eyeball. Get the neuron *math* right
before introducing any game engine. Plain C# console/library — no Godot yet.

## Why this first

Everything in the project is composition of one neuron. If the single-neuron dynamics
are correct and understood, circuits are just wiring. This milestone is the foundation
the whole bottom-up approach rests on.

## The concept (Lesson 1 — delivered)

A neuron = a **leaky bucket with a trigger**:
- **Integrate** — inputs raise the membrane voltage `V`.
- **Leak** — with no input, `V` drifts back toward rest.
- **Fire** — when `V` crosses a threshold, emit a spike and reset.

Governing equation (continuous):

```
τ dV/dt = -(V - V_rest) + R·I
```

- `V` — membrane voltage (the only state).
- `-(V - V_rest)` — leak; pulls `V` toward rest, harder the further away.
- `R·I` — input current `I` scaled by membrane resistance `R`.
- `τ` (tau) — time constant; how fast the neuron responds/leaks.

Plus the discontinuous rule: `if V ≥ V_threshold: spike; V = V_reset`.

**Core idea:** a neuron's output is **spike timing**, not an analog value.

## Steps

- [x] Lesson 1: understand the single LIF neuron (concept + equation).
- [x] Lesson 2: discretize the ODE with Euler's method → runnable update step.
- [x] Write the `LifNeuron` class (assistant writes, tiny parts; see `notes/lif-neuron.md`).
  - [x] Part 1: state + parameters.
  - [x] Part 2: `Step(I, dt)` — the update line + threshold/reset.
  - [x] Part 3: a `Program` driver that feeds constant `I` and prints a voltage trace.
- [x] Run it — confirmed: under constant `I` it charges and fires periodically (the sawtooth).
- [~] Add a `Synapse` (on source spike, add weight to target's input).
  - [x] Part 1: the `Synapse` type — source, target, weight, and `Current(sourceFired)`
    (the spike-event → current adapter). Simplest model: same-step, one-step kick.
- [ ] Wire a 3-neuron chain (input → middle → output).
- [ ] Emit a voltage trace (CSV or ASCII) and eyeball the dynamics.

## Deferred (start simple; add when a circuit needs it)

- **Synaptic delay.** Right now the target feels the current on the *same* step the
  source fired. Real synapses have a transmission delay (~ms); add it later (e.g. a
  small queue / ring buffer per synapse) so a spike arrives N steps after it's sent.
- **PSP shape (lingering current).** Right now a spike is a one-step kick. Real synaptic
  input rises and decays over a few ms; model it later as a decaying current instead of
  an instantaneous pulse.

## Open questions to resolve with the user

- Does the leaky-bucket equation land? (Especially: why the leak is `-(V - V_rest)`,
  not `-V`.)

_Resolved: working mode is **(b) assistant writes and narrates each line** (see FOUNDATIONS
"Working style")._

## Done when

A 3-neuron chain runs, the user can explain every line, and the voltage trace shows
charge → fire → reset propagating through the chain.
