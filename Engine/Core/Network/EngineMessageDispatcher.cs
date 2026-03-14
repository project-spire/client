using System.Reflection;
using Microsoft.Extensions.Logging;
using Spire.Message.Game;

namespace Spire.Core.Network;

public class EngineMessageDispatcher(ILogger logger) : MessageDispatcher
{
    public static void Initialize()
    {
        MessageDispatcher.Initialize(Assembly.GetExecutingAssembly());
    }

    public override void Dispatch(IMessage message)
    {
        if (!HandlerEntries.TryGetValue(message.MessageId, out var entry))
        {
            logger.LogError($"Unhandled message {nameof(message)}");
            return;
        }

        try
        {
            var messageData = entry.valueProperty.GetValue(message);
            entry.handler.Invoke(null, [messageData]);
        }
        catch (Exception e)
        {
            logger.LogError("Failed to dispatch message {MessageName}: {EMessage}", nameof(message), e.Message);
        }
    }
}