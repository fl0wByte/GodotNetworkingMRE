using Godot;
using System;

public partial class ClientNetworkManager : Node
{
    [Export]
    public string ServerAddress = "localhost";
    [Export] public int Port = 5000;
    public static ClientNetworkManager Instance { get; private set; }
    public ENetMultiplayerPeer peer = new();
    public double Ping =>
     GetServerPacketPeer()
         ?.GetStatistic(ENetPacketPeer.PeerStatistic.RoundTripTime) ?? 0;

    public override void _EnterTree()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Instance = null;
    }

    public override void _Ready()
    {
         ConnectToServer();
    }

    public void ConnectToServer()
    {
        var connection = peer.CreateClient(ServerAddress, Port, 2, 0, 0);

        if (connection != Error.Ok)
        {
            GD.Print("[Client] Failed to create client: " + connection);
        }

        peer.Host.Compress(ENetConnection.CompressionMode.None);

        Multiplayer.MultiplayerPeer = peer;
        GD.Print($"[Client] Connecting to server at {ServerAddress}:{Port}");

        Multiplayer.ConnectionFailed += OnConnectionFailed;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ServerDisconnected += OnServerDisconnected;
    }

    private void OnConnectionFailed()
    {
        GD.Print("[Client] Connection to game server failed.");

        Multiplayer.ConnectionFailed -= OnConnectionFailed;
        Multiplayer.ConnectedToServer -= OnConnectedToServer;
        Multiplayer.ServerDisconnected -= OnServerDisconnected;

        if (Multiplayer.MultiplayerPeer != null)
        {
            peer.Close();
            Multiplayer.MultiplayerPeer = null;
        }
    }

    private void OnConnectedToServer()
    {
        GD.Print("[Client] Connection to game server succeeded.");
    }

    private void OnServerDisconnected()
    {
        GD.Print("[Client] Disconnected from server (server disconnected).");
        GetTree().Quit();
    }

    private ENetPacketPeer GetServerPacketPeer()
    {
        if (Multiplayer.MultiplayerPeer is not ENetMultiplayerPeer enet)
            return null;

        if (enet.GetConnectionStatus() != MultiplayerPeer.ConnectionStatus.Connected)
            return null;

        return enet.GetPeer(1);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable, TransferChannel = 1)]
    public void ReceiveConnectionCount(int connectionsCount)
    {
        MonitorUI.Instance.SetConnectionCount(connectionsCount);
    }
}
