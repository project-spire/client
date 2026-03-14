using Microsoft.Extensions.Logging;
using Spire.Core.Network;
using Spire.Message.Game;
using Spire.Message.Game.Auth;

namespace Spire.Handler.Auth;

public static class LoginResultHandler
{
    [MessageHandler(typeof(LoginResultMessage))]
    public static void Handle(LoginResult result)
    {

    }
}