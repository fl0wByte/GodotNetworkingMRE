using Godot;

public partial class IngameUi : Control
{
    public void QuitButtonPressed()
    {
        GetTree().Quit();
    }
}
