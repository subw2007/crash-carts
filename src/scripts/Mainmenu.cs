using Godot;

public partial class Mainmenu : Control
{
    [Export] private PackedScene _hostLobbyScene;
    [Export] private PackedScene _clientLobbyScene;

    private Button _hostServerButton;
    private Button _hostP2PButton;
    private Button _joinGameButton;

    public override void _Ready()
    {
        _hostServerButton = GetNode<Button>("HBoxContainer/hostserver");
        _hostP2PButton = GetNode<Button>("HBoxContainer/hostp2p");
        _joinGameButton = GetNode<Button>("HBoxContainer/joingame");

        _hostServerButton.Pressed += OnHostServerPressed;
        _hostP2PButton.Pressed += OnHostP2PPressed;
        _joinGameButton.Pressed += OnJoinGamePressed;
    }

    private void OnHostServerPressed()
    {
        GD.Print("[MainMenu] Hosting Server/IP...");
        NetworkManager.Instance.SetLobbyScenes(_hostLobbyScene, _clientLobbyScene);
        NetworkManager.Instance.HostGameDirect();
    }

    private void OnHostP2PPressed()
    {
        GD.Print("[MainMenu] Hosting Steam P2P...");
        NetworkManager.Instance.SetLobbyScenes(_hostLobbyScene, _clientLobbyScene);
        NetworkManager.Instance.HostSteamLobby();
    }

    private void OnJoinGamePressed()
    {
        GD.Print("[MainMenu] Joining Game via Steam...");
        NetworkManager.Instance.SetLobbyScenes(_hostLobbyScene, _clientLobbyScene);
        
        // FIX: Replaced GodotSteam call with NetworkManager helper
        NetworkManager.Instance.OpenFriendsOverlay();
    }
}