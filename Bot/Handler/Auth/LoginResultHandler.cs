using Microsoft.Extensions.Logging;
using Spire.Core.Network;
using Spire.Protocol.Auth;

namespace Spire.Bot.Handler.Auth;

public static class LoginResultHandler
{
    [ProtocolHandler]
    public static void Handle(ISessionContext ctx, LoginResult result)
    {
        var btx = (BotContext)ctx;
        btx.Logger.LogInformation("Login result: {result}", result.Result.ToString());
    }
}
