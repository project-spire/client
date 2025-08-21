using Microsoft.Extensions.Logging;
using Spire.Core.Network;
using Spire.Protocol;
using Spire.Protocol.Auth;

namespace Spire.Bot.Handler.Auth;

public static class LoginResultHandler
{
    [ProtocolHandler(typeof(LoginResultProtocol))]
    public static void Handle(LoginResult result, BotContext ctx)
    {
        ctx.Logger.LogInformation("Login result: {result}", result.Result.ToString());
    }
}
