using Spire.Core.Log;

namespace Spire.Core.Network;

public partial class NetworkManager : LoggableNode
{
    public static NetworkManager Instance { get; private set; } = null!;
    
    public Session Session { get; private set; } = null!;

    public override void _Ready()
    {
        Instance = this;
        
        ClientProtocolDispatcher.Initialize();

        var protocolDispatcher = new ClientProtocolDispatcher(Logger);
        Session = new Session(protocolDispatcher, Logger);
    }
}