# ADR 0003 — Use the Leaky Integrate-and-Fire (LIF) neuron model

**Status:** Accepted

## Context

Spiking neuron models span a complexity spectrum:

- **Rate / McCulloch-Pitts** — just a weighted sum + nonlinearity; not truly spiking.
- **Leaky Integrate-and-Fire (LIF)** — membrane voltage integrates input and leaks;
  fires + resets at a threshold. ~5 lines of code.
- **Izhikevich** — two coupled ODEs; reproduces many firing patterns; still cheap.
- **Hodgkin-Huxley** — biophysically detailed (ion channels); expensive and complex.

We need the *minimal* model that still captures the essential idea that a neuron's output
is **spike timing**, and that a beginner can fully understand line by line.

## Decision

Use **LIF** as the neuron model — the "instruction set" for the whole project.

## Consequences

- Minimal, fully understandable, and cheap; scales to the full connectome.
- Captures spike-timing dynamics (unlike rate models).
- Gives up biophysical detail and richer firing patterns (Izhikevich / Hodgkin-Huxley).
  Acceptable now; can be revisited per-neuron-type later if a circuit needs it.
