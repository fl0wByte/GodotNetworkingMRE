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
    private int connectionCount;

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
        Multiplayer.MultiplayerPeer = peer;

        GD.Print($"[Server] Server started on port {Port}");

        Multiplayer.PeerConnected += PeerConnected;
        Multiplayer.PeerDisconnected += PeerDisconnected;
    }

    private void PeerConnected(long id)
    {
        GD.Print($"[Server] User connected with ID: {id}");
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
