using Grpc.Core;
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
    public ushort BotId { get; init; }
    public string DevId { get; init; }
    public long? AccountId { get; private set; }
    public string? Token { get; private set; }
    public GrpcChannel LobbyChannel { get; init; }
    public readonly ILogger<BotContext> Logger;
    
    private readonly TaskCompletionSource _stopped = new();

    // public DevAuth.DevAuthClient DevAuthClient { get; }
    
    public Session GameSession { get; }
    // public Account? Account { get; set; }
    
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

        var credentials = ChannelCredentials.Create(
            ChannelCredentials.SecureSsl,
            CallCredentials.FromInterceptor((_, metadata) =>
            {
                if (Token is not null)
                    metadata.Add("authentication", Token);
                
                return Task.CompletedTask;
            }));

        var options = new GrpcChannelOptions
        {
            HttpHandler = handler,
            Credentials = credentials
        };
        LobbyChannel = GrpcChannel.ForAddress(Config.LobbyAddress, options);
        
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

    public void OnDevAccountAcquired(long accountId, string token)
    {
        Logger.LogInformation("Dev Account acquired: {accountId}, {token}", accountId, token);
        
        AccountId = accountId;
        Token = token;
    }

    public void OnCharacterAcquired(Protocol.Character character)
    {
        Logger.LogInformation("Dev Account acquired: {name}", character.Name);
    }
}
