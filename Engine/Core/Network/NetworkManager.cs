using Spire.Core.Log;

namespace Spire.Core.Network;

public partial class NetworkManager : LoggableNode
{
    public static Session Session { get; private set; } = null!;

    public override void _Ready()
    {
        EngineMessageDispatcher.Initialize();

        var messageDispatcher = new EngineMessageDispatcher(Logger);
        Session = new Session(messageDispatcher, Logger);
    }
}