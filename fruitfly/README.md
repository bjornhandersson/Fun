# fruitfly

A fruit fly simulated from the neurons up, in C#.

Nothing in here tells the fly what to do. There is no `if (hungry)`, no steering code, no
"move toward the banana" function. There are neurons that leak and fire, synapses that pass
current between them, and a body that turns smell into input current and motor spikes into
thrust. Wire them right and the fly finds the banana anyway. That is the whole project:
watching behaviour fall out of wiring.

## Try it in 30 seconds

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download). For the graphical viewer
you also need [Godot 4.7 with .NET support](https://godotengine.org/download) (the ".NET"
download, not the standard one).

```bash
git clone https://github.com/bjornhandersson/Fun.git
cd Fun/fruitfly
./start.sh            # builds, then opens the viewer: a menu of circuits, click one
```

No Godot? The same brain runs in the terminal:

```bash
./start.sh console    # builds, then runs every circuit headless and prints PASS / FAIL
```

Using VS Code? Open the `fruitfly` folder (not its parent) and press **F5**. The default
launch runs the headless checks. Pick "Godot viewer" in the Run and Debug dropdown to open
the gallery instead. Install the recommended extensions when prompted.

If `start.sh` cannot find Godot, point it there:

```bash
GODOT=/path/to/godot ./start.sh
```

## What you will see

The viewer is a gallery. Each entry is one circuit, built in order of complexity, and each
one adds exactly one idea to the previous. Play 6 is the fly. Play 7 is the first piece of the
next thing, flight.

| # | Play | The one new idea |
|---|------|------------------|
| 1 | Single neuron | A leaky integrate-and-fire cell. Watch voltage climb, hit threshold, spike, reset. |
| 2 | Two-neuron chain | A synapse. Spikes in A become current in B. |
| 3 | Summation | Two inputs onto one neuron. The output rate tracks the combined drive. |
| 4 | Inhibition | A negative synapse. Push versus pull. |
| 5 | Loop | Two neurons exciting each other keep firing after the input stops. Memory. |
| 5b | Memory neuron | The same trick with one cell and a self-synapse. Poke it, it holds. |
| 6 | Braitenberg fly | The assembled fly. Smells its way to a banana, feels walls, remembers being stuck. Press Space to poke it. |
| 7 | Wingbeat CPG | Two neurons that inhibit each other and tire, driven by a self-firing pacemaker. A rhythm nobody programmed. |

The terminal version prints a membrane trace for the early circuits, `*` for voltage and
`|` for threshold:

```
   A (driven by constant input)        B (fed by A through the synapse)
                    *  |                    *               |
                     * |                     *              |
                      *|                     *              |
    *                  |     SPIKE       *                  |     SPIKE
```

## How it works

Three pieces. Everything else is composition.

**A neuron** is a leaky capacitor with a trigger. Its voltage `V` charges from input current
`I`, leaks back toward rest, and when it crosses threshold it fires and resets. The whole
update is one Euler step (from `src/FruitFly.Core/LifNeuron.cs`):

```csharp
V += (dt / Tau) * (-(V - VRest) + R * I - Adaptation);

if (V >= VThreshold)
{
    V = VReset;
    Adaptation += AdaptKick;   // fatigue: each spike makes the next one a little harder
    return true;               // spiked
}
return false;
```

Default constants are textbook values: rest at -65 mV, threshold at -50 mV, reset to -70 mV,
membrane time constant 10 ms. The output of a neuron is *when* it spikes, not a number.

**A synapse** is a one-way pipe with a leak. When the source neuron fires, the synapse
dumps `Weight` units of current onto whatever is still lingering from earlier spikes. The
current then decays. Positive weight excites, negative weight inhibits. Stacking is how
inputs summate over time.

```csharp
Current += (dt / TauSyn) * (-Current);   // fade
if (sourceFired) Current += Weight;      // kick
return Current;
```

**A circuit** is neurons joined by synapses. Circuits do things their parts cannot:

- Cross-wire a left smell sensor to a right motor and vice versa, and the fly turns toward
  the stronger smell. That is a Braitenberg vehicle, and it is Play 6.
- Let two neurons inhibit each other while each one tires, and they take turns firing. That
  is a half-centre oscillator, and it is the wingbeat in Play 7.
- Let a neuron excite itself and a single poke keeps it firing until something shuts it
  off. That is the memory in Play 5b.

For big populations the same equation runs in `SpikingNet`, a struct-of-arrays layout with
sparse synapses in CSR form and event-driven spike delivery. The hand-wired circuits use
`LifNeuron` and `Synapse` objects because they are easier to read, and reading is the point.

## The one rule

Every behaviour has to be *caused* through neurons, never faked to look right. No
background current injected to keep the fly moving. No `if (stuck) turn`. No nudging a motor
because the demo looked dull. If a neuron at rest stays silent, that is the model being
honest, not a bug to hide.

The rule has teeth. Two plays that used to follow Play 7, a hover and a fly in 3D, were
deleted in September 2026 because they broke it. The fly was handed the banana's height
directly and climbed a private odour field that did not exist in the world, and its 3D body
was two separate networks stepped side by side and drawn as one animal. They looked good.
They were not true. [ADR 0006](doc/adr/0006-remove-unfaithful-flight-plays.md) records what
went wrong and how flight will be rebuilt from Play 7 instead.

## Why a fruit fly

About 140,000 neurons. Small enough to simulate on a laptop, large enough to do real
things, and the only animal of that size whose complete connectome has been mapped. When
the hand-built circuits run out, there is an answer key to compare against.

## Where things are

| Path | What |
|------|------|
| `start.sh` | Build and run: viewer, console, or editor. |
| `src/FruitFly.Core/` | The brain: `LifNeuron`, `Synapse`, `Network`, `SpikingNet`. No Godot dependency. |
| `src/FruitFly.Living/` | The creature: `Fly`, `HalfCentreOscillator`, `World`. |
| `src/FruitFly/` | Console runner. One headless check per milestone, each printing PASS or FAIL. |
| `src/FruitFly.Godot/` | The viewer. One scene per Play; `Plays.cs` is the gallery list. |
| [`doc/adr/`](doc/adr/) | Architecture decisions and the reasoning behind them. |
| [`doc/ai/plans/`](doc/ai/plans/) | One plan per milestone, written before building. |
| [`doc/ai/notes/`](doc/ai/notes/) | Plain-language explanations of the code. |
| [`doc/ai/FOUNDATIONS.md`](doc/ai/FOUNDATIONS.md) | The working rules for the project. |

## Troubleshooting

- **Godot warns about invalid UIDs on first launch.** Harmless. Godot rebuilds its cache in
  `.godot/`, which is not committed. The warning goes away on the second run.
- **Godot cannot find the game assembly.** Run `./start.sh` again, or `dotnet build
  src/FruitFly.slnx`. Compiled output is not committed.
- **Godot regenerated the csproj and the viewer lost sight of `LifNeuron` or `Fly`.** Check
  that `src/FruitFly.Godot/FruitFly.Godot.csproj` still references the Core and Living
  projects. Godot sometimes strips them.

## About the pace

This is a learning project. Every piece is understood before the next one goes in, so
progress is counted in circuits that can be explained line by line, not in features that
demo well. If a file looks over-commented, that is why.
