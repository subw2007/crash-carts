using Godot;
using Godot.Collections;
using System;

public partial class NetworkManager : Node
{
    public static NetworkManager Instance { get; private set; }

    public event Action PlayerListUpdated;

    private GodotObject _steam;
    private PackedScene _hostLobbyScene;
    private PackedScene _clientLobbyScene;

    // Player metadata dictionary: Key = PeerId, Value = Dictionary
    public Dictionary<long, Dictionary<string, string>> Players { get; private set; } = new();

    public override void _Ready()
	{
	    Instance = this;

	    // Fetch the GodotSteam GDExtension singleton dynamically
	    if (Engine.HasSingleton("Steam"))
	    {
	        _steam = Engine.GetSingleton("Steam");

	        // Initialize Steam API (480 = AppId, true = embedded callbacks)
	        // steamInitEx returns a Godot Dictionary with the init status
	        Godot.Collections.Dictionary initResult = (Godot.Collections.Dictionary)_steam.Call("steamInitEx", 480, true);
	
	        long status = (long)initResult["status"];

	        if (status == 0) // 0 = STEAM_API_INIT_RESULT_OK
	        {
	            string personaName = (string)_steam.Call("getPersonaName");
	            GD.Print($"[GodotSteam] Successfully initialized as: {personaName}");

	            // Connect GDExtension signals
	            _steam.Connect("lobby_created", Callable.From<long, ulong>(OnLobbyCreated));
	            _steam.Connect("lobby_joined", Callable.From<ulong, uint, bool, int>(OnLobbyJoined));
	            _steam.Connect("join_requested", Callable.From<ulong, ulong>(OnLobbyJoinRequested));
	        }
	        else
	        {
	            GD.PrintErr($"[GodotSteam] Steam initialization failed (Code {status}): {initResult["verbal"]}");
	            GD.PrintErr("[GodotSteam] Defaulting to local direct networking mode.");
	            _steam = null; // Nullify so fallback logic takes over
	        }
	    }
	    else
	    {
	        GD.PrintErr("[GodotSteam] Steam GDExtension singleton not found!");
	    }

	    // Connect Godot high-level multiplayer signals
	    Multiplayer.PeerConnected += OnPeerConnected;
	    Multiplayer.PeerDisconnected += OnPeerDisconnected;
	    Multiplayer.ConnectedToServer += OnConnectedToServer;
	}
    

    public override void _Process(double delta)
    {
        // Run Steam Callbacks every frame via dynamic invocation
		_steam?.Call("run_callbacks");
    }

    public void SetLobbyScenes(PackedScene hostScene, PackedScene clientScene)
    {
        _hostLobbyScene = hostScene;
        _clientLobbyScene = clientScene;
    }

    // --- STEAM OVERLAY ---

    public void OpenFriendsOverlay()
    {
        if (_steam != null)
        {
            _steam.Call("activateGameOverlay", "Friends");
        }
        else
        {
            GD.PrintErr("[NetworkManager] Cannot open overlay: Steam is not initialized.");
        }
    }

    // --- HOSTING LOGIC ---

    public void HostSteamLobby()
    {
        if (_steam == null) return;
        GD.Print("[GodotSteam] Creating Steam Lobby...");
        
        // Call GDExtension function: CreateLobby(type: 2 [FriendsOnly], max_members: 4)
        _steam.Call("createLobby", 2, 4);
    }

    public void HostGameDirect()
    {
        var peer = new ENetMultiplayerPeer();
        peer.CreateServer(7000, 4);
        Multiplayer.MultiplayerPeer = peer;

        AddLocalPlayer(1);

        if (_hostLobbyScene != null) 
            GetTree().ChangeSceneToPacked(_hostLobbyScene);
    }

    private void OnLobbyCreated(long connectResult, ulong lobbyId)
    {
        if (connectResult != 1) return; // 1 = OK

        GD.Print($"[GodotSteam] Lobby Created Successfully: {lobbyId}");

        var peer = new ENetMultiplayerPeer();
        peer.CreateServer(7000, 4);
        Multiplayer.MultiplayerPeer = peer;

        AddLocalPlayer(1);

        if (_hostLobbyScene != null) 
            GetTree().ChangeSceneToPacked(_hostLobbyScene);
    }

    // --- JOINING LOGIC ---

    private void OnLobbyJoinRequested(ulong lobbyId, ulong steamIdFriend)
    {
        _steam?.Call("joinLobby", lobbyId);
    }

    private void OnLobbyJoined(ulong lobbyId, uint permissions, bool locked, int response)
    {
        if (!Multiplayer.IsServer())
        {
            var peer = new ENetMultiplayerPeer();
            peer.CreateClient("127.0.0.1", 7000);
            Multiplayer.MultiplayerPeer = peer;

            if (_clientLobbyScene != null) 
                GetTree().ChangeSceneToPacked(_clientLobbyScene);
        }
    }

    // --- DATA SYNC & RPCs ---

    private void AddLocalPlayer(long peerId)
    {
        string pName = _steam != null ? (string)_steam.Call("getPersonaName") : "Player";
        var localData = new Dictionary<string, string> { { "name", pName } };

        Players[peerId] = localData;
        PlayerListUpdated?.Invoke();
    }

    private void OnPeerConnected(long peerId)
    {
        string pName = _steam != null ? (string)_steam.Call("getPersonaName") : "Player";
        RpcId(peerId, MethodName.SendPlayerData, pName);
    }

    private void OnPeerDisconnected(long peerId)
    {
        Players.Remove(peerId);
        PlayerListUpdated?.Invoke();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SendPlayerData(string name)
    {
        long senderId = Multiplayer.GetRemoteSenderId();
        Players[senderId] = new Dictionary<string, string> { { "name", name } };
        PlayerListUpdated?.Invoke();
    }

    private void OnConnectedToServer()
    {
        AddLocalPlayer(Multiplayer.GetUniqueId());
    }

    public void StartGameWithPackedScene(PackedScene gameScene)
    {
        if (Multiplayer.IsServer())
        {
            Rpc(MethodName.LoadGameSceneRPC, gameScene.ResourcePath);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void LoadGameSceneRPC(string scenePath)
    {
        GetTree().ChangeSceneToFile(scenePath);
    }
}