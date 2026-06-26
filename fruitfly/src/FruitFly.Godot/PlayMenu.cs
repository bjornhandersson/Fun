using Godot;

// The gallery screen: lists every Play (see Plays.cs) as a button and loads the one you
// click. This is the app's main scene, so you always start here and pick what to watch.
//
// The button list is built in code from Plays.All, so the menu grows itself as we register
// new milestones — there's no per-Play UI to hand-author.
public partial class PlayMenu : Control
{
    public override void _Ready()
    {
        // A simple vertical stack, inset from the top-left corner.
        var box = new VBoxContainer { Position = new Vector2(60, 60) };
        AddChild(box);

        box.AddChild(new Label { Text = "FruitFly — Plays" });

        foreach (var play in Plays.All)
        {
            var button = new Button { Text = play.Title };
            string path = play.ScenePath;   // capture per-iteration so the lambda loads the right scene
            button.Pressed += () => GetTree().ChangeSceneToFile(path);
            box.AddChild(button);
        }
    }
}
