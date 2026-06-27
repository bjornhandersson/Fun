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
        new("2 — Two-neuron chain", "res://ChainView.tscn"),  // A → decaying synapse → B (watch B charge up)
        new("3 — Summation", "res://SummationView.tscn"),     // two inputs pool onto one output; rate tracks combined drive
        new("4 — Inhibition", "res://InhibitionView.tscn"),   // Input 2 negative: push vs pull, the brake
        new("5 — Loop", "res://LoopView.tscn"),               // A ⇄ B mutual excitation via Network; self-sustaining = memory
        new("6 — Braitenberg fly: seek + avoid", "res://BraitenbergFlyView.tscn"),  // crossed smell→motor (seek) + uncrossed wall→motor (avoid), summed; emerges
        new("7 — Two flies + walls", "res://TwoFliesView.tscn"),         // ~300k neurons each: wall-avoidance EMERGES from neurons, no bounce cheat
    };
}
