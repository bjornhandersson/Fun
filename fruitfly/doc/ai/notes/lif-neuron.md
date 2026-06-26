# Note: the LIF neuron

Explains `src/FruitFly/LifNeuron.cs`. Built up part by part as the code grows.

---

## Part 1 — state and parameters

The first version is pure *data*: what a neuron knows about itself. No behavior yet.

### State vs. parameters — the key distinction

- **State** = what *changes* moment to moment as the simulation runs. A LIF neuron has
  exactly one piece of state: `V`, its membrane voltage. That single number is the whole
  "mind" of the neuron at any instant.
- **Parameters** = the neuron's fixed *personality* — set once, then constant while it
  runs. Different neuron types differ by their parameters, not their equations.

### The members

| Member | Default | Meaning | Why this value |
|--------|---------|---------|----------------|
| `V` | starts at `VRest` | membrane voltage — the only state | a neuron at rest sits at its resting level |
| `VRest` | −65 | the level `V` settles to with no input | a realistic resting potential (~−65 mV) |
| `VThreshold` | −50 | cross this and the neuron fires | ~15 mV above rest; inputs must add up to reach it |
| `VReset` | −70 | where `V` is forced to after a spike | *below* rest → a brief recovery dip (see below) |
| `Tau` | 10 | time constant (ms) — how fast it responds/leaks | a typical, moderate responsiveness |
| `R` | 1 | membrane resistance | kept at 1 so input `I` is already in voltage units — one fewer knob for now |

### Units

We work in **millivolts (mV)** for voltage and **milliseconds (ms)** for time. Keeping
units consistent everywhere is what lets the numbers stay biologically meaningful.

### Two modeling choices worth understanding

- **`V` starts at `VRest`.** The constructor sets `V = VRest` rather than repeating the
  literal `-65`. This expresses intent ("a neuron begins at rest") and keeps the resting
  value defined in exactly one place.
- **`VReset` (−70) is *below* `VRest` (−65), not equal to it.** After firing, a real
  neuron briefly dips below its resting level before recovering — a short refractory-like
  "cool down" that makes it momentarily harder to fire again. Modeling reset *below* rest
  reproduces that for free.

### C# representation — a standard class with properties

- **Properties, not public fields.** `V` is `get; private set;` (only the neuron changes
  its own voltage); the parameters are `get; init;` with default initializers (fixed
  personality, set once at construction). This is the idiomatic C# encapsulation.
- **A reference type (`class`), not a value type.** We routinely store neurons in
  collections and mutate them every step. With a `class`, reference semantics mean
  `list[i].V = ...` (and method calls that mutate) just work — no copy gotchas. We first
  tried a mutable `record struct` for cache/allocation wins, but a *mutable* value type
  silently loses mutations through a `List<T>` indexer (the indexer returns a copy), and
  `new T[n]` skips the constructor. Reference semantics win for correctness here; the real
  performance path is a future struct-of-arrays engine, not this object. (This struct-vs-
  class choice is an implementation detail, so it lives here in the notes rather than as an
  ADR.)

---

## Part 2 — the `Step` method

This is the behavior: one tick of simulated time. It does exactly what Lessons 1–2
described — integrate + leak, then check the threshold and maybe fire.

### Signature: `bool Step(double I, double dt)`

- **`I`** — the input current arriving this step (from sensors, or from other neurons'
  spikes). With `R = 1`, `I` is already in voltage-compatible units.
- **`dt`** — the length of this time step, in ms. Smaller = more accurate; keep it well
  below `Tau` (= 10) for stability.
- **Returns `bool`** — *did the neuron spike this step?* This is the crucial part: the
  neuron's **output is the spike event itself**, not the voltage. A caller wires neurons
  together by passing one neuron's spike (the `true`) into another's `I`.

### The update line

```
V += (dt / Tau) * (-(V - VRest) + R * I);
```

This is the discretized equation, read as: *"move V a fraction `dt/Tau` of the way toward
where the forces are pushing it."* The forces are the **leak** `-(V - VRest)` (pulls back
to rest) and the **input** `R * I` (pushes up/down). Everything from Lessons 1–2 lives in
this one line.

### The trigger, and a subtlety

After integrating, we check `V >= VThreshold`. If crossed, we set `V = VReset` and return
`true`.

- **LIF does not model the spike's shape.** A real action potential is a fast voltage
  spike-and-fall; LIF abstracts that away. The spike is an *event* (the returned `true`),
  not a waveform — which is why `V` jumps straight from threshold to the reset level
  instead of drawing a peak.
- **No hard refractory period yet.** The only "cool-down" is that `VReset` (−70) sits
  below `VRest` (−65), so just after firing the neuron must climb a little farther to fire
  again. A true refractory period (a few ms where the neuron ignores input entirely) can
  be added later if a circuit needs it.

### Where firing comes from (derived, not coded)

`I` is **not** a field on the neuron — it's the argument to `Step`, the input arriving
*this step*, which can differ every step. It is never stored.

We can predict the neuron's behavior by asking the update rule where it comes to rest.
The climb stops when the nudge is zero:

```
-(V - VRest) + R*I = 0   →   V = VRest + R*I
```

So for a constant input `I`, `V` heads toward the ceiling `VRest + R*I`:

- If that ceiling is **below** `VThreshold`, the neuron rises to a quiet plateau and
  **never fires**.
- If it's **above** `VThreshold`, the neuron fires before reaching it, resets, and repeats
  → **periodic firing** (faster for larger `I`).

The minimum constant input that makes it fire at all (the *rheobase*):

```
I ≥ (VThreshold - VRest) / R = (-50 - (-65)) / 1 = 15
```

None of this is in the code — the code only holds the *rule*. This is what the rule
*implies*, worked out on paper.

---

## Part 3 — the driver (`Program.cs`)

The first runnable piece. It creates one neuron, feeds it a *constant* input for many
steps, and draws `V` as an ASCII trace so we can watch the dynamics.

### What it does

- Creates a `LifNeuron` with default parameters.
- Sets `dt = 1` ms, `I = 20` (above the ~15 rheobase → it *will* fire), `steps = 120`.
- Each step: call `Step(I, dt)`, then render the resulting `V`.

### Reading the trace

- `'|'` marks the firing threshold; `'*'` is `V` at that step.
- `'*'` climbs from rest toward `'|'`, with **shrinking** steps (the leak fighting back).
- When it crosses, the line shows `SPIKE!` — and the *next* `'*'` jumps to the far left.
  **That leftward jump is the reset to `VReset`.** The climb then repeats → a sawtooth.
- The drop *is* the spike: LIF doesn't draw an upward peak (it doesn't model spike shape);
  firing is the reset event, reported by `Step` returning `true`.

### Run it

```
dotnet run --project src/FruitFly
```

Confirmed working: the trace shows the climb → `SPIKE!` → leftward reset → repeat.

### Things to try (to feel the dynamics)

- Set `I` below 15 → no spikes; `'*'` settles at a plateau left of `'|'` (sub-rheobase).
- Raise `I` → spikes get closer together (higher firing rate).
- Shrink `dt` (e.g. `0.5`) → a smoother climb (finer Euler steps).
