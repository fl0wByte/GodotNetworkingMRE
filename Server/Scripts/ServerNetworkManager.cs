using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ServerNetworkManager : Node
{
    public static ServerNetworkManager Instance { get; private set; }
    [Export] public int Port = 5000;
    [Export] public int MaxPlayers = 4095;
    private ENetMultiplayerPeer peer = new();
    private SceneMultiplayer api = new();
    private int connectionCount;
    private const double FrameThresholdSeconds = 1.0 / 60.0;           // ≈ 0.0166667 s
    private int _physicsProcessCount = 0;
    private double _accumulatedTime = 0.0;


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
        StartServer();
    }

    public override void _PhysicsProcess(double delta)
    {
        _physicsProcessCount++;
        _accumulatedTime += delta;

        if (delta > FrameThresholdSeconds)
        {
            GD.PrintErr($"[Warning] _PhysicsProcess delta too high: {delta}s (> {FrameThresholdSeconds:F2}s)");
        }

        // Every one second…
        if (_accumulatedTime >= 1.0)
        {
            GD.Print($"[Performance] PhysicsProcess/sec = {_physicsProcessCount}");
            GD.Print($"[Server] Connected clients: {connectionCount}");

            // reset
            _physicsProcessCount = 0;
            _accumulatedTime -= 1.0;
        }

        SendConnectionCountToAllClients();
    }

    private void StartServer()
    {
        var err = peer.CreateServer(Port, MaxPlayers, 2, 0, 0);
        if (err != Error.Ok)
        {
            GD.PushError($"[Server] Failed to start server on port {Port}: {err}");
            return;
        }
        peer.Host.Compress(ENetConnection.CompressionMode.None);


        api.ServerRelay = false;
        api.MultiplayerPeer = peer;

        GetTree().SetMultiplayer(api);

        GD.Print($"[Server] Server started on port {Port}");

        api.PeerConnected += PeerConnected;
        api.PeerDisconnected += PeerDisconnected;
    }

    private void PeerConnected(long id)
    {
        GD.Print($"[Server] User connected with ID: {id}");
        ENetPacketPeer packetPeer = peer.GetPeer((int)id);
        packetPeer.SetTimeout(1500, 30000, 60000);
        packetPeer.PingInterval(2000);

        connectionCount++;
    }

    private void PeerDisconnected(long id)
    {
        GD.Print($"[Server] User disconnected with ID: {id}");
        connectionCount--;
    }

    private void SendConnectionCountToAllClients()
    {
        ReceiveConnectionCount(connectionCount);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable, TransferChannel = 0)]
    public void ReceiveConnectionCount(int connectionsCount)
    {
        RpcId(0, nameof(ReceiveConnectionCount), connectionsCount);
    }
}
