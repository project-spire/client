using Spire.Protocol;
using Spire.Protocol.Auth;
using Spire.Protocol.Game;
using Spire.Protocol.Net;

namespace Spire.Core.Network;

public abstract class MessageHandler
{
    public void Handle(IngressMessage message)
    {
        switch (message.Category)
        {
            case ProtocolCategory.Auth:
                var auth = AuthServerProtocol.Parser.ParseFrom(message.Data);
                if (auth == null)
                {
                    HandleError(new Exception("Failed to parse auth protocol"), message);
                    return;
                }
                
                HandleAuth(auth);
                break;
            case ProtocolCategory.Game:
                var game = GameServerProtocol.Parser.ParseFrom(message.Data);
                if (game == null)
                {
                    HandleError(new Exception("Failed to parse game protocol"), message);
                    return;
                }
                
                HandleGame(game);
                break;
            case ProtocolCategory.Net:
                var net = NetServerProtocol.Parser.ParseFrom(message.Data);
                if (net == null)
                {
                    HandleError(new Exception("Failed to parse net protocol"), message);
                    return;
                }
                
                HandleNet(net);
                break;
            case ProtocolCategory.None:
            default:
                HandleError(new Exception("Unknown protocol category"), message);
                break;
        }
    }
    
    protected abstract void HandleError(Exception e, IngressMessage message);

    protected abstract void HandleAuth(AuthServerProtocol auth);
    
    protected abstract void HandleGame(GameServerProtocol game);
    
    protected abstract void HandleNet(NetServerProtocol net);
}