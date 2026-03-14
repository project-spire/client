using Microsoft.Extensions.Logging;
using Spire.Core.Network;
using Spire.Message.Game;
using Spire.Message.Game.Auth;

namespace Spire.Bot.Handler.Auth;

public static class LoginResultHandler
{
    [MessageHandler(typeof(LoginResultMessage))]
    public static void Handle(LoginResult result, BotContext ctx)
    {
        ctx.Logger.LogInformation("Login result: {result}", result.Result.ToString());
    }
}
