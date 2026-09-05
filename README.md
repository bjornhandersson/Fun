# Fun

Side projects built to understand things, not to ship them. Each one takes something that
usually hides behind an abstraction and rebuilds it from the smallest piece that can be
explained.

## Projects

### [fruitfly](fruitfly/) — a tiny brain, one neuron at a time

A fruit fly simulated bottom-up from leaky integrate-and-fire neurons and synapses, nothing
else. No line says "if hungry, go to the banana"; the fly seeks the banana because two
smell sensors are cross-wired to two motors.

So far it smells its way to food, feels walls and turns away, latches a memory of being
stuck to break free, beats its wings on a rhythm from two mutually inhibiting neurons, and
hovers in 3D on a reflex against gravity. Runs live in a Godot viewer with the world on one
side and the neurons spiking on the other. C#.

### [soundcheck](soundcheck/) — compare headphones with your own ears

Headphone reviews are written in adjectives. This web page replaces them with numbers you
measure yourself: slide a sine down until it stops being a pitch and that is bass extension
in Hz; level-match 100 Hz against 1 kHz and that is bass tilt in dB. Marks go on a
scoreboard, one column per pair, and whatever differs is the gear.

Every signal is synthesised in the browser with the Web Audio API. Nothing is downloaded, so
nothing has been through a lossy codec. Open [`index.html`](soundcheck/index.html) on a
phone.

## The common thread

Start from the primitive, not the framework. A small thing that is understood completely
beats a big thing that works for reasons nobody can follow.
