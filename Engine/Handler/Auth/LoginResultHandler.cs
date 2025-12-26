using Microsoft.Extensions.Logging;
using Spire.Core.Network;
using Spire.Protocol.Game;
using Spire.Protocol.Game.Auth;

namespace Spire.Handler.Auth;

public static class LoginResultHandler
{
    [ProtocolHandler(typeof(LoginResultProtocol))]
    public static void Handle(LoginResult result)
    {
        
    }
}