# Fun

Random — but relevant — **low-level projects**, built for the joy of understanding how
things actually work.

Nothing here is about shipping products. Each project picks something that usually hides
behind an abstraction (a brain, a chip, a protocol, a physics engine…) and rebuilds it
from the smallest honest unit upward, one fully-understood piece at a time. If it can't
be explained line by line, it doesn't belong here.

## Projects

### 🪰 [fruitfly](fruitfly/) — simulating a tiny brain, neuron by neuron

A fruit fly simulation built **bottom-up from spiking neurons** in C#. No scripted
behavior, no `if (hungry) seekFood()` — just Leaky Integrate-and-Fire neurons wired
together through synapses, with behavior *emerging* from the wiring. Think *"assembly
language for a fruit fly"*: the neuron is the instruction, circuits are the programs,
behavior is what runs.

So far the fly:

- **seeks a banana by smell** — a Braitenberg circuit: two cross-wired sensors, seeking
  appears with no code that mentions seeking
- **avoids walls by touch**, and **latches a memory** of being stuck to break free
- **beats its wings on its own rhythm** — a central pattern generator from two mutually
  inhibiting, fatiguing neurons
- **hovers** — a body with mass and gravity, held aloft by a neural reflex (shove it and
  it recovers)
- **flies through 3D space** as one united creature, combining the seeking brain with the
  wingbeat altitude layer

All of it visualized live in a **Godot** viewer showing the world and the neurons side by
side. See the [project README](fruitfly/README.md) for the full story.

### 🎧 [soundcheck](soundcheck/) — hi-fi test signals from first principles

A mobile-first web page for testing headphones and audio chains. Every test signal —
sweeps, a kick drum, a Karplus–Strong plucked string, pink noise — is **synthesized
sample by sample in the browser**; nothing is downloaded. Open [`index.html`](soundcheck/index.html)
and listen.

## The common thread

- **Bottom-up.** Start from the primitive (a neuron, an opcode, a packet), not the
  framework.
- **True simulation, not pretend.** Every value must correspond to something real making
  it happen — never "whatever produces the output I want".
- **Understanding over finishing.** A working-but-not-understood program counts as a
  failure. Slow is fine; magic is not.

More projects will land here as curiosity strikes.
