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
        int ping = (int)GetServerPacketPeer()?.GetStatistic(ENetPacketPeer.PeerStatistic.RoundTripTime);
        PingLabel.Text = $"Ping: {ping:F0} ms";
    }

    private ENetPacketPeer GetServerPacketPeer()
    {
        if (Multiplayer.MultiplayerPeer is not ENetMultiplayerPeer enet)
            return null;

        if (enet.GetConnectionStatus() != MultiplayerPeer.ConnectionStatus.Connected)
            return null;

        return enet.GetPeer(1);
    }
}
