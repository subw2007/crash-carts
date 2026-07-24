using Godot;

public partial class HostLobbyUI : Control
{
    [Export] private PackedScene _mainGameScene;
    [Export] private VBoxContainer _playerListContainer;
    [Export] private Button _startGameButton;
    [Export] private Button _inviteButton;

    public override void _Ready()
    {
        NetworkManager.Instance.PlayerListUpdated += RefreshLobbyList;

        if (_startGameButton != null) _startGameButton.Pressed += OnStartGamePressed;
        if (_inviteButton != null) _inviteButton.Pressed += OnInvitePressed;

        RefreshLobbyList();
    }

    private void RefreshLobbyList()
    {
        if (_playerListContainer == null) return;

        foreach (Node child in _playerListContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var player in NetworkManager.Instance.Players)
        {
            Label label = new Label();
            string pName = player.Value.ContainsKey("name") ? player.Value["name"] : "Player";
            bool isHost = player.Key == 1;

            label.Text = isHost ? $"[HOST] {pName}" : $"{pName}";
            _playerListContainer.AddChild(label);
        }
    }

    private void OnStartGamePressed()
    {
        if (_mainGameScene == null)
        {
            GD.PrintErr("[HostLobbyUI] Please assign Main Game Scene in Inspector!");
            return;
        }

        NetworkManager.Instance.StartGameWithPackedScene(_mainGameScene);
    }

    private void OnInvitePressed()
    {
        // FIX: Replaced GodotSteam call with NetworkManager helper
        NetworkManager.Instance.OpenFriendsOverlay();
    }

    public override void _ExitTree()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.PlayerListUpdated -= RefreshLobbyList;
    }
}