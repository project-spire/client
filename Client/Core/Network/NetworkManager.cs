using Spire.Core.Log;

namespace Spire.Core.Network;

public partial class NetworkManager : LoggableNode
{
    public static NetworkManager Instance { get; private set; } = null!;
    
    public Session Session { get; private set; } = null!;
    private ClientProtocolDispatcher _protocolDispatcher = null!;

    public override void _Ready()
    {
        Instance = this;
        
        ClientProtocolDispatcher.Initialize();

        _protocolDispatcher = new ClientProtocolDispatcher(Logger);
        Session = new Session(_protocolDispatcher, Logger);
    }
}