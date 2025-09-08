using System.Reflection;
using Microsoft.Extensions.Logging;
using Spire.Core.Network;
using Spire.Protocol.Game;

namespace Spire.Bot.Network;

public class BotProtocolDispatcher(ILogger logger, BotContext ctx) : ProtocolDispatcher
{
    public static void Initialize(Assembly assembly)
    {
        ProtocolDispatcher.Initialize(assembly, typeof(BotContext));
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
            entry.handler.Invoke(null, [ctx, protocolData]);
        }
        catch (Exception e)
        {
            logger.LogError("Failed to dispatch protocol {ProtocolName}: {EMessage}", nameof(protocol), e.Message);
        }
    }
}
