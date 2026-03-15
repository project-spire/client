using Spire.Core.Log;
using Spire.Core.State;

namespace Spire.Core.Network;

public partial class NetworkManager : LoggableNode
{
    public static Session Session { get; private set; } = null!;
    public static GameState GameState { get; private set; } = null!;

    public override void _Ready()
    {
        GameState = new GameState();
        var messageDispatcher = new EngineMessageDispatcher(Logger, GameState);
        messageDispatcher.Initialize();
        Session = new Session(messageDispatcher, Logger);
    }
}
