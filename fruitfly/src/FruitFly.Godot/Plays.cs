// The single source of truth for the gallery.
//
// Each "step forward" in the brain project is a Play: a self-contained Godot scene that
// builds its circuit from FruitFly.Core and draws it. To add a milestone, add ONE line
// here — the menu (PlayMenu) builds its buttons from this list automatically.
//
// This file deliberately uses no Godot types: it's just data describing the gallery.
public static class Plays
{
    // Title = what the menu button says. ScenePath = the .tscn to load when clicked.
    public readonly record struct Play(string Title, string ScenePath);

    public static readonly Play[] All =
    {
        new("1 — Single neuron", "res://NeuronView.tscn"),
        new("2 — Two-neuron chain", "res://ChainView.tscn"), // A → decaying synapse → B (watch B charge up)
        new("3 — Summation", "res://SummationView.tscn"), // two inputs pool onto one output; rate tracks combined drive
        new("4 — Inhibition", "res://InhibitionView.tscn"), // Input 2 negative: push vs pull, the brake
        new("5 — Loop", "res://LoopView.tscn"), // A ⇄ B mutual excitation via Network; self-sustaining = memory
        new("5b — Memory: one self-exciting neuron", "res://MemoryNeuronView.tscn"), // Plan 0002 in isolation: one cell + a self-synapse; poke → hold → release
        new("6 — Braitenberg fly: seek + avoid + memory", "res://BraitenbergFlyMemoryView.tscn"), // the assembled fly (FruitFly.Living): crossed smell→motor (seek) + uncrossed touch→motor (avoid) + a self-exciting memory neuron. SPACE pokes it
        new("7 — Wingbeat CPG: a self-made rhythm", "res://WingbeatCpgView.tscn"), // Plan 0007, rung 1 of flight: two neurons (mutual inhibition + fatigue) generate their own beat — the motor's first self-generated rhythm
        new("8 — Hover & seek: flies to the food", "res://HoverView.tscn"), // Plan 0008: gravity + lift; a hover reflex holds altitude AND vertical chemotaxis climbs to the food's smell. Move the food (↑/↓), shove the fly (Space)
        new("9 — United fly in 3D: flies to the banana", "res://Fly3DView.tscn"), // Plan 0009: the 2D seek/avoid/memory brain UNITED with the wingbeat altitude layer — one creature that flies through 3D space and eats in 3D. Lift is the honest beat; forward thrust is still the 2D abstraction
    };
}
