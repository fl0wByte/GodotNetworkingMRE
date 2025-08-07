extends Node
class_name ServerNetworkManager

@export var Port: int = 5000
@export var MaxPlayers: int = 4095

# Singleton instance
static var Instance: ServerNetworkManager = null

var peer: ENetMultiplayerPeer
var api: SceneMultiplayer
var connection_count: int = 0

const FRAME_THRESHOLD_SECONDS := 1.0 / 60.0 # ≈ 0.0166667 s
var _physics_process_count: int = 0
var _accumulated_time: float = 0.0

var fixed_array: Array = []

func _enter_tree() -> void:
	if Instance != null:
		queue_free()
		return
	Instance = self

func _exit_tree() -> void:
	Instance = null

func _ready() -> void:
	for i in range(150):
		fixed_array.append(i)

	# Initialize peer & API
	peer = ENetMultiplayerPeer.new()
	api = SceneMultiplayer.new()
	_start_server()

func _process(delta: float) -> void:
	api.poll()

func _physics_process(delta: float) -> void:
	_physics_process_count += 1
	_accumulated_time += delta

	if delta > FRAME_THRESHOLD_SECONDS:
		push_error("[Warning] _PhysicsProcess delta too high: %ss (> %ss)" % [delta, FRAME_THRESHOLD_SECONDS])

	# Every one second…
	if _accumulated_time >= 1.0:
		print("[Performance] PhysicsProcess/sec = %d" % _physics_process_count)
		print("[Server] Connected clients: %d" % connection_count)
		_physics_process_count = 0
		_accumulated_time -= 1.0

	ReceiveArray(fixed_array);


func _start_server() -> void:
	var err = peer.create_server(Port, MaxPlayers, 2, 0, 0)
	if err != OK:
		push_error("[Server] Failed to start server on port %d: %s" % [Port, err])
		return

	# Disable compression
	peer.host.compress(ENetConnection.COMPRESS_NONE)

	# Disable VSync for maximum throughput
	DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)

	# Configure SceneMultiplayer
	api.server_relay = false
	api.multiplayer_peer = peer
	get_tree().set_multiplayer(api, self.get_path())

	print("[Server] Server started on port %d" % Port)

	# Connect signals
	api.peer_connected.connect(_on_peer_connected)
	api.peer_disconnected.connect(_on_peer_disconnected)

func _on_peer_connected(id: int) -> void:
	print("[Server] User connected with ID: %d" % id)
	var pp: ENetPacketPeer = peer.get_peer(id)
	# Tuning timeouts & ping
	pp.set_timeout(1500, 30000, 60000)
	pp.ping_interval(2000)
	connection_count += 1

func _on_peer_disconnected(id: int) -> void:
	print("[Server] User disconnected with ID: %d" % id)
	connection_count -= 1
    

@rpc("authority", "call_remote", "unreliable", 0)
func ReceiveArray(array) -> void:
	rpc_id(0, "ReceiveArray", array);
