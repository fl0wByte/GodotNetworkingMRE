using Godot;
using System;
using System.Collections.Generic;
using System.Net.Http;

public partial class TelepathyServer : Node
{
    [Export] public int Port = 5000;
    [Export] public int MaxMessageSize = 1024;
    private Telepathy.Server server;
    private readonly List<int> connections = [];
    private ArraySegment<byte> message;

    private double elapsedLogTime = 0;

    public override void _Ready()
    {
        server = new Telepathy.Server(MaxMessageSize)
        {
            OnConnected = OnConnected,
            OnData = OnData,
            OnDisconnected = OnDisconnected
        };

        int[] data = new int[150];

        for (int i = 0; i < data.Length; i++)
            data[i] = i + 1;

        byte[] bytes = new byte[data.Length * sizeof(int)];
        Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);
        message = new ArraySegment<byte>(bytes);

        server.Start(Port);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        server.Tick(100);

        elapsedLogTime += delta;
        if (elapsedLogTime >= 1.0)
        {
            GD.Print($"[Server] Connection count: {connections.Count}");
            elapsedLogTime = 0;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // broadcast to all
        foreach (var id in connections)
            server.Send(id, message);
    }

    private void OnConnected(int connectionId, string address)
    {
        GD.Print($"[Server] Client ({connectionId}) connected from {address}");
        connections.Add(connectionId);
    }

    private void OnData(int connectionId, ArraySegment<byte> arraySegment)
    {
        GD.Print($"[Server] Received Data from Client ({connectionId}): {BitConverter.ToString(arraySegment.Array, arraySegment.Offset, arraySegment.Count)}");
    }

    private void OnDisconnected(int connectionId)
    {
        GD.Print($"[Server] Client ({connectionId}) disconnected");
        connections.Remove(connectionId);
    }

    public override void _ExitTree()
    {
        server.Stop();
        base._ExitTree();
    }
}
