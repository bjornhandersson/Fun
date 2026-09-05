# fruitfly — simulating a tiny brain

This project builds a **brain**, from the bottom up.

Not a metaphorical brain — an actual one, the size of a fruit fly's, grown from
individual **spiking neurons** wired together. The goal is to make a fruit fly's
*behavior* (seeking food, escaping threats, getting tired) **emerge** from the wiring of
its neurons, rather than scripting that behavior with `if`-statements. We don't tell the
fly what to do; we build the neurons, connect them, and watch what falls out.

Think of it as **"assembly language for a fruit fly"**: the neuron is the instruction,
circuits of neurons are the programs, and behavior is what runs.

## Why a fruit fly?

A fruit fly's brain is about **140,000 neurons** — small enough that the whole thing fits
on one laptop core in real time, but big enough to produce rich, real behavior. Its full
wiring diagram (the *connectome*) has even been mapped. It's the perfect size to actually
understand a brain end to end.

## This is a learning project (the most important thing)

The real goal is **deeply understanding how a brain works in code** — how a single neuron
computes, and how connecting neurons creates behavior. A finished-but-not-understood
program would be a *failure*. So the work goes slowly and deliberately: concept first,
then one small, fully-explained piece of code at a time. (See
[`doc/ai/FOUNDATIONS.md`](doc/ai/FOUNDATIONS.md) for the working style.)

## How it works, in three ideas

1. **The neuron** — a *Leaky Integrate-and-Fire* (LIF) cell: a voltage that charges up
   from input, leaks back toward rest, and **fires a spike** when it crosses a threshold,
   then resets. Electrically it's a leaky capacitor with a trigger — a relaxation
   oscillator. Its output isn't a number; it's the **timing of its spikes**.
2. **The synapse** — a one-way wire between two neurons. When the source neuron spikes,
   the synapse injects current into the target, nudging it toward (or away from) firing.
   The connection's **weight** decides how strong, and its **sign** decides excite vs.
   inhibit.
3. **Circuits** — wire neurons through synapses and behavior emerges. The first target is
   a *Braitenberg vehicle*: cross-wire two sensors to two motors and watch "seeking"
   appear with no code that ever mentions seeking.

## Current status

The substrate works, and the first circuits are wired and running:

- ✅ A single **LIF neuron** that charges, fires, and resets (a periodic spike train).
- ✅ A **synapse** connecting two neurons — one neuron's spikes drive another.
- ✅ Multi-neuron circuits: a **chain**, **summation**, **inhibition**, and a self-sustaining **loop**.
- ✅ The first real circuit — a **Braitenberg fruit fly** that seeks a banana and avoids walls, both behaviours *emerging* from cross- and uncross-wired neurons.
- ✅ Scaled to **populations**: two flies of ~300,000 LIF neurons each, steered purely by population firing rates.

The early circuits run as a plain C# console viewer that draws the neurons' voltages as a
live ASCII trace (`'*'` = voltage, `'|'` = firing threshold); the full gallery above is
explored interactively in a Godot viewer:

```
   A (driven by constant input)        B (fed by A through the synapse)
                    *  |                    *               |
                     * |                     *              |
                      *|                     *              |
    *                  |     SPIKE       *                  |     SPIKE
```

## Run it (from a fresh clone)

### What you need

- **.NET SDK 10** — `dotnet --version` should print `10.x`. Get it from
  <https://dotnet.microsoft.com/download>.
- **Godot 4.7 with .NET support** (the "*.NET*" / mono build, not the standard one) — only
  needed for the visual gallery. Get it from <https://godotengine.org/download>. On macOS the
  app is assumed to live at `/Applications/Godot_mono.app`; adjust the path below if yours
  differs.

All commands are run from the repo root.

### 1. Clone and build

```
git clone <this repo> fruitfly
cd fruitfly
dotnet build src/FruitFly.slnx
```

This builds every project — the Godot-free brain (`FruitFly.Core`), the creatures
(`FruitFly.Living`), the console viewer (`FruitFly`) and the Godot viewer (`FruitFly.Godot`).
The Godot build output lands under `src/FruitFly.Godot/.godot/`, which is exactly where
Godot loads it from, so the app can be started straight from the command line.

### 2. Headless checks (console, no graphics)

```
dotnet run --project src/FruitFly
```

Runs every circuit built so far and prints a PASS/FAIL line per check (pacemaker, CPG,
the 2D fly, the 3D fly), plus live ASCII traces of the neurons' voltages (`'*'` = voltage,
`'|'` = firing threshold). One check, the 3D fly *eating*, is a documented FAIL for now:
it nails the banana's height but orbits just outside the eat radius.

### 3. The Godot gallery (watch the fly)

macOS, launched directly (Godot is usually not on `PATH`):

```
/Applications/Godot_mono.app/Contents/MacOS/Godot --path src/FruitFly.Godot &
```

Linux / Windows: run the Godot .NET executable with the same `--path src/FruitFly.Godot`
argument. A menu opens with one *play* per circuit — single neuron, chain, summation,
inhibition, loop, memory, the Braitenberg fruit fly, the wingbeat CPG, the hover, and the
united fly in 3D.

To open the project in the Godot **editor** instead, add `-e`:

```
/Applications/Godot_mono.app/Contents/MacOS/Godot -e --path src/FruitFly.Godot &
```

If Godot complains that it cannot find the game assembly, rerun step 1 (or press *Build* in
the editor) — the compiled output is not committed to git.

## Where things live

| Path | What |
|------|------|
| `src/FruitFly/` | The simulation code (C#). |
| [`CLAUDE.md`](CLAUDE.md) | Short operational guide for working in the repo. |
| [`doc/ai/FOUNDATIONS.md`](doc/ai/FOUNDATIONS.md) | Goals, working style, and indexes of plans + decisions. |
| [`doc/ai/plans/`](doc/ai/plans/) | One plan per milestone — where we think before building. |
| [`doc/ai/notes/`](doc/ai/notes/) | Deeper intuition and explanations of the code. |
| [`doc/adr/`](doc/adr/) | Architecture Decision Records — the big choices and *why*. |

## Tech

**C#** (the language), data-oriented engine, with a **Godot** live dual-view (the world +
the brain, side by side) planned once the neural math is solid. Math first; pictures later.
