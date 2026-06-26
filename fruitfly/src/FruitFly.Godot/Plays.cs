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
        // 2 — Two-neuron chain   (A drives B)            — next
        // 3 — Decaying synapse   (watch B charge up)
        // 4 — Coincidence cluster (2 inputs → 1 output, AND emerges)
    };
}
