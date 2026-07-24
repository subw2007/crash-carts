using Godot;
using System.Collections.Generic;

public partial class GameManager : Node3D
{
    [Export] private PackedScene _playerPrefab;
    [Export] private Node3D _spawnPointsContainer;

    public override void _Ready()
    {
        // Only the Host/Server is allowed to spawn player nodes
        if (Multiplayer.IsServer())
        {
            SpawnAllPlayers();

            // Handle players connecting late (mid-game joiners)
            Multiplayer.PeerConnected += OnPlayerJoinedMidGame;
            Multiplayer.PeerDisconnected += OnPlayerDisconnected;
        }
    }

    private void SpawnAllPlayers()
    {
        var spawnPoints = _spawnPointsContainer.GetChildren();
        int spawnIndex = 0;

        foreach (var player in NetworkManager.Instance.Players)
        {
            long peerId = player.Key;
            
            // Get position from spawn markers
            Transform3D spawnTransform = Transform3D.Identity;
            if (spawnPoints.Count > 0)
            {
                var marker = (Marker3D)spawnPoints[spawnIndex % spawnPoints.Count];
                spawnTransform = marker.GlobalTransform;
                spawnIndex++;
            }

            SpawnPlayer(peerId, spawnTransform);
        }
    }

    private void OnPlayerJoinedMidGame(long peerId)
    {
        var spawnPoints = _spawnPointsContainer.GetChildren();
        Transform3D spawnTransform = Transform3D.Identity;

        if (spawnPoints.Count > 0)
        {
            // Pick a random index safely using GD.RandRange or explicit int cast
            int randomIndex = GD.RandRange(0, spawnPoints.Count - 1);
            var marker = (Marker3D)spawnPoints[randomIndex];
            spawnTransform = marker.GlobalTransform;
        }

        SpawnPlayer(peerId, spawnTransform);
    }

    private void SpawnPlayer(long peerId, Transform3D transform)
    {
        Node3D playerInstance = _playerPrefab.Instantiate<Node3D>();
        
        // IMPORTANT: Name the node after its Peer ID so Godot's MultiplayerSpawner can sync it!
        playerInstance.Name = peerId.ToString();
        playerInstance.GlobalTransform = transform;

        // Adding as a child triggers MultiplayerSpawner to spawn it on all clients
        AddChild(playerInstance, true);
    }

    private void OnPlayerDisconnected(long peerId)
    {
        if (HasNode(peerId.ToString()))
        {
            GetNode(peerId.ToString()).QueueFree();
        }
    }

    public override void _ExitTree()
    {
        if (Multiplayer.IsServer())
        {
            Multiplayer.PeerConnected -= OnPlayerJoinedMidGame;
            Multiplayer.PeerDisconnected -= OnPlayerDisconnected;
        }
    }
}