using System.Reflection;
using Microsoft.Extensions.Logging;
using Spire.Core.State;
using Spire.Message.Game;

namespace Spire.Core.Network;

public class EngineMessageDispatcher(ILogger logger, GameState gameState) : MessageDispatcher
{
    public void Initialize()
    {
        RegisterCoreHandlers(typeof(GameState).Assembly, typeof(GameState));
        RegisterFrontendHandlers(Assembly.GetExecutingAssembly());
    }

    protected override object?[] BuildCoreArgs(object? messageData) => [messageData, gameState];

    protected override object?[] BuildFrontendArgs(object? messageData) => [messageData];

    protected override void OnUnhandledMessage(IMessage message)
    {
        logger.LogWarning("Unhandled message: {MessageId}", message.MessageId);
    }

    protected override void OnDispatchError(IMessage message, Exception e)
    {
        logger.LogError("Failed to dispatch message {MessageId}: {Error}", message.MessageId, e.Message);
    }
}
