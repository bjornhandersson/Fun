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

Early days — the substrate is taking shape:

- ✅ A single **LIF neuron** that charges, fires, and resets (a periodic spike train).
- ✅ A **synapse** connecting two neurons — one neuron's spikes now drive another.
- ⏭️ Next: a small chain of neurons, then the first real circuit.

It runs as a plain C# console program that draws the neurons' voltages as a live ASCII
trace (`'*'` = voltage, `'|'` = firing threshold):

```
   A (driven by constant input)        B (fed by A through the synapse)
                    *  |                    *               |
                     * |                     *              |
                      *|                     *              |
    *                  |     SPIKE       *                  |     SPIKE
```

## Run it

Requires the .NET SDK.

```
dotnet run --project src/FruitFly
```

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
