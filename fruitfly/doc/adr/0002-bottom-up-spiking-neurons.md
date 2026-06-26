# ADR 0002 — Bottom-up spiking neurons (emergent), not a top-down state machine

**Status:** Accepted

## Context

There are two ways to model fly behavior in software:

- **Top-down:** write the behavior as explicit rules / a state machine
  (`if odor: turn_toward()`, `SEARCH → TRAPPED → EXHAUSTED`).
- **Bottom-up:** build the substrate (spiking neurons + synapses) and let behavior
  **emerge** from how neurons are wired together.

The project's real goal is to *understand neural computation* — how neurons work and how
they are represented in code. A top-down state machine would model the behavior without
touching that goal.

## Decision

Build **bottom-up**: implement neurons and synapses, then create behavior by **wiring
circuits**. Do not script behavior with a top-down state machine. Mental model:
"assembly for an 8-bit chip, but for a fruit fly" — the neuron is the instruction set,
circuits are the programs.

## Consequences

- Directly serves the learning goal: behavior is explained by mechanism, not by rules.
- More work to get any given behavior to appear; behavior must be coaxed via wiring.
- Richer and more authentic — conflicts (e.g. attraction vs. escape) emerge from
  competing drives rather than being hand-coded.
- Opens a path to later load real connectome data and assign dynamics.
