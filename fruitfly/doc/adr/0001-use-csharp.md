# ADR 0001 — Use C#

**Status:** Accepted

## Context

This is a personal learning project (see `doc/ai/FOUNDATIONS.md`). Languages considered:

- **C#** — the user's strongest language.
- **TypeScript** — well known to the user; would enable a browser-shareable visualization.
- **Rust** — the user wants to learn it and is drawn to its efficiency.
- **Python** — acceptable fallback; fast to prototype.

Key fact: the simulation is **computationally tiny** (even the full fly connectome runs in
real time on one CPU core). So raw performance is *not* a deciding factor. The real
constraints are iteration speed, visualization quality, and whether the language is a
help or a distraction. The project's priority is **learning the fly brain, not learning a
new language.**

## Decision

Use **C#**.

## Consequences

- Fastest path to the actual subject (neurons) in the user's strongest language.
- Visualization will use **Godot (C#)** — see backlog ADR.
- Good fit for the planned data-oriented engine (structs, `Span<T>`, arrays).
- Gives up: the Rust learning side-goal, and TypeScript's URL-shareable web visualization.
  These were judged secondary to momentum on the core goal.
