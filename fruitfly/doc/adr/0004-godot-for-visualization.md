# ADR 0004 — Use Godot (C#) for visualization, brought forward now

**Status:** Accepted

## Context

Two prior positions are revisited here:

- Godot (C#) was the *intended* visualization layer but only a backlog item ("confirm
  when we start the viz").
- The plan was **"math first, GUI later"** — keep everything to plain C# until the neuron
  dynamics were solid.

In practice, every experiment required hand-editing `Program.cs` (drive, dt, steps, weights)
and recompiling. That friction is now the main thing slowing down *understanding the
dynamics* — which is the project's real goal. We evaluated lighter options (interactive
terminal UI, a pure-.NET `HttpListener` + browser view, Avalonia). The user chose Godot:
it is already the project's long-term target for the live world+brain dual-view, so the
work is not throwaway, and a Godot editor as a **dev-time** dependency is acceptable.

The risk is coupling the neuron math to a game engine. We avoid that with a hard boundary
(see Decision).

## Decision

1. Use **Godot (.NET / C# build)** as the visualization layer, and **build it now** rather
   than deferring it — superseding the "GUI later" stance for everything except scope
   (we still build the engine only after the single-neuron + synapse math is correct,
   which it is).
2. Keep the brain in a **Godot-free class library, `FruitFly.Core`** (`LifNeuron`,
   `Synapse`, future circuits). The Godot project and the console runner are both *viewers*
   that reference Core. The neuron math never depends on Godot.

## Consequences

- A real, interactive GUI: tweak parameters with sliders and watch the dynamics live —
  directly serving the learning goal.
- Godot becomes a **dev dependency** (editor to develop; the engine is bundled into any
  exported build). This is heavier than a pure-.NET option, and accepted deliberately.
- The Core/viewer split keeps the math pure and testable, and means the eventual
  world+brain dual-view reuses the same library — no rework.
- Godot's .NET build must support Core's target framework (a library can't target newer
  than its consumer). We reconcile the TFM against the installed Godot version.
