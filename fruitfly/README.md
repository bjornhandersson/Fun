# fruitfly

A fruit fly simulated from the neurons up.

Not a program that behaves like a fly, but a network of spiking neurons wired the way a
fly's might be, with behaviour left to fall out of the wiring. The fly is never told what to
do. There is no `if (hungry)` anywhere; anything that decides for the fly in ordinary code
gets pushed back into the network. The only algorithmic parts are the scaffolding: the
world, the body, and the transducers that turn smell into input current and motor spikes
into thrust.

## Why a fruit fly

About 140,000 neurons. Small enough to simulate on a laptop, big enough to produce real
behaviour, and the only animal of that size whose complete connectome has been mapped.
There is an answer key.

## The three pieces

**Neuron.** A leaky integrate-and-fire (LIF) cell. Membrane voltage charges from input
current, leaks back towards rest, fires a spike at threshold and resets. A leaky capacitor
with a trigger. Its output is the timing of its spikes, not a number.

**Synapse.** A one-way connection. When the presynaptic neuron spikes, the synapse injects
current into the postsynaptic one. Weight sets how much, sign sets excitatory or inhibitory.

**Circuit.** Neurons joined by synapses. Circuits do things their parts don't: cross-wire
two smell sensors to two motors and the fly climbs the odour gradient; let two neurons
inhibit each other and fatigue and you get a half-centre oscillator, a wingbeat nobody
programmed.

## What the fly does so far

- Smells its way to a banana. A Braitenberg vehicle with contralateral wiring.
- Feels walls by touch and turns away.
- Remembers being stuck. One self-exciting neuron latches and drives an escape until it
  clears.
- Beats its wings on its own rhythm from a two-neuron central pattern generator.
- Hovers. The body has mass, gravity acts on it, a neural reflex holds altitude.
- Flies to the banana in 3D as one creature.

Known gap: in 3D it nails the banana's height but orbits just outside the eat radius. That
check prints FAIL and stays FAIL until the wiring fixes it, not a tweaked constant.

## Seeing it

Early circuits print membrane voltages as a scrolling ASCII trace (`*` voltage,
`|` threshold):

```
   A (driven by constant input)        B (fed by A through the synapse)
                    *  |                    *               |
                     * |                     *              |
                      *|                     *              |
    *                  |     SPIKE       *                  |     SPIKE
```

The full set runs in a Godot viewer, one scene per circuit: single neuron, chain, summation,
inhibition, loop, memory neuron, Braitenberg fly, wingbeat CPG, hover, and the united fly in
3D.

## Running it

Needs the .NET 10 SDK, plus Godot 4.7 with .NET support for the viewer. Run from this
folder.

```
dotnet build src/FruitFly.slnx
dotnet run --project src/FruitFly        # headless checks, PASS/FAIL per circuit
```

Godot viewer (macOS; adjust the path if yours differs, add `-e` for the editor):

```
/Applications/Godot_mono.app/Contents/MacOS/Godot --path src/FruitFly.Godot &
```

If Godot can't find the game assembly, build again; compiled output isn't committed.

## Where things are

| Path | What |
|------|------|
| `src/FruitFly.Core/` | The brain: LIF neurons, synapses, spiking network. No Godot dependency. |
| `src/FruitFly.Living/` | The fly, its flying body, the half-centre oscillator, the world. |
| `src/FruitFly/` | Console runner and headless checks. |
| `src/FruitFly.Godot/` | The viewer. |
| [`doc/adr/`](doc/adr/) | Architecture decisions and why. |
| [`doc/ai/plans/`](doc/ai/plans/) | One plan per milestone. |
| [`doc/ai/notes/`](doc/ai/notes/) | Plain-language explanations of the code. |
| [`doc/ai/FOUNDATIONS.md`](doc/ai/FOUNDATIONS.md) | The working rules. |

This is a learning project. Every piece gets understood before the next one goes in, so
progress is counted in circuits that can be explained, not features that demo well.
