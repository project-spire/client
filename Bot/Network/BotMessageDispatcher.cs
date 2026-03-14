using System.Reflection;
using Microsoft.Extensions.Logging;
using Spire.Core.Network;
using Spire.Message.Game;

namespace Spire.Bot.Network;

public class BotMessageDispatcher(ILogger logger, BotContext ctx) : MessageDispatcher
{
    public static void Initialize(Assembly assembly)
    {
        MessageDispatcher.Initialize(assembly, typeof(BotContext));
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
            entry.handler.Invoke(null, [ctx, messageData]);
        }
        catch (Exception e)
        {
            logger.LogError("Failed to dispatch message {MessageName}: {EMessage}", nameof(message), e.Message);
        }
    }
}
