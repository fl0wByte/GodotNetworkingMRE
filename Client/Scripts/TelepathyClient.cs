using Godot;
using System;

public partial class TelepathyClient : Node
{
    [Export] public string ServerAddress = "localhost";
    [Export] public int Port = 5000;
    [Export] public int MaxMessageSize = 1024;

    private Telepathy.Client client;
    public override void _Ready()
    {
        client = new Telepathy.Client(MaxMessageSize)
        {
            OnConnected = OnConnected,
            OnData = OnData,
            OnDisconnected = OnDisconnected
        };

        client.Connect(ServerAddress, Port);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        client.Tick(100);
    }

    private void OnConnected()
    {
        GD.Print($"[Client] Connected to {ServerAddress} ");
    }

    private void OnData(ArraySegment<byte> arraySegment)
    {
        int count = arraySegment.Count / sizeof(int);
        int[] numbers = new int[count];
        Buffer.BlockCopy(arraySegment.Array, arraySegment.Offset, numbers, 0, arraySegment.Count);
    }

    private void OnDisconnected()
    {
        GD.Print("[Client] Disconnected");
    }

    public override void _ExitTree()
    {
        client.Disconnect();
        base._ExitTree();
    }
}
