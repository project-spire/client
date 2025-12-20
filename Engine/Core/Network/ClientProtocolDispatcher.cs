using System.Reflection;
using Microsoft.Extensions.Logging;
using Spire.Protocol.Game;

namespace Spire.Core.Network;

public class ClientProtocolDispatcher(ILogger logger) : ProtocolDispatcher
{
    public static void Initialize()
    {
        ProtocolDispatcher.Initialize(Assembly.GetExecutingAssembly());
    }

    public override void Dispatch(IProtocol protocol)
    {
        if (!HandlerEntries.TryGetValue(protocol.ProtocolId, out var entry))
        {
            logger.LogError($"Unhandled protocol {nameof(protocol)}");
            return;
        }
        
        try
        {
            var protocolData = entry.valueProperty.GetValue(protocol);
            entry.handler.Invoke(null, [protocolData]);
        }
        catch (Exception e)
        {
            logger.LogError("Failed to dispatch protocol {ProtocolName}: {EMessage}", nameof(protocol), e.Message);
        }
    }
}