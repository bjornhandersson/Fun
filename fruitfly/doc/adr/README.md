# Architecture Decision Records

This folder records **significant, hard-to-reverse architecture decisions** and *why* they
were made, so the reasoning survives even when the choice is no longer obviously
questionable. Keep the bar high: ADRs are for *big* directional choices (language, model,
overall approach) — **not** local implementation details (e.g. whether a type is a `struct`
or a `class`). Those belong in code comments or `doc/ai/notes/`.

## Format

Each ADR is a numbered file `NNNN-short-title.md` with:

- **Status** — Proposed / Accepted / Superseded (by ADR-XXXX)
- **Context** — the forces and constraints at the time
- **Decision** — what we chose
- **Consequences** — what this gains and gives up

Keep them short. One decision per file. Never edit a decided ADR's intent; instead add a
new ADR that supersedes it.

## Index

| ADR | Status | Decision |
|-----|--------|----------|
| [0001](0001-use-csharp.md) | Accepted | Use C# |
| [0002](0002-bottom-up-spiking-neurons.md) | Accepted | Bottom-up spiking neurons (emergent), not a top-down state machine |
| [0003](0003-leaky-integrate-and-fire.md) | Accepted | Use the Leaky Integrate-and-Fire (LIF) neuron model |

### Backlog (decisions made, ADR not yet written)

- Full struct-of-arrays (SoA) engine (CSR sparse synapses, event-driven spikes) — the real
  high-performance data layout, to be written when we build the engine at scale.
- Godot (C#) for the visualization layer — to be confirmed/written when we start the viz.
