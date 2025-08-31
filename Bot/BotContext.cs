using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Spire.Bot.Network;
using Spire.Core;
using Spire.Core.BehaviorTree;
using Spire.Core.Network;
using Spire.Protocol.Lobby;

namespace Spire.Bot;

public class BotContext : INodeContext
{
    public readonly ushort BotId;
    public readonly string DevId;
    public readonly ILogger<BotContext> Logger;
    
    private readonly TaskCompletionSource _stopped = new();

    public Accountant.AccountantClient LobbyClient { get; }
    public Session GameSession { get; }
    public Account? Account { get; set; }
    public Character? Character { get; set; }
    public Task Stopped => _stopped.Task;
    
    public BotContext(ushort botId, ILogger<BotContext> logger)
    {
        BotId = botId;
        DevId = $"{Config.BotPrefix}_{BotId:D5}";
        Logger = logger;

        var handler = new HttpClientHandler();
        if (Config.TrustServerCertificate)
        {
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        }

        var options = new GrpcChannelOptions
        {
            HttpHandler = handler
        };
        var channel = GrpcChannel.ForAddress(Config.LobbyAddress, options);
        
        LobbyClient = new Accountant.AccountantClient(channel);
        
        var protocolDispatcher = new BotProtocolDispatcher(Logger, this);
        GameSession = new Session(protocolDispatcher, Logger);
    }

    public void Stop(Exception? e = null)
    {
        if (e != null)
            Logger.LogError("Stopping: {message}", e.Message);
        
        GameSession.Stop();
        _stopped.TrySetResult();
    }
}
