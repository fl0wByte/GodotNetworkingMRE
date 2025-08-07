extends Node
class_name ClientNetworkManager

@export var ServerAddress: String = "localhost"
@export var Port: int = 5000


# Singleton instance
static var Instance: ClientNetworkManager = null

var peer: ENetMultiplayerPeer
var api: SceneMultiplayer
var _handshake_start_time: int = 0


func _enter_tree() -> void:
	if Instance != null:
		queue_free()
		return
	Instance = self

func _exit_tree() -> void:
	Instance = null

func _ready() -> void:
	peer = ENetMultiplayerPeer.new()
	api = SceneMultiplayer.new()
	_connect_to_server()

func _connect_to_server() -> void:
	_handshake_start_time = Time.get_ticks_msec()
	var err = peer.create_client(ServerAddress, Port, 2, 0, 0)
	if err != OK:
		print("[Client] Failed to create client: %s" % err)
		return

	# Disable compression
	peer.host.compress(ENetConnection.COMPRESS_NONE)

	# Assign to SceneMultiplayer
	api.multiplayer_peer = peer
	get_tree().set_multiplayer(api, self.get_path())

	print("[Client] Connecting to server at %s:%d" % [ServerAddress, Port])

	# Connect signals
	api.connect("connection_failed", Callable(self, "_on_connection_failed"))
	api.connect("connected_to_server", Callable(self, "_on_connected_to_server"))
	api.connect("server_disconnected", Callable(self, "_on_server_disconnected"))

func _on_connection_failed() -> void:
	print("[Client] Connection to game server failed.")
	api.disconnect("connection_failed", Callable(self, "_on_connection_failed"))
	api.disconnect("connected_to_server", Callable(self, "_on_connected_to_server"))
	api.disconnect("server_disconnected", Callable(self, "_on_server_disconnected"))
	peer.close()
	api.multiplayer_peer = null

func _on_connected_to_server() -> void:
	var handshake_ms = Time.get_ticks_msec() - _handshake_start_time
	print("[Client] Connection to game server succeeded.")
	print("[Client] Handshake took %.2f ms" % handshake_ms)

func _on_server_disconnected() -> void:
	print("[Client] Disconnected from server (server disconnected).")
	get_tree().quit()


@rpc("authority", "call_local", "unreliable", 0)
func ReceiveArray(array) -> void:
	pass