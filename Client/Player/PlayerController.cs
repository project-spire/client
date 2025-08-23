using Godot;

namespace Spire.Player;

public class PlayerController
{
    private const string MoveLeftKey = "MoveLeft";
    private const string MoveRightKey = "MoveRight";
    private const string MoveForwardKey = "MoveForward";
    private const string MoveBackwardKey = "MoveBackward";
    private const string JumpKey = "Jump";
    
    public void HandleInput()
    {
        float lr = 0;
        float fb = 0;
        
        if (Input.Is)
    }
}