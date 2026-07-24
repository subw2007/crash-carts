using Godot;

public partial class ClientLobbyUI : Control
{
    [Export] private VBoxContainer _playerListContainer;
    [Export] private Label _statusLabel;

    public override void _Ready()
    {
        NetworkManager.Instance.PlayerListUpdated += RefreshLobbyList;

        if (_statusLabel != null)
        {
            _statusLabel.Text = "Waiting for host to start the game...";
        }

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

    public override void _ExitTree()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.PlayerListUpdated -= RefreshLobbyList;
    }
}