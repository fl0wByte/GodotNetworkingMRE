using Godot;
using System;

public partial class MonitorUI : Control
{

    [Export] public Label PingLabel;
    [Export] public Label ConnectionCountLabel;
    public static MonitorUI Instance { get; private set; }

    public override void _EnterTree()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;
    }

    public override void _Ready()
    {
        // We dont need to process UI on Bot Clients
        if (MultiplayerController.Instance.IsHeadless)
            SetProcess(false);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Instance = null;
    }

    public override void _Process(double delta)
    {
        if (ClientNetworkManager.Instance != null)
        {
            PingLabel.Text = $"Ping: {ClientNetworkManager.Instance.Ping} ms";
        }
        else
        {
            PingLabel.Text = "Ping: N/A";
        }
    }

    public void SetConnectionCount(int count)
    {
        ConnectionCountLabel.Text = $"Connections: {count}";
    }
}
