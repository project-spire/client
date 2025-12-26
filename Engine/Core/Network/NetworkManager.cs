using Spire.Core.Log;

namespace Spire.Core.Network;

public partial class NetworkManager : LoggableNode
{
    public static Session Session { get; private set; } = null!;

    public override void _Ready()
    {
        EngineProtocolDispatcher.Initialize();

        var protocolDispatcher = new EngineProtocolDispatcher(Logger);
        Session = new Session(protocolDispatcher, Logger);
    }
}