using Godot;
using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;

public partial class ClientNetworkManager : Node
{
    [Export] public string ServerAddress = "localhost";
    [Export] public int Port = 5000;
    public static ClientNetworkManager Instance { get; private set; }
    private ENetMultiplayerPeer peer = new();
    private SceneMultiplayer api = new();
    private Stopwatch _handshakeTimer;

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
        _handshakeTimer = Stopwatch.StartNew();
        var connection = peer.CreateClient(ServerAddress, Port, 2, 0, 0);

        if (connection != Error.Ok)
        {
            GD.Print("[Client] Failed to create client: " + connection);
        }

        peer.Host.Compress(ENetConnection.CompressionMode.None);


        api.MultiplayerPeer = peer;


        GetTree().SetMultiplayer(api);

        GD.Print($"[Client] Connecting to server at {ServerAddress}:{Port}");
        api.ConnectionFailed += OnConnectionFailed;
        api.ConnectedToServer += OnConnectedToServer;
        api.ServerDisconnected += OnServerDisconnected;
    }

    private void OnConnectionFailed()
    {
        GD.Print("[Client] Connection to game server failed.");

        api.ConnectionFailed -= OnConnectionFailed;
        api.ConnectedToServer -= OnConnectedToServer;
        api.ServerDisconnected -= OnServerDisconnected;

        if (api.MultiplayerPeer != null)
        {
            peer.Close();
            api.MultiplayerPeer = null;
        }
    }

    private void OnConnectedToServer()
    {
        GD.Print("[Client] Connection to game server succeeded.");
        _handshakeTimer.Stop();
        GD.Print($"[Client] Handshake took {_handshakeTimer.Elapsed.TotalMilliseconds:F2} ms");
    }

    private void OnServerDisconnected()
    {
        GD.Print("[Client] Disconnected from server (server disconnected).");
        GetTree().Quit();
    }

    private ENetPacketPeer GetServerPacketPeer()
    {
        if (api.MultiplayerPeer is not ENetMultiplayerPeer enet)
            return null;

        if (enet.GetConnectionStatus() != MultiplayerPeer.ConnectionStatus.Connected)
            return null;

        return enet.GetPeer(1);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable, TransferChannel = 0)]
    public void ReceiveArray(Array array)
    {
        return;
    }
}
