using Godot;

namespace Spire.Lobby;

public partial class Lobby : Node
{
    [Export] public required LineEdit DevIdInput;
    [Export] public required Button StartButton;
    
    private LobbyManager _lobbyManager = null!;

    public override void _Ready()
    {
        StartButton.Pressed += OnStartButtonPressed;
        
        _lobbyManager = GetNode<LobbyManager>("/root/LobbyManager");
        _lobbyManager.AccountRequestCompleted += OnAccountRequestCompleted;
        _lobbyManager.AccountRequestFailed += OnAccountRequestFailed;
    }

    private void OnStartButtonPressed()
    {
        _ = _lobbyManager.RequestDevAccountAsync(DevIdInput.Text);
    }

    private void OnAccountRequestCompleted()
    {
        
    }

    private void OnAccountRequestFailed()
    {
        
    }
}
