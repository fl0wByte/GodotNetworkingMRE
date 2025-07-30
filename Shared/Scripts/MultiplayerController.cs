using System.Linq;
using Godot;

public partial class MultiplayerController : Node
{
    public static MultiplayerController Instance { get; private set; }
    public bool IsServer = false;
    public bool IsHeadless = false;
    public string GameServerScenePath = "res://Server/Scenes/GameServer.tscn";
    public string ClientScenePath = "res://Client/Scenes/Client.tscn";

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
        IsHeadless = DisplayServer.GetName() == "headless" || OS.HasFeature("headless");
        Initialize();
    }

    public void Initialize()
    {
        GD.Print("MultiplayerController Initialize");
        var args = OS.GetCmdlineUserArgs();

        IsServer = OS.HasFeature("dedicated_server") || args.Contains("--server");

        if (IsServer)
        {
            LoadServer();
            return;
        }
        else
        {
            if (IsHeadless)
            {
                Engine.MaxFps = 0;
                Engine.PhysicsTicksPerSecond = 60;
                RenderingServer.RenderLoopEnabled = false;
                OS.LowProcessorUsageMode = true;
                OS.LowProcessorUsageModeSleepUsec = 16_000;
            }
            LoadClient();
            return;
        }
    }

    private void LoadServer()
    {
        GD.Print("Load GameServer ...");
        var packedScene = GD.Load<PackedScene>(GameServerScenePath);
        if (packedScene != null)
        {
            var gameServer = packedScene.Instantiate();
            AddChild(gameServer);
            GD.Print("GameServer loaded!");
        }
        else
        {
            GD.PushError($"GameServer scene not found at path: {GameServerScenePath}");
        }
    }

    private void LoadClient()
    {
        GD.Print("Load Client ...");
        var packedScene = GD.Load<PackedScene>(ClientScenePath);
        if (packedScene != null)
        {
            Node client = packedScene.Instantiate();
            AddChild(client, true);
            GD.Print("Client loaded!");
        }
        else
        {
            GD.PushError($"Client scene not found at path: {ClientScenePath}");
        }
    }
}
